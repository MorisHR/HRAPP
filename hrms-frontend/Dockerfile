# ============================================================================
# HRMS Frontend - Multi-Stage Production Dockerfile
# ============================================================================
# Purpose: Build optimized production-ready Angular application
# Strategy: Multi-stage build for minimal image size
# Base Image: nginx:alpine for production serving
# Security: Non-root user, minimal dependencies
# ============================================================================

# ============================================================================
# Stage 1: Build Environment
# ============================================================================
FROM node:18-alpine AS builder

# Set working directory
WORKDIR /app

# Install build dependencies
RUN apk add --no-cache \
    python3 \
    make \
    g++

# Copy package files
COPY package*.json ./

# Install dependencies with npm ci for reproducible builds
RUN npm ci --silent

# Copy source code
COPY . .

# Build application for production
RUN npm run build -- --configuration=production

# Verify build output
RUN ls -la dist/hrms-frontend/browser/ && \
    test -f dist/hrms-frontend/browser/index.html || exit 1

# ============================================================================
# Stage 2: Production Environment
# ============================================================================
FROM nginx:1.25-alpine

# Metadata
LABEL maintainer="DevOps Team <devops@hrms.example.com>"
LABEL version="1.0"
LABEL description="HRMS Frontend - Angular 20 Application"

# Install runtime dependencies
RUN apk add --no-cache \
    curl \
    tzdata \
    ca-certificates

# Set timezone
ENV TZ=UTC

# Create nginx user and group
RUN addgroup -g 101 -S nginx && \
    adduser -S -D -H -u 101 -h /var/cache/nginx -s /sbin/nologin -G nginx -g nginx nginx || true

# Remove default nginx configuration
RUN rm -rf /etc/nginx/conf.d/* /usr/share/nginx/html/*

# Copy custom nginx configuration
COPY nginx/nginx.conf /etc/nginx/nginx.conf
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

# Copy built application from builder stage
COPY --from=builder /app/dist/hrms-frontend/browser /usr/share/nginx/html

# Copy ngsw-config for service worker
COPY --from=builder /app/ngsw-config.json /usr/share/nginx/html/

# Set permissions
RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d && \
    touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid

# Create health check endpoint
RUN echo '{"status":"healthy","service":"hrms-frontend","timestamp":"'$(date -u +"%Y-%m-%dT%H:%M:%SZ")'"}' > /usr/share/nginx/html/health.json

# Switch to non-root user
USER nginx

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health.json || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
