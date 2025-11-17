# Infrastructure Monitoring Strategy
## Fortune 500-Grade Infrastructure Monitoring for Multi-Tenant HRMS

**Document Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** Production-Ready Infrastructure Monitoring

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Kubernetes Cluster Monitoring](#kubernetes-cluster-monitoring)
3. [Database Monitoring (PostgreSQL)](#database-monitoring-postgresql)
4. [Cache Monitoring (Redis)](#cache-monitoring-redis)
5. [CDN Monitoring](#cdn-monitoring)
6. [Network Monitoring](#network-monitoring)
7. [Tool Recommendations](#tool-recommendations)
8. [Implementation Guide](#implementation-guide)

---

## Executive Summary

### Infrastructure Monitoring Goals

- **Proactive issue detection:** Identify problems before users notice
- **Capacity planning:** Predict when to scale infrastructure
- **Cost optimization:** Right-size resources based on actual usage
- **SLA compliance:** Ensure 99.9% uptime commitment
- **Security monitoring:** Detect anomalous behavior

### Key Infrastructure Components

```
┌─────────────────────────────────────────────────────────┐
│                    Load Balancer                         │
│                  (Nginx/Azure LB)                        │
└─────────────────────┬───────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
┌───────▼──────┐ ┌───▼──────┐ ┌───▼──────┐
│  API Pod 1   │ │ API Pod 2│ │ API Pod 3│  Kubernetes
│  (.NET 8)    │ │ (.NET 8) │ │ (.NET 8) │  Cluster
└──────┬───────┘ └────┬─────┘ └────┬─────┘
       │              │            │
       └──────────────┼────────────┘
                      │
         ┌────────────┼────────────┐
         │            │            │
┌────────▼─────┐ ┌───▼──────┐ ┌──▼───────┐
│ PostgreSQL   │ │  Redis   │ │   CDN    │
│ (Master+     │ │  Cache   │ │ (Static  │
│  Replicas)   │ │          │ │  Assets) │
└──────────────┘ └──────────┘ └──────────┘
```

---

## Kubernetes Cluster Monitoring

### 1. Pod Health Monitoring

**Metrics to Track:**

| Metric | Description | Threshold | Alert |
|--------|-------------|-----------|-------|
| **Pod Status** | Running/Pending/Failed/Unknown | Failed > 0 | Critical |
| **Pod Restarts** | Container restart count | > 3 in 5 min | High |
| **Pod CPU Usage** | CPU utilization per pod | > 80% | Medium |
| **Pod Memory Usage** | Memory utilization per pod | > 85% | High |
| **Pod Ready Status** | Readiness probe status | Not ready > 2 min | High |
| **Pod Network I/O** | Incoming/outgoing traffic | Baseline +200% | Medium |

**Implementation with Prometheus + Grafana:**

```yaml
# prometheus-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s

    scrape_configs:
      - job_name: 'kubernetes-pods'
        kubernetes_sd_configs:
          - role: pod
        relabel_configs:
          - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
            action: keep
            regex: true
          - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_path]
            action: replace
            target_label: __metrics_path__
            regex: (.+)
          - source_labels: [__address__, __meta_kubernetes_pod_annotation_prometheus_io_port]
            action: replace
            regex: ([^:]+)(?::\d+)?;(\d+)
            replacement: $1:$2
            target_label: __address__
```

**Alert Rules:**

```yaml
# prometheus-alerts.yaml
groups:
  - name: kubernetes_pods
    interval: 30s
    rules:
      - alert: PodRestartingTooOften
        expr: rate(kube_pod_container_status_restarts_total[15m]) > 0.2
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Pod {{ $labels.pod }} restarting frequently"
          description: "Pod {{ $labels.pod }} in namespace {{ $labels.namespace }} has restarted {{ $value }} times in the last 15 minutes"

      - alert: PodNotReady
        expr: kube_pod_status_phase{phase!="Running"} > 0
        for: 5m
        labels:
          severity: high
        annotations:
          summary: "Pod {{ $labels.pod }} not ready"
          description: "Pod {{ $labels.pod }} has been in {{ $labels.phase }} state for more than 5 minutes"

      - alert: PodCPUThrottling
        expr: rate(container_cpu_cfs_throttled_seconds_total[5m]) > 0.5
        for: 10m
        labels:
          severity: medium
        annotations:
          summary: "Pod {{ $labels.pod }} CPU throttling"
          description: "Pod {{ $labels.pod }} is experiencing CPU throttling"
```

---

### 2. Node Resource Utilization

**Metrics to Track:**

| Metric | Description | Threshold | Action |
|--------|-------------|-----------|--------|
| **Node CPU** | CPU usage across all nodes | > 75% | Scale out |
| **Node Memory** | Memory usage across all nodes | > 80% | Scale out |
| **Node Disk** | Disk usage | > 85% | Expand storage |
| **Node Network** | Network bandwidth utilization | > 80% | Scale out |
| **Node Count** | Number of healthy nodes | < minimum | Critical alert |

**Grafana Dashboard Query Examples:**

```promql
# Node CPU usage
100 - (avg by (node) (irate(node_cpu_seconds_total{mode="idle"}[5m])) * 100)

# Node memory usage
(node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes) / node_memory_MemTotal_bytes * 100

# Node disk usage
(node_filesystem_size_bytes{mountpoint="/"} - node_filesystem_free_bytes{mountpoint="/"}) / node_filesystem_size_bytes{mountpoint="/"} * 100

# Pod count per node
count(kube_pod_info) by (node)
```

**Auto-scaling Configuration:**

```yaml
# horizontal-pod-autoscaler.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: hrms-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: hrms-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 50
          periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
        - type: Percent
          value: 100
          periodSeconds: 30
```

---

### 3. Container Restarts

**Why Monitor Restarts:**
- Indicates application crashes
- Memory leaks causing OOMKilled
- Health check failures
- Configuration issues

**Alert Configuration:**

```yaml
- alert: ContainerHighRestartRate
  expr: rate(kube_pod_container_status_restarts_total[1h]) > 0.05
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "Container {{ $labels.container }} restarting frequently"
    description: "Container {{ $labels.container }} in pod {{ $labels.pod }} has a restart rate of {{ $value }} per hour"

- alert: ContainerOOMKilled
  expr: kube_pod_container_status_last_terminated_reason{reason="OOMKilled"} > 0
  for: 1m
  labels:
    severity: critical
  annotations:
    summary: "Container {{ $labels.container }} killed due to out of memory"
    description: "Container {{ $labels.container }} in pod {{ $labels.pod }} was killed due to OOM. Consider increasing memory limits."
```

**Debugging Restart Issues:**

```bash
# Check pod events
kubectl describe pod <pod-name>

# Check pod logs before restart
kubectl logs <pod-name> --previous

# Check resource limits
kubectl get pod <pod-name> -o jsonpath='{.spec.containers[*].resources}'

# Monitor real-time resource usage
kubectl top pod <pod-name>
```

---

### 4. Network Metrics

**Metrics to Track:**

```promql
# Network throughput (bytes/sec)
rate(container_network_receive_bytes_total[5m])
rate(container_network_transmit_bytes_total[5m])

# Packet errors
rate(container_network_receive_errors_total[5m])
rate(container_network_transmit_errors_total[5m])

# TCP connections
node_netstat_Tcp_CurrEstab

# Network latency between pods (requires service mesh)
histogram_quantile(0.95, rate(istio_request_duration_milliseconds_bucket[5m]))
```

**Service Mesh Monitoring (Istio/Linkerd):**

```yaml
# Install Linkerd for service mesh observability
linkerd install | kubectl apply -f -

# View service metrics
linkerd viz dashboard

# Metrics available:
# - Request rate per service
# - Success rate per service
# - Latency percentiles (p50, p95, p99)
# - Service topology
```

---

## Database Monitoring (PostgreSQL)

### 1. Connection Pool Usage

**Why Monitor Connection Pools:**
- PostgreSQL has max_connections limit (default: 100)
- Connection exhaustion = application downtime
- Multi-tenant architecture = multiple connection pools

**Metrics to Track:**

| Metric | Query | Threshold | Alert |
|--------|-------|-----------|-------|
| **Active Connections** | `SELECT count(*) FROM pg_stat_activity WHERE state = 'active'` | > 80 | High |
| **Idle Connections** | `SELECT count(*) FROM pg_stat_activity WHERE state = 'idle'` | > 50 | Medium |
| **Idle in Transaction** | `SELECT count(*) FROM pg_stat_activity WHERE state = 'idle in transaction'` | > 10 | High |
| **Max Connections** | `SHOW max_connections` | N/A | Info |
| **Connection Usage %** | `(active + idle) / max_connections * 100` | > 85% | Critical |

**PostgreSQL Exporter for Prometheus:**

```yaml
# Install postgres-exporter
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install postgres-exporter prometheus-community/prometheus-postgres-exporter \
  --set config.datasource="postgresql://postgres:password@postgres:5432/postgres?sslmode=disable"
```

**Connection Pool Alert:**

```yaml
- alert: PostgreSQLConnectionPoolExhaustion
  expr: sum(pg_stat_database_numbackends) / max(pg_settings_max_connections) * 100 > 85
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "PostgreSQL connection pool exhaustion"
    description: "Database connection usage is at {{ $value }}%"
```

**Application-Level Monitoring (.NET):**

```csharp
// Track connection pool metrics in Application Insights
public class DatabaseMetricsService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly TelemetryClient _telemetryClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync(stoppingToken);

            // Active connections
            var activeConnections = await connection.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM pg_stat_activity WHERE state = 'active'"
            );

            _telemetryClient.TrackMetric("DB_ActiveConnections", activeConnections);

            // Idle connections
            var idleConnections = await connection.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM pg_stat_activity WHERE state = 'idle'"
            );

            _telemetryClient.TrackMetric("DB_IdleConnections", idleConnections);

            // Long-running queries (> 30 seconds)
            var longQueries = await connection.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM pg_stat_activity WHERE state = 'active' AND now() - query_start > interval '30 seconds'"
            );

            if (longQueries > 0)
            {
                _logger.LogWarning("Found {Count} long-running queries", longQueries);
            }
        }
    }
}
```

---

### 2. Query Performance

**Metrics to Track:**

```sql
-- Slow queries (> 1 second)
SELECT
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time,
    stddev_exec_time
FROM pg_stat_statements
WHERE mean_exec_time > 1000
ORDER BY mean_exec_time DESC
LIMIT 20;

-- Most frequently executed queries
SELECT
    query,
    calls,
    mean_exec_time,
    total_exec_time
FROM pg_stat_statements
ORDER BY calls DESC
LIMIT 20;

-- Queries with highest total time
SELECT
    query,
    calls,
    total_exec_time,
    mean_exec_time
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;
```

**Enable pg_stat_statements:**

```sql
-- In postgresql.conf
shared_preload_libraries = 'pg_stat_statements'
pg_stat_statements.track = all
pg_stat_statements.max = 10000

-- Create extension
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
```

**Dashboard Queries:**

```promql
# Query duration (95th percentile)
histogram_quantile(0.95, rate(pg_stat_statements_mean_exec_time_bucket[5m]))

# Queries per second
rate(pg_stat_statements_calls_total[5m])

# Cache hit ratio (should be > 95%)
sum(pg_stat_database_blks_hit) / (sum(pg_stat_database_blks_hit) + sum(pg_stat_database_blks_read)) * 100
```

---

### 3. Slow Query Log

**Configure Slow Query Logging:**

```sql
-- In postgresql.conf
log_min_duration_statement = 1000  # Log queries > 1 second
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
log_statement = 'none'             # Don't log all statements
log_duration = off                 # Don't log all durations
```

**Parse Slow Query Logs:**

```bash
# Install pgBadger for log analysis
apt-get install pgbadger

# Generate HTML report
pgbadger /var/log/postgresql/postgresql-*.log -o /var/www/html/pgbadger.html

# Schedule daily reports
0 2 * * * pgbadger /var/log/postgresql/postgresql-*.log -o /var/www/html/pgbadger-$(date +\%Y\%m\%d).html
```

---

### 4. Replication Lag

**For Master-Replica Setup:**

```sql
-- On replica, check replication lag
SELECT
    CASE
        WHEN pg_last_wal_receive_lsn() = pg_last_wal_replay_lsn() THEN 0
        ELSE EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp()))
    END AS replication_lag_seconds;

-- Replication status
SELECT * FROM pg_stat_replication;
```

**Alert on High Replication Lag:**

```yaml
- alert: PostgreSQLReplicationLag
  expr: pg_replication_lag_seconds > 30
  for: 5m
  labels:
    severity: high
  annotations:
    summary: "PostgreSQL replication lag high"
    description: "Replication lag is {{ $value }} seconds"
```

---

### 5. Disk I/O

**Metrics to Track:**

```sql
-- Table I/O statistics
SELECT
    schemaname,
    tablename,
    heap_blks_read,
    heap_blks_hit,
    idx_blks_read,
    idx_blks_hit,
    (heap_blks_hit::float / NULLIF(heap_blks_read + heap_blks_hit, 0)) * 100 AS cache_hit_ratio
FROM pg_statio_user_tables
ORDER BY heap_blks_read DESC
LIMIT 20;

-- Index usage
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan ASC
LIMIT 20;  -- Unused indexes
```

**System-Level Disk I/O:**

```bash
# Install iotop
apt-get install iotop

# Monitor disk I/O
iotop -o  # Show only processes doing I/O

# Check disk stats
iostat -x 5  # 5-second intervals
```

**Prometheus Metrics:**

```promql
# Disk read bytes/sec
rate(node_disk_read_bytes_total[5m])

# Disk write bytes/sec
rate(node_disk_written_bytes_total[5m])

# Disk I/O utilization
rate(node_disk_io_time_seconds_total[5m]) * 100

# Disk queue length
node_disk_io_now
```

---

## Cache Monitoring (Redis)

### 1. Hit/Miss Ratio

**Why Monitor Cache Hit Ratio:**
- Hit ratio < 70% = cache not effective
- Miss ratio high = poor cache strategy or insufficient memory
- Critical for performance (database load reduction)

**Redis Metrics:**

```bash
# Get Redis stats
redis-cli INFO stats

# Key metrics:
# - keyspace_hits: Number of successful key lookups
# - keyspace_misses: Number of failed key lookups
# - hit_rate = hits / (hits + misses) * 100
```

**Calculate Hit Ratio:**

```bash
redis-cli INFO stats | grep -E 'keyspace_hits|keyspace_misses' | awk '
BEGIN { hits=0; misses=0; }
/keyspace_hits/ { hits=$2; }
/keyspace_misses/ { misses=$2; }
END {
    total = hits + misses;
    if (total > 0) {
        hit_rate = (hits / total) * 100;
        printf "Hit Rate: %.2f%% (Hits: %d, Misses: %d)\n", hit_rate, hits, misses;
    }
}'
```

**Prometheus Exporter:**

```yaml
# Install redis-exporter
helm install redis-exporter prometheus-community/prometheus-redis-exporter \
  --set redisAddress=redis://redis:6379
```

**Grafana Dashboard Query:**

```promql
# Cache hit ratio
rate(redis_keyspace_hits_total[5m]) / (rate(redis_keyspace_hits_total[5m]) + rate(redis_keyspace_misses_total[5m])) * 100
```

**Alert Configuration:**

```yaml
- alert: RedisCacheHitRateLow
  expr: rate(redis_keyspace_hits_total[5m]) / (rate(redis_keyspace_hits_total[5m]) + rate(redis_keyspace_misses_total[5m])) * 100 < 70
  for: 10m
  labels:
    severity: medium
  annotations:
    summary: "Redis cache hit rate low"
    description: "Cache hit rate is {{ $value }}% (target: > 70%)"
```

---

### 2. Memory Usage

**Redis Memory Metrics:**

```bash
redis-cli INFO memory

# Key metrics:
# - used_memory: Total memory used (bytes)
# - used_memory_human: Human-readable format
# - used_memory_peak: Peak memory usage
# - maxmemory: Configured max memory
# - mem_fragmentation_ratio: Memory fragmentation (should be ~1.0)
```

**Memory Eviction Policy:**

```bash
# Check current policy
redis-cli CONFIG GET maxmemory-policy

# Recommended for cache: allkeys-lru
redis-cli CONFIG SET maxmemory-policy allkeys-lru

# Set max memory (4GB)
redis-cli CONFIG SET maxmemory 4294967296
```

**Monitor Memory Usage:**

```promql
# Memory usage percentage
redis_memory_used_bytes / redis_config_maxmemory * 100

# Evicted keys (should be low)
rate(redis_evicted_keys_total[5m])

# Memory fragmentation ratio (should be close to 1.0)
redis_memory_fragmentation_ratio
```

**Alerts:**

```yaml
- alert: RedisMemoryHigh
  expr: redis_memory_used_bytes / redis_config_maxmemory * 100 > 90
  for: 5m
  labels:
    severity: high
  annotations:
    summary: "Redis memory usage high"
    description: "Memory usage is {{ $value }}%"

- alert: RedisHighEvictionRate
  expr: rate(redis_evicted_keys_total[5m]) > 10
  for: 5m
  labels:
    severity: medium
  annotations:
    summary: "Redis evicting keys frequently"
    description: "Eviction rate is {{ $value }} keys/sec. Consider increasing memory."
```

---

### 3. Eviction Rate

**Why Monitor Evictions:**
- High eviction = not enough memory
- Indicates cache churn
- May need to increase Redis memory or optimize cache strategy

**Track Evictions:**

```bash
redis-cli INFO stats | grep evicted_keys

# Monitor real-time
redis-cli --stat
```

**Optimization Strategies:**

```csharp
// .NET: Set appropriate TTLs
await cache.SetStringAsync("employee:123", json, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),  // Short-lived data
    SlidingExpiration = TimeSpan.FromMinutes(30)              // Reset on access
});

// Long-lived data (reference data)
await cache.SetStringAsync("industry-sectors", json, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
});
```

---

## CDN Monitoring

### 1. Cache Hit Ratio

**CDN Metrics to Track:**

| Metric | Target | Impact |
|--------|--------|--------|
| **Cache Hit Ratio** | > 85% | Reduced origin load |
| **Bandwidth Savings** | > 80% | Cost reduction |
| **Origin Requests** | < 15% of total | Reduced backend load |
| **Cache Miss Rate** | < 15% | Improved performance |

**CloudFlare Analytics:**

```bash
# API to fetch metrics
curl -X GET "https://api.cloudflare.com/client/v4/zones/{zone_id}/analytics/dashboard" \
  -H "X-Auth-Email: your-email@example.com" \
  -H "X-Auth-Key: your-api-key"

# Key metrics in response:
# - requests.cached: Cache hits
# - requests.uncached: Cache misses
# - bandwidth.cached: Bandwidth saved
```

**Azure CDN Metrics:**

```bash
az monitor metrics list \
  --resource "/subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.Cdn/profiles/{profile}/endpoints/{endpoint}" \
  --metric-names "CacheHitRatio,OriginRequestCount,TotalLatency"
```

---

### 2. Origin Requests

**Why Monitor Origin Requests:**
- High origin requests = CDN not caching effectively
- Increased backend load
- Higher costs

**Optimize CDN Caching:**

```nginx
# Nginx configuration for CDN-friendly headers
location ~* \.(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}

location ~* \.(html)$ {
    expires 1h;
    add_header Cache-Control "public, must-revalidate";
}

# API responses (cache with caution)
location /api/public/ {
    expires 5m;
    add_header Cache-Control "public, max-age=300";
}
```

---

### 3. Edge Response Times

**Monitor CDN Latency:**

```bash
# Test CDN edge response time
curl -o /dev/null -s -w "Time: %{time_total}s\nConnect: %{time_connect}s\nTTFB: %{time_starttransfer}s\n" https://cdn.hrms.com/app.js

# Monitor from multiple locations
# Use: Pingdom, GTmetrix, WebPageTest
```

**Grafana Dashboard:**

```promql
# CDN response time (if using Prometheus exporter)
histogram_quantile(0.95, rate(cdn_request_duration_seconds_bucket[5m]))

# Requests by edge location
sum by (edge_location) (rate(cdn_requests_total[5m]))
```

---

## Network Monitoring

### Network Latency

**Monitor Network Latency Between Services:**

```bash
# Ping database from application pod
kubectl exec -it <api-pod> -- ping postgres-service

# Traceroute
kubectl exec -it <api-pod> -- traceroute postgres-service

# TCP connection test
kubectl exec -it <api-pod> -- nc -zv postgres-service 5432
```

**Service Mesh Metrics (Istio):**

```promql
# Request latency between services
histogram_quantile(0.95,
  sum(rate(istio_request_duration_milliseconds_bucket{source_app="hrms-api", destination_app="postgres"}[5m])) by (le)
)

# Network bytes sent/received
sum(rate(istio_tcp_sent_bytes_total[5m])) by (source_app, destination_app)
```

---

## Tool Recommendations

### Recommended Stack

**Option 1: Prometheus + Grafana (Open Source)**

| Component | Tool | Cost |
|-----------|------|------|
| Metrics Collection | Prometheus | Free |
| Visualization | Grafana | Free |
| Alerting | Alertmanager | Free |
| Node Monitoring | Node Exporter | Free |
| Postgres Monitoring | postgres-exporter | Free |
| Redis Monitoring | redis-exporter | Free |
| **Total** | | **$0** (infrastructure only) |

**Option 2: Datadog (Commercial)**

| Component | Coverage | Cost |
|-----------|----------|------|
| Infrastructure Monitoring | Servers, containers, K8s | $15/host/month |
| Database Monitoring | PostgreSQL, Redis | Included |
| Network Monitoring | NPM add-on | $5/host/month |
| Log Management | 15-day retention | $0.10/GB ingested |
| **Total (5 hosts)** | | **$100/month** |

**Option 3: Azure Monitor (Azure-native)**

| Component | Coverage | Cost |
|-----------|----------|------|
| Azure Monitor | VMs, AKS, databases | $2.88/GB ingested |
| Log Analytics | 31-day retention | Included |
| Alerts | Unlimited | $0.10/alert |
| **Total** | | **$200/month** (estimated) |

---

## Implementation Guide

### Quick Start: Prometheus + Grafana on Kubernetes

**1. Install Prometheus Stack**

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm install prometheus prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --set grafana.adminPassword=admin \
  --set prometheus.prometheusSpec.retention=15d
```

**2. Access Grafana**

```bash
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80

# Open browser: http://localhost:3000
# Login: admin / admin
```

**3. Import Dashboards**

Pre-built Grafana dashboards:
- **Kubernetes Cluster:** Dashboard ID 7249
- **PostgreSQL:** Dashboard ID 9628
- **Redis:** Dashboard ID 11835
- **Node Exporter:** Dashboard ID 1860

**4. Configure Alerts**

```bash
kubectl edit configmap -n monitoring prometheus-kube-prometheus-prometheus

# Add alert rules (see examples in previous sections)
```

---

## Cost Estimate

### Monthly Infrastructure Monitoring Costs

| Tier | Infrastructure | Tool | Cost |
|------|----------------|------|------|
| **Startup** (< 50 customers) | 3 nodes, 1 DB, 1 Redis | Prometheus + Grafana (self-hosted) | $50 (infra) |
| **Growing** (50-200 customers) | 10 nodes, 3 DB, 2 Redis | Datadog | $200/month |
| **Enterprise** (200+ customers) | 20+ nodes, HA DB, Redis cluster | Datadog Premium | $500/month |

---

**Document Owner:** Infrastructure Team
**Review Frequency:** Monthly
**Last Review:** November 17, 2025
