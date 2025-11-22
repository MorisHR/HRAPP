# ════════════════════════════════════════════════════════════════════════════
# HRMS Fortune 500 - Google Cloud Platform Infrastructure
# Terraform Configuration for Production Deployment
# ════════════════════════════════════════════════════════════════════════════
#
# This Terraform configuration deploys a production-ready HRMS system to GCP
# with enterprise-grade architecture, scalability, and security.
#
# Components:
# - Cloud SQL (PostgreSQL 15) with High Availability
# - Cloud Memorystore (Redis 7.0) with HA
# - Cloud Run (Managed containers with auto-scaling)
# - Cloud Load Balancer (Global HTTPS)
# - Cloud Storage (File uploads, backups)
# - Cloud Logging & Monitoring
# - IAM Roles & Security
#
# Estimated Cost: $500-800/month (based on GCP_DEPLOYMENT_GUIDE.md)
# ════════════════════════════════════════════════════════════════════════════

terraform {
  required_version = ">= 1.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
    google-beta = {
      source  = "hashicorp/google-beta"
      version = "~> 5.0"
    }
  }

  # Backend configuration for state management
  # Recommended: Use GCS bucket for team collaboration
  backend "gcs" {
    bucket = "hrms-terraform-state"  # Change to your bucket name
    prefix = "terraform/state"
  }
}

# ════════════════════════════════════════════════════════════════════════════
# PROVIDERS
# ════════════════════════════════════════════════════════════════════════════

provider "google" {
  project = var.project_id
  region  = var.region
  zone    = var.zone
}

provider "google-beta" {
  project = var.project_id
  region  = var.region
  zone    = var.zone
}

# ════════════════════════════════════════════════════════════════════════════
# DATA SOURCES
# ════════════════════════════════════════════════════════════════════════════

data "google_project" "project" {}

# ════════════════════════════════════════════════════════════════════════════
# NETWORKING
# ════════════════════════════════════════════════════════════════════════════

# VPC Network
resource "google_compute_network" "vpc" {
  name                    = "${var.environment}-hrms-vpc"
  auto_create_subnetworks = false
  description             = "VPC network for HRMS application"
}

# Subnet for Cloud SQL and Redis
resource "google_compute_subnetwork" "subnet" {
  name          = "${var.environment}-hrms-subnet"
  ip_cidr_range = var.subnet_cidr
  region        = var.region
  network       = google_compute_network.vpc.id

  private_ip_google_access = true

  log_config {
    aggregation_interval = "INTERVAL_5_SEC"
    flow_sampling        = 0.5
    metadata             = "INCLUDE_ALL_METADATA"
  }
}

# VPC Peering for Cloud SQL
resource "google_compute_global_address" "private_ip_address" {
  name          = "${var.environment}-hrms-private-ip"
  purpose       = "VPC_PEERING"
  address_type  = "INTERNAL"
  prefix_length = 16
  network       = google_compute_network.vpc.id
}

resource "google_service_networking_connection" "private_vpc_connection" {
  network                 = google_compute_network.vpc.id
  service                 = "servicenetworking.googleapis.com"
  reserved_peering_ranges = [google_compute_global_address.private_ip_address.name]
}

# Serverless VPC Access Connector (for Cloud Run to access VPC resources)
resource "google_vpc_access_connector" "connector" {
  name          = "${var.environment}-hrms-connector"
  region        = var.region
  network       = google_compute_network.vpc.name
  ip_cidr_range = var.vpc_connector_cidr

  min_instances = 2
  max_instances = 10
}

# ════════════════════════════════════════════════════════════════════════════
# CLOUD SQL (PostgreSQL) - PRODUCTION-GRADE WITH HA
# ════════════════════════════════════════════════════════════════════════════

resource "random_password" "db_password" {
  length  = 32
  special = true
}

resource "google_sql_database_instance" "postgres" {
  name             = "${var.environment}-hrms-db"
  database_version = "POSTGRES_15"
  region           = var.region

  deletion_protection = var.environment == "production" ? true : false

  settings {
    # Instance tier - Fortune 500 grade
    tier              = var.db_tier
    availability_type = var.environment == "production" ? "REGIONAL" : "ZONAL"
    disk_type         = "PD_SSD"
    disk_size         = var.db_disk_size
    disk_autoresize   = true

    # Backup configuration - Daily at 2 AM
    backup_configuration {
      enabled                        = true
      start_time                     = "02:00"
      point_in_time_recovery_enabled = true
      transaction_log_retention_days = 7
      backup_retention_settings {
        retained_backups = 30
        retention_unit   = "COUNT"
      }
    }

    # Maintenance window - Sunday 3 AM
    maintenance_window {
      day          = 7  # Sunday
      hour         = 3
      update_track = "stable"
    }

    # IP configuration
    ip_configuration {
      ipv4_enabled    = false  # Private IP only for security
      private_network = google_compute_network.vpc.id
      require_ssl     = true
    }

    # Database flags for performance
    database_flags {
      name  = "max_connections"
      value = "500"
    }
    database_flags {
      name  = "shared_buffers"
      value = "4194304"  # 4GB in 8KB pages
    }
    database_flags {
      name  = "effective_cache_size"
      value = "12582912"  # 12GB in 8KB pages
    }
    database_flags {
      name  = "maintenance_work_mem"
      value = "1048576"  # 1GB in KB
    }
    database_flags {
      name  = "checkpoint_completion_target"
      value = "0.9"
    }
    database_flags {
      name  = "wal_buffers"
      value = "16384"  # 16MB in KB
    }
    database_flags {
      name  = "default_statistics_target"
      value = "100"
    }
    database_flags {
      name  = "random_page_cost"
      value = "1.1"  # SSD optimization
    }
    database_flags {
      name  = "effective_io_concurrency"
      value = "200"  # SSD optimization
    }

    # Insights configuration
    insights_config {
      query_insights_enabled  = true
      query_plans_per_minute  = 5
      query_string_length     = 1024
      record_application_tags = true
    }
  }

  depends_on = [google_service_networking_connection.private_vpc_connection]
}

# Create master database
resource "google_sql_database" "master" {
  name     = "hrms_master"
  instance = google_sql_database_instance.postgres.name
}

# Create default tenant schema database (template)
resource "google_sql_database" "tenant_default" {
  name     = "hrms_tenant_default"
  instance = google_sql_database_instance.postgres.name
}

# Create postgres user
resource "google_sql_user" "postgres" {
  name     = "postgres"
  instance = google_sql_database_instance.postgres.name
  password = random_password.db_password.result
}

# ════════════════════════════════════════════════════════════════════════════
# CLOUD MEMORYSTORE (Redis) - HIGH AVAILABILITY
# ════════════════════════════════════════════════════════════════════════════

resource "google_redis_instance" "cache" {
  name               = "${var.environment}-hrms-cache"
  tier               = var.environment == "production" ? "STANDARD_HA" : "BASIC"
  memory_size_gb     = var.redis_memory_gb
  region             = var.region
  redis_version      = "REDIS_7_0"
  display_name       = "HRMS Redis Cache"
  reserved_ip_range  = var.redis_cidr

  authorized_network = google_compute_network.vpc.id
  connect_mode       = "PRIVATE_SERVICE_ACCESS"

  # High availability settings
  replica_count            = var.environment == "production" ? 1 : 0
  read_replicas_mode       = var.environment == "production" ? "READ_REPLICAS_ENABLED" : null

  # Maintenance policy
  maintenance_policy {
    weekly_maintenance_window {
      day = "SUNDAY"
      start_time {
        hours   = 3
        minutes = 0
      }
    }
  }

  # Redis configuration for performance
  redis_configs = {
    maxmemory-policy = "allkeys-lru"
    notify-keyspace-events = "Ex"
    timeout = "300"
  }

  depends_on = [google_service_networking_connection.private_vpc_connection]
}

# ════════════════════════════════════════════════════════════════════════════
# CLOUD STORAGE - FILE UPLOADS & BACKUPS
# ════════════════════════════════════════════════════════════════════════════

# Bucket for file uploads
resource "google_storage_bucket" "uploads" {
  name          = "${var.project_id}-${var.environment}-uploads"
  location      = var.region
  storage_class = "STANDARD"

  uniform_bucket_level_access = true

  versioning {
    enabled = true
  }

  lifecycle_rule {
    condition {
      age = 90
    }
    action {
      type          = "SetStorageClass"
      storage_class = "NEARLINE"
    }
  }

  lifecycle_rule {
    condition {
      age = 365
    }
    action {
      type          = "SetStorageClass"
      storage_class = "COLDLINE"
    }
  }

  cors {
    origin          = ["https://${var.domain}"]
    method          = ["GET", "HEAD", "PUT", "POST", "DELETE"]
    response_header = ["*"]
    max_age_seconds = 3600
  }
}

# Bucket for database backups
resource "google_storage_bucket" "backups" {
  name          = "${var.project_id}-${var.environment}-backups"
  location      = var.region
  storage_class = "NEARLINE"

  uniform_bucket_level_access = true

  versioning {
    enabled = true
  }

  lifecycle_rule {
    condition {
      age = 30
    }
    action {
      type          = "SetStorageClass"
      storage_class = "COLDLINE"
    }
  }

  lifecycle_rule {
    condition {
      age = 90
    }
    action {
      type          = "SetStorageClass"
      storage_class = "ARCHIVE"
    }
  }
}

# ════════════════════════════════════════════════════════════════════════════
# SECRET MANAGER - SECURE SECRETS STORAGE
# ════════════════════════════════════════════════════════════════════════════

# Database password
resource "google_secret_manager_secret" "db_password" {
  secret_id = "${var.environment}-db-password"

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "db_password" {
  secret      = google_secret_manager_secret.db_password.id
  secret_data = random_password.db_password.result
}

# JWT secret key
resource "random_password" "jwt_secret" {
  length  = 64
  special = true
}

resource "google_secret_manager_secret" "jwt_secret" {
  secret_id = "${var.environment}-jwt-secret"

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "jwt_secret" {
  secret      = google_secret_manager_secret.jwt_secret.id
  secret_data = random_password.jwt_secret.result
}

# Encryption key
resource "random_password" "encryption_key" {
  length  = 64
  special = false
}

resource "google_secret_manager_secret" "encryption_key" {
  secret_id = "${var.environment}-encryption-key"

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "encryption_key" {
  secret      = google_secret_manager_secret.encryption_key.id
  secret_data = random_password.encryption_key.result
}

# ════════════════════════════════════════════════════════════════════════════
# CLOUD RUN - SERVERLESS CONTAINERS
# ════════════════════════════════════════════════════════════════════════════

resource "google_cloud_run_v2_service" "api" {
  name     = "${var.environment}-hrms-api"
  location = var.region

  deletion_protection = var.environment == "production" ? true : false

  template {
    # Scaling configuration
    scaling {
      min_instance_count = var.min_instances
      max_instance_count = var.max_instances
    }

    # VPC access
    vpc_access {
      connector = google_vpc_access_connector.connector.id
      egress    = "PRIVATE_RANGES_ONLY"
    }

    # Container configuration
    containers {
      image = var.api_image

      # Resource limits - Fortune 500 grade
      resources {
        limits = {
          cpu    = var.api_cpu
          memory = var.api_memory
        }
        cpu_idle = true
      }

      # Environment variables
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "production" ? "Production" : "Staging"
      }

      env {
        name  = "ConnectionStrings__DefaultConnection"
        value = "Host=${google_sql_database_instance.postgres.private_ip_address};Database=hrms_master;Username=postgres;Password=$(DB_PASSWORD);SSL Mode=Require"
      }

      env {
        name = "DB_PASSWORD"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.db_password.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "JwtSettings__Secret"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.jwt_secret.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "EncryptionSettings__Key"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.encryption_key.secret_id
            version = "latest"
          }
        }
      }

      env {
        name  = "RedisSettings__ConnectionString"
        value = "${google_redis_instance.cache.host}:${google_redis_instance.cache.port}"
      }

      env {
        name  = "StorageSettings__BucketName"
        value = google_storage_bucket.uploads.name
      }

      # Health check
      startup_probe {
        http_get {
          path = "/health"
          port = 8080
        }
        initial_delay_seconds = 10
        timeout_seconds       = 5
        period_seconds        = 10
        failure_threshold     = 3
      }

      liveness_probe {
        http_get {
          path = "/health"
          port = 8080
        }
        initial_delay_seconds = 30
        timeout_seconds       = 5
        period_seconds        = 30
        failure_threshold     = 3
      }
    }

    # Service account
    service_account = google_service_account.api.email

    # Session affinity
    session_affinity = true
  }

  traffic {
    type    = "TRAFFIC_TARGET_ALLOCATION_TYPE_LATEST"
    percent = 100
  }

  depends_on = [
    google_sql_database_instance.postgres,
    google_redis_instance.cache,
    google_vpc_access_connector.connector
  ]
}

# Allow unauthenticated access to Cloud Run (will be protected by load balancer)
resource "google_cloud_run_v2_service_iam_member" "noauth" {
  location = google_cloud_run_v2_service.api.location
  name     = google_cloud_run_v2_service.api.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# ════════════════════════════════════════════════════════════════════════════
# IAM - SERVICE ACCOUNTS & PERMISSIONS
# ════════════════════════════════════════════════════════════════════════════

# Service account for Cloud Run
resource "google_service_account" "api" {
  account_id   = "${var.environment}-hrms-api"
  display_name = "HRMS API Service Account"
  description  = "Service account for HRMS Cloud Run service"
}

# Grant Cloud SQL Client role
resource "google_project_iam_member" "api_sql_client" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.api.email}"
}

# Grant Secret Manager accessor role
resource "google_secret_manager_secret_iam_member" "api_secret_accessor" {
  for_each = toset([
    google_secret_manager_secret.db_password.id,
    google_secret_manager_secret.jwt_secret.id,
    google_secret_manager_secret.encryption_key.id
  ])

  secret_id = each.value
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.api.email}"
}

# Grant Storage Object Admin role for uploads bucket
resource "google_storage_bucket_iam_member" "api_uploads" {
  bucket = google_storage_bucket.uploads.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.api.email}"
}

# Grant Storage Object Creator role for backups bucket
resource "google_storage_bucket_iam_member" "api_backups" {
  bucket = google_storage_bucket.backups.name
  role   = "roles/storage.objectCreator"
  member = "serviceAccount:${google_service_account.api.email}"
}

# ════════════════════════════════════════════════════════════════════════════
# CLOUD LOAD BALANCER - GLOBAL HTTPS
# ════════════════════════════════════════════════════════════════════════════

# Reserve static IP
resource "google_compute_global_address" "lb_ip" {
  name = "${var.environment}-hrms-lb-ip"
}

# Backend service pointing to Cloud Run
resource "google_compute_region_network_endpoint_group" "api_neg" {
  name                  = "${var.environment}-hrms-api-neg"
  network_endpoint_type = "SERVERLESS"
  region                = var.region

  cloud_run {
    service = google_cloud_run_v2_service.api.name
  }
}

resource "google_compute_backend_service" "api" {
  name                  = "${var.environment}-hrms-backend"
  protocol              = "HTTP"
  port_name             = "http"
  timeout_sec           = 30
  enable_cdn            = false

  backend {
    group = google_compute_region_network_endpoint_group.api_neg.id
  }

  log_config {
    enable      = true
    sample_rate = 1.0
  }
}

# URL map
resource "google_compute_url_map" "default" {
  name            = "${var.environment}-hrms-lb"
  default_service = google_compute_backend_service.api.id
}

# SSL certificate (managed)
resource "google_compute_managed_ssl_certificate" "default" {
  name = "${var.environment}-hrms-cert"

  managed {
    domains = [var.domain]
  }
}

# HTTPS proxy
resource "google_compute_target_https_proxy" "default" {
  name             = "${var.environment}-hrms-https-proxy"
  url_map          = google_compute_url_map.default.id
  ssl_certificates = [google_compute_managed_ssl_certificate.default.id]
}

# Forwarding rule
resource "google_compute_global_forwarding_rule" "default" {
  name                  = "${var.environment}-hrms-https-rule"
  ip_protocol           = "TCP"
  load_balancing_scheme = "EXTERNAL_MANAGED"
  port_range            = "443"
  target                = google_compute_target_https_proxy.default.id
  ip_address            = google_compute_global_address.lb_ip.id
}

# HTTP to HTTPS redirect
resource "google_compute_url_map" "http_redirect" {
  name = "${var.environment}-hrms-http-redirect"

  default_url_redirect {
    https_redirect         = true
    redirect_response_code = "MOVED_PERMANENTLY_DEFAULT"
    strip_query            = false
  }
}

resource "google_compute_target_http_proxy" "http_redirect" {
  name    = "${var.environment}-hrms-http-proxy"
  url_map = google_compute_url_map.http_redirect.id
}

resource "google_compute_global_forwarding_rule" "http_redirect" {
  name                  = "${var.environment}-hrms-http-rule"
  ip_protocol           = "TCP"
  load_balancing_scheme = "EXTERNAL_MANAGED"
  port_range            = "80"
  target                = google_compute_target_http_proxy.http_redirect.id
  ip_address            = google_compute_global_address.lb_ip.id
}
