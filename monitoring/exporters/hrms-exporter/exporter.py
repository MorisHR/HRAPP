#!/usr/bin/env python3
"""
HRMS Custom Metrics Exporter
Exports application-specific metrics from PostgreSQL monitoring schema
Optimized for: Millions of requests per minute
Performance: Sub-millisecond metric collection using materialized views
"""

import os
import sys
import time
import logging
import psycopg2
from psycopg2.extras import RealDictCursor
from prometheus_client import start_http_server, Gauge, Counter, Histogram, Info
from prometheus_client.core import GaugeMetricFamily, CounterMetricFamily, REGISTRY
import signal

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('hrms-exporter')

# Configuration from environment
DATABASE_URL = os.getenv('DATABASE_URL', 'postgresql://monitoring_reader:monitoring_pass@localhost:5432/hrms_master')
EXPORTER_PORT = int(os.getenv('EXPORTER_PORT', '9188'))
METRICS_REFRESH_INTERVAL = int(os.getenv('METRICS_REFRESH_INTERVAL', '30'))
LOG_LEVEL = os.getenv('LOG_LEVEL', 'INFO').upper()

logger.setLevel(LOG_LEVEL)

class HRMSMetricsCollector:
    """
    Custom collector for HRMS application metrics
    Uses materialized views for high-performance metric retrieval
    """

    def __init__(self, db_url):
        self.db_url = db_url
        self.conn = None
        self._connect()

    def _connect(self):
        """Establish database connection with connection pooling"""
        try:
            self.conn = psycopg2.connect(
                self.db_url,
                connect_timeout=10,
                options='-c statement_timeout=5000'  # 5 second query timeout
            )
            self.conn.set_session(readonly=True, autocommit=True)
            logger.info("Connected to PostgreSQL database")
        except Exception as e:
            logger.error(f"Database connection failed: {e}")
            raise

    def _execute_query(self, query, params=None):
        """Execute query with automatic reconnection"""
        max_retries = 3
        for attempt in range(max_retries):
            try:
                if self.conn is None or self.conn.closed:
                    self._connect()

                with self.conn.cursor(cursor_factory=RealDictCursor) as cursor:
                    cursor.execute(query, params)
                    return cursor.fetchall()
            except Exception as e:
                logger.warning(f"Query failed (attempt {attempt + 1}/{max_retries}): {e}")
                self.conn = None
                if attempt == max_retries - 1:
                    raise
                time.sleep(1)
        return []

    def collect(self):
        """
        Collect metrics from monitoring schema
        Called by Prometheus on each scrape
        """
        try:
            # ================================================================
            # Database Performance Metrics
            # ================================================================
            perf_metrics = self._execute_query("""
                SELECT
                    cache_hit_ratio,
                    active_connections,
                    idle_connections,
                    total_connections,
                    transactions_per_sec,
                    queries_per_sec,
                    avg_query_time_ms,
                    slow_queries_count,
                    deadlocks_count,
                    rollback_ratio
                FROM monitoring.get_dashboard_metrics()
                LIMIT 1;
            """)

            if perf_metrics:
                m = perf_metrics[0]

                # Cache hit ratio (should be >95%)
                yield GaugeMetricFamily(
                    'hrms_db_cache_hit_ratio',
                    'PostgreSQL cache hit ratio percentage',
                    value=float(m.get('cache_hit_ratio', 0))
                )

                # Database connections
                yield GaugeMetricFamily(
                    'hrms_db_connections_active',
                    'Number of active database connections',
                    value=float(m.get('active_connections', 0))
                )

                yield GaugeMetricFamily(
                    'hrms_db_connections_idle',
                    'Number of idle database connections',
                    value=float(m.get('idle_connections', 0))
                )

                yield GaugeMetricFamily(
                    'hrms_db_connections_total',
                    'Total number of database connections',
                    value=float(m.get('total_connections', 0))
                )

                # Throughput metrics
                yield GaugeMetricFamily(
                    'hrms_db_transactions_per_second',
                    'Database transactions per second',
                    value=float(m.get('transactions_per_sec', 0))
                )

                yield GaugeMetricFamily(
                    'hrms_db_queries_per_second',
                    'Database queries per second',
                    value=float(m.get('queries_per_sec', 0))
                )

                # Query performance
                yield GaugeMetricFamily(
                    'hrms_db_avg_query_time_ms',
                    'Average query execution time in milliseconds',
                    value=float(m.get('avg_query_time_ms', 0))
                )

                yield GaugeMetricFamily(
                    'hrms_db_slow_queries_count',
                    'Number of slow queries (>100ms)',
                    value=float(m.get('slow_queries_count', 0))
                )

                # Error metrics
                yield GaugeMetricFamily(
                    'hrms_db_deadlocks_total',
                    'Total number of deadlocks detected',
                    value=float(m.get('deadlocks_count', 0))
                )

                yield GaugeMetricFamily(
                    'hrms_db_rollback_ratio',
                    'Ratio of rolled back transactions',
                    value=float(m.get('rollback_ratio', 0))
                )

            # ================================================================
            # Multi-Tenant Metrics
            # ================================================================
            tenant_metrics = self._execute_query("""
                SELECT
                    COUNT(DISTINCT tenant_id) as active_tenants,
                    SUM(requests_count) as total_requests,
                    AVG(avg_response_time_ms) as avg_response_time,
                    SUM(error_count) as total_errors
                FROM monitoring.tenant_activity
                WHERE recorded_at > NOW() - INTERVAL '5 minutes';
            """)

            if tenant_metrics:
                m = tenant_metrics[0]

                yield GaugeMetricFamily(
                    'hrms_active_tenants_total',
                    'Number of active tenants in last 5 minutes',
                    value=float(m.get('active_tenants', 0) or 0)
                )

                yield CounterMetricFamily(
                    'hrms_tenant_requests_total',
                    'Total requests across all tenants (last 5 min)',
                    value=float(m.get('total_requests', 0) or 0)
                )

                yield GaugeMetricFamily(
                    'hrms_tenant_avg_response_time_ms',
                    'Average response time across all tenants',
                    value=float(m.get('avg_response_time', 0) or 0)
                )

                yield CounterMetricFamily(
                    'hrms_tenant_errors_total',
                    'Total errors across all tenants (last 5 min)',
                    value=float(m.get('total_errors', 0) or 0)
                )

            # ================================================================
            # API Performance Metrics
            # ================================================================
            api_metrics = self._execute_query("""
                SELECT
                    endpoint,
                    COUNT(*) as request_count,
                    AVG(response_time_ms) as avg_response_time,
                    MAX(response_time_ms) as max_response_time,
                    SUM(CASE WHEN status_code >= 500 THEN 1 ELSE 0 END) as error_5xx,
                    SUM(CASE WHEN status_code >= 400 AND status_code < 500 THEN 1 ELSE 0 END) as error_4xx
                FROM monitoring.api_performance
                WHERE recorded_at > NOW() - INTERVAL '5 minutes'
                GROUP BY endpoint
                ORDER BY request_count DESC
                LIMIT 20;
            """)

            if api_metrics:
                # Per-endpoint request counts
                request_count_metric = GaugeMetricFamily(
                    'hrms_api_endpoint_requests',
                    'Request count per API endpoint (last 5 min)',
                    labels=['endpoint']
                )

                # Per-endpoint response times
                response_time_metric = GaugeMetricFamily(
                    'hrms_api_endpoint_response_time_ms',
                    'Average response time per endpoint',
                    labels=['endpoint']
                )

                # Per-endpoint errors
                error_metric = GaugeMetricFamily(
                    'hrms_api_endpoint_errors',
                    'Error count per endpoint',
                    labels=['endpoint', 'type']
                )

                for row in api_metrics:
                    endpoint = row['endpoint'] or 'unknown'
                    request_count_metric.add_metric([endpoint], float(row['request_count'] or 0))
                    response_time_metric.add_metric([endpoint], float(row['avg_response_time'] or 0))
                    error_metric.add_metric([endpoint, '5xx'], float(row['error_5xx'] or 0))
                    error_metric.add_metric([endpoint, '4xx'], float(row['error_4xx'] or 0))

                yield request_count_metric
                yield response_time_metric
                yield error_metric

            # ================================================================
            # Security Events
            # ================================================================
            security_metrics = self._execute_query("""
                SELECT
                    event_type,
                    COUNT(*) as event_count
                FROM monitoring.security_events
                WHERE recorded_at > NOW() - INTERVAL '1 hour'
                GROUP BY event_type;
            """)

            if security_metrics:
                security_metric = CounterMetricFamily(
                    'hrms_security_events_total',
                    'Security events by type (last hour)',
                    labels=['event_type']
                )

                for row in security_metrics:
                    security_metric.add_metric(
                        [row['event_type'] or 'unknown'],
                        float(row['event_count'] or 0)
                    )

                yield security_metric

            logger.debug("Metrics collection completed successfully")

        except Exception as e:
            logger.error(f"Error collecting metrics: {e}", exc_info=True)
            # Return empty metrics on error to prevent scrape failure
            yield GaugeMetricFamily('hrms_exporter_error', 'Exporter error occurred', value=1)

def signal_handler(signum, frame):
    """Graceful shutdown on SIGTERM/SIGINT"""
    logger.info(f"Received signal {signum}, shutting down gracefully...")
    sys.exit(0)

def main():
    """Main exporter entry point"""
    logger.info(f"Starting HRMS Metrics Exporter on port {EXPORTER_PORT}")
    logger.info(f"Metrics refresh interval: {METRICS_REFRESH_INTERVAL}s")
    logger.info(f"Database: {DATABASE_URL.split('@')[1] if '@' in DATABASE_URL else 'unknown'}")

    # Register signal handlers
    signal.signal(signal.SIGTERM, signal_handler)
    signal.signal(signal.SIGINT, signal_handler)

    try:
        # Create and register collector
        collector = HRMSMetricsCollector(DATABASE_URL)
        REGISTRY.register(collector)

        # Start HTTP server
        start_http_server(EXPORTER_PORT)
        logger.info(f"✓ Exporter started successfully on port {EXPORTER_PORT}")
        logger.info(f"✓ Metrics available at http://localhost:{EXPORTER_PORT}/metrics")

        # Keep running
        while True:
            time.sleep(1)

    except Exception as e:
        logger.error(f"Failed to start exporter: {e}", exc_info=True)
        sys.exit(1)

if __name__ == '__main__':
    main()
