# Custom HRMS Metrics Exporter - Optimized for High-Throughput
FROM python:3.11-slim

WORKDIR /app

# Install dependencies
RUN pip install --no-cache-dir \
    prometheus_client==0.18.0 \
    psycopg2-binary==2.9.9 \
    python-dotenv==1.0.0

# Copy exporter code
COPY exporter.py /app/

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:9188/metrics')"

# Run exporter
CMD ["python", "-u", "exporter.py"]
