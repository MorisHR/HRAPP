# HRMS Load Testing Suite

Complete load testing infrastructure for the Fortune 500 HRMS system using K6.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Test Scenarios](#test-scenarios)
- [Running Tests](#running-tests)
- [Understanding Results](#understanding-results)
- [Performance Targets](#performance-targets)
- [Troubleshooting](#troubleshooting)
- [CI/CD Integration](#cicd-integration)

---

## Overview

This test suite provides comprehensive performance testing for the HRMS system, including:

- **Authentication Testing**: Login flows for all user types
- **End-to-End Testing**: Complete user journeys
- **Stress Testing**: Finding system limits (10,000+ concurrent users)
- **Database Performance**: Query optimization validation
- **Cache Efficiency**: Redis performance testing

### Architecture

```
tests/load/
├── k6/
│   ├── config.js                    # Test configuration
│   ├── utils.js                     # Utility functions
│   ├── auth-test.js                 # Authentication tests
│   ├── end-to-end-test.js           # E2E user workflows
│   ├── stress-test.js               # 10K+ user stress test
│   └── database-cache-test.js       # DB & cache performance
├── scripts/
│   ├── run-all-tests.sh             # Run all test suites
│   ├── run-smoke-test.sh            # Quick validation
│   └── analyze-results.sh           # Results analysis
└── results/                         # Test results (gitignored)
```

---

## Prerequisites

### Required Tools

1. **K6** - Load testing tool
   ```bash
   # macOS
   brew install k6

   # Linux
   sudo gpg -k
   sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
     --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
   echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] \
     https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
   sudo apt-get update
   sudo apt-get install k6

   # Windows
   choco install k6
   ```

2. **jq** - JSON processor (for results analysis)
   ```bash
   # macOS
   brew install jq

   # Linux
   sudo apt-get install jq

   # Windows
   choco install jq
   ```

### Environment Setup

Create a `.env` file in `tests/load/`:

```bash
# API Configuration
API_URL=https://api.yourdomain.com

# Admin Credentials
ADMIN_EMAIL=admin@hrms.com
ADMIN_PASSWORD=Admin@123!

# Tenant Admin Credentials
TENANT_ADMIN_EMAIL=tenant@example.com
TENANT_ADMIN_PASSWORD=Tenant@123!

# Employee Credentials
EMPLOYEE_EMAIL=employee@example.com
EMPLOYEE_PASSWORD=Employee@123!
```

---

## Quick Start

### 1. Smoke Test (2 minutes)

Quick validation that the system is accessible and functional:

```bash
cd tests/load
k6 run --vus 1 --duration 1m k6/auth-test.js
```

Expected result: All checks should pass with < 1% error rate.

### 2. Load Test (15 minutes)

Realistic load testing with ramping users:

```bash
k6 run k6/end-to-end-test.js
```

Expected result:
- P95 latency < 1s
- Error rate < 1%
- Cache hit rate > 90%

### 3. Stress Test (40 minutes)

Find system breaking point:

```bash
k6 run k6/stress-test.js
```

Expected result:
- System should handle 500-1000 req/s
- Graceful degradation beyond that
- No cascading failures

---

## Test Scenarios

### Authentication Test (`auth-test.js`)

**Duration**: 5-10 minutes
**Virtual Users**: Up to 180 (across all scenarios)
**Tests**:
- SuperAdmin login/logout flows
- Tenant Admin authentication
- Employee authentication
- Token refresh mechanisms

**Key Metrics**:
- Login duration: P95 < 200ms
- Token refresh: P95 < 150ms
- Success rate: > 99%

**Run Command**:
```bash
k6 run k6/auth-test.js
```

---

### End-to-End Test (`end-to-end-test.js`)

**Duration**: 10-15 minutes
**Virtual Users**: Up to 145 (across workflows)
**Tests**:
- SuperAdmin: Tenant management, reports
- Tenant Admin: Employee CRUD, attendance tracking
- Employee: Dashboard, attendance, leave requests

**Key Metrics**:
- Dashboard load time: P95 < 2s
- Employee creation: P95 < 1s
- Attendance record: P95 < 500ms
- Report generation: P95 < 5s

**Run Command**:
```bash
k6 run k6/end-to-end-test.js
```

---

### Stress Test (`stress-test.js`)

**Duration**: 40-50 minutes
**Virtual Users**: Up to 15,000
**Request Rate**: Progressively increases to 2,000 req/s

**Test Phases**:
1. **Warmup** (50 req/s): Baseline performance
2. **Normal Load** (100 req/s): Expected production load
3. **High Load** (500 req/s): Peak hours simulation
4. **Extreme Load** (1,000 req/s): 2x peak capacity
5. **Breaking Point** (2,000 req/s): Find system limits
6. **Recovery**: Validate system recovery

**Key Metrics**:
- Success rate: > 85%
- P95 latency: < 5s (under stress)
- Error rate: < 10%
- No cascading failures

**Run Command**:
```bash
k6 run k6/stress-test.js --out json=results/stress-test-$(date +%Y%m%d-%H%M%S).json
```

---

### Database & Cache Test (`database-cache-test.js`)

**Duration**: 5-10 minutes
**Virtual Users**: Up to 140 (across scenarios)
**Tests**:
- Cache hit rate validation
- Simple query performance
- Complex queries with JOIN operations
- Aggregation query performance
- Cache invalidation

**Key Metrics**:
- Cache hit rate: > 90%
- Cache response: P95 < 10ms
- DB response: P95 < 100ms
- Complex queries: P95 < 1s
- Aggregations: P95 < 2s

**Run Command**:
```bash
k6 run k6/database-cache-test.js
```

---

## Running Tests

### Single Test Execution

```bash
# Basic execution
k6 run k6/auth-test.js

# With custom VUs and duration
k6 run --vus 50 --duration 5m k6/auth-test.js

# With results output
k6 run k6/auth-test.js --out json=results/auth-test.json

# With environment variables
API_URL=https://staging.api.com k6 run k6/auth-test.js
```

### Running All Tests

```bash
cd tests/load
./scripts/run-all-tests.sh
```

This will execute:
1. Auth test
2. End-to-end test
3. Database & cache test
4. Stress test (optional, long-running)

### Test Modes

You can specify different test modes in `config.js`:

- **smoke**: Minimal load (1 VU, 1min)
- **load**: Normal expected load
- **stress**: Push beyond limits
- **spike**: Sudden traffic increase
- **soak**: Sustained load (1 hour)
- **breakingPoint**: Find maximum capacity

```bash
# Run in smoke mode (quick validation)
TEST_MODE=smoke k6 run k6/end-to-end-test.js
```

---

## Understanding Results

### K6 Output

K6 provides real-time output during test execution:

```
✓ dashboard loaded
✓ employee created
✓ attendance recorded

checks.........................: 98.50% ✓ 1970      ✗ 30
data_received..................: 15 MB  50 kB/s
data_sent......................: 5.0 MB 17 kB/s
http_req_duration..............: avg=245ms  min=50ms  med=180ms  max=2.5s p(90)=450ms p(95)=650ms
http_req_failed................: 1.50%  ✓ 30       ✗ 1970
http_reqs......................: 2000   6.66/s
iteration_duration.............: avg=1.2s   min=800ms med=1.1s  max=5s
iterations.....................: 500    1.66/s
vus............................: 10     min=10     max=100
vus_max........................: 100    min=100    max=100
```

### Key Metrics

| Metric | Description | Good | Warning | Critical |
|--------|-------------|------|---------|----------|
| `http_req_duration` | Response time | P95 < 500ms | P95 < 1s | P95 > 2s |
| `http_req_failed` | Error rate | < 1% | < 5% | > 10% |
| `checks` | Validation pass rate | > 99% | > 95% | < 95% |
| `cache_hit_rate` | Cache efficiency | > 90% | > 80% | < 80% |
| `login_duration` | Auth performance | P95 < 200ms | P95 < 500ms | P95 > 1s |

### Custom Metrics

The test suite tracks additional metrics:

- `auth_duration`: Authentication time
- `dashboard_load_time`: Dashboard rendering
- `employee_creation_time`: Employee CRUD performance
- `attendance_record_time`: Attendance tracking
- `cache_hit_rate`: Redis cache efficiency
- `db_query_duration`: Database query performance
- `slow_queries`: Count of queries > 1s

### Analyzing JSON Results

```bash
# View summary statistics
cat results/stress-test.json | jq '.metrics | to_entries[] | select(.key == "http_req_duration") | .value'

# Count errors
cat results/stress-test.json | jq '[.metrics.http_req_failed.values.count] | add'

# Get P95 latency
cat results/stress-test.json | jq '.metrics.http_req_duration.values.p95'
```

---

## Performance Targets

### Production Readiness Criteria

| Scenario | Load | P95 Latency | P99 Latency | Error Rate | Min Pass Criteria |
|----------|------|-------------|-------------|------------|-------------------|
| **Auth** | 100 users | < 200ms | < 500ms | < 1% | ✅ Must pass |
| **Dashboard** | 500 users | < 1s | < 2s | < 1% | ✅ Must pass |
| **CRUD Operations** | 200 users | < 800ms | < 1.5s | < 1% | ✅ Must pass |
| **Reports** | 50 concurrent | < 5s | < 10s | < 2% | ⚠️ Should pass |
| **Stress (1K req/s)** | ~6K users | < 5s | < 10s | < 10% | ⚠️ Should pass |
| **Stress (2K req/s)** | ~10K users | < 10s | < 20s | < 20% | 💡 Nice to have |

### Cache Performance Targets

- **Cache Hit Rate**: > 90% for repeated reads
- **Cache Response Time**: P95 < 10ms
- **Cache Miss Penalty**: < 100ms additional latency

### Database Performance Targets

- **Simple Queries**: P95 < 100ms
- **Complex Queries (JOINs)**: P95 < 500ms
- **Aggregations**: P95 < 2s
- **Search Queries**: P95 < 200ms (with indexes)

---

## Troubleshooting

### Common Issues

#### 1. High Error Rate (> 5%)

**Symptoms**: Many failed HTTP requests

**Possible Causes**:
- Database connection pool exhausted
- API rate limiting triggered
- Server resource constraints (CPU/Memory)

**Diagnosis**:
```bash
# Check error distribution
k6 run k6/auth-test.js --out json=results.json
cat results.json | jq '.points[] | select(.metric == "http_req_failed") | .data'

# Review application logs
kubectl logs -f deployment/hrms-api --tail=100
```

**Solutions**:
- Increase database connection pool size
- Scale API instances
- Add caching layer
- Implement rate limiting on client side

---

#### 2. Slow Response Times (P95 > 2s)

**Symptoms**: High latency in test results

**Possible Causes**:
- Slow database queries
- N+1 query problems
- Inefficient caching
- Network latency

**Diagnosis**:
```bash
# Run database performance test
k6 run k6/database-cache-test.js

# Check for slow queries
psql -h localhost -d hrms_master -c "SELECT query, mean_exec_time, calls FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 20;"
```

**Solutions**:
- Add database indexes
- Optimize queries (use EXPLAIN ANALYZE)
- Enable query result caching
- Use connection pooling

---

#### 3. Low Cache Hit Rate (< 80%)

**Symptoms**: Cache hit rate below target

**Possible Causes**:
- Cache TTL too short
- Cache eviction policy too aggressive
- High write rate invalidating cache
- Insufficient cache memory

**Diagnosis**:
```bash
# Monitor Redis
redis-cli INFO stats

# Check cache metrics
k6 run k6/database-cache-test.js | grep cache_hit_rate
```

**Solutions**:
- Increase cache memory
- Adjust TTL values
- Implement smarter invalidation
- Use cache warming strategies

---

#### 4. Connection Timeouts

**Symptoms**: `dial tcp: i/o timeout` errors

**Possible Causes**:
- Too many concurrent connections
- Network infrastructure limits
- Firewall/load balancer limits

**Solutions**:
- Reduce concurrent VUs
- Increase connection timeout
- Scale infrastructure

---

## CI/CD Integration

### GitHub Actions

The test suite integrates with GitHub Actions for automated testing.

**Manual Trigger**:
```bash
# From GitHub UI: Actions → Load Tests → Run workflow
```

**Automatic Triggers**:
- Before production deployments
- Nightly performance regression tests
- After infrastructure changes

### Results Storage

Test results are automatically uploaded as artifacts:
- JSON results file
- HTML summary report
- Performance comparison vs. baseline

### Performance Regression Detection

The CI pipeline compares results against baseline:
- **P95 Latency**: Must not increase > 20%
- **Error Rate**: Must not increase > 2%
- **Throughput**: Must not decrease > 15%

---

## Best Practices

### Before Running Tests

1. **Verify System Health**
   ```bash
   curl https://api.yourdomain.com/health
   ```

2. **Check Resource Availability**
   - Database connections available
   - Redis memory sufficient
   - Application instances scaled appropriately

3. **Notify Team**
   - Inform team of load test schedule
   - Avoid testing during production traffic

### During Tests

1. **Monitor System Resources**
   - CPU utilization
   - Memory usage
   - Database connection pool
   - Network throughput

2. **Watch for Alerts**
   - Error rate spikes
   - Latency increases
   - Resource exhaustion

### After Tests

1. **Review Results**
   - Compare against targets
   - Identify bottlenecks
   - Document findings

2. **Create Action Items**
   - Performance optimization tasks
   - Infrastructure scaling needs
   - Code improvements

3. **Update Baselines**
   - Store successful test results
   - Update performance targets

---

## Test Data Cleanup

Load tests create test data. Clean up after testing:

```sql
-- Delete load test tenants
DELETE FROM tenants WHERE subdomain LIKE 'loadtest%';

-- Delete load test employees
DELETE FROM employees WHERE email LIKE '%loadtest.com';

-- Delete load test attendance records
DELETE FROM attendance WHERE created_at > 'LOAD_TEST_START_TIME';
```

---

## Support

- **Documentation**: See GCP_DEPLOYMENT_GUIDE.md
- **Issues**: File in GitHub Issues
- **Questions**: Contact platform team

---

**Last Updated**: 2025-11-22
**Version**: 1.0.0
**Maintained By**: Platform Team
