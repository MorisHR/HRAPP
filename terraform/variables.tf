# ════════════════════════════════════════════════════════════════════════════
# HRMS Fortune 500 - Terraform Variables
# ════════════════════════════════════════════════════════════════════════════

# ───────────────────────────────────────────────────────────────────────────
# PROJECT CONFIGURATION
# ───────────────────────────────────────────────────────────────────────────

variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "region" {
  description = "GCP region for resources"
  type        = string
  default     = "us-central1"
}

variable "zone" {
  description = "GCP zone for zonal resources"
  type        = string
  default     = "us-central1-a"
}

variable "environment" {
  description = "Environment name (production, staging, development)"
  type        = string
  default     = "production"

  validation {
    condition     = contains(["production", "staging", "development"], var.environment)
    error_message = "Environment must be one of: production, staging, development."
  }
}

variable "domain" {
  description = "Primary domain for the application"
  type        = string
}

# ───────────────────────────────────────────────────────────────────────────
# NETWORKING
# ───────────────────────────────────────────────────────────────────────────

variable "subnet_cidr" {
  description = "CIDR range for the subnet"
  type        = string
  default     = "10.0.0.0/24"
}

variable "vpc_connector_cidr" {
  description = "CIDR range for VPC Access Connector"
  type        = string
  default     = "10.8.0.0/28"
}

variable "redis_cidr" {
  description = "CIDR range for Redis instance"
  type        = string
  default     = "10.0.1.0/29"
}

# ───────────────────────────────────────────────────────────────────────────
# CLOUD SQL (PostgreSQL)
# ───────────────────────────────────────────────────────────────────────────

variable "db_tier" {
  description = "Cloud SQL instance tier"
  type        = string
  default     = "db-custom-4-16384"  # 4 vCPUs, 16GB RAM

  validation {
    condition     = can(regex("^db-(custom|standard|highmem)-", var.db_tier))
    error_message = "Database tier must be a valid Cloud SQL tier (e.g., db-custom-4-16384)."
  }
}

variable "db_disk_size" {
  description = "Cloud SQL disk size in GB"
  type        = number
  default     = 100

  validation {
    condition     = var.db_disk_size >= 10
    error_message = "Database disk size must be at least 10 GB."
  }
}

# ───────────────────────────────────────────────────────────────────────────
# CLOUD MEMORYSTORE (Redis)
# ───────────────────────────────────────────────────────────────────────────

variable "redis_memory_gb" {
  description = "Redis instance memory in GB"
  type        = number
  default     = 5

  validation {
    condition     = var.redis_memory_gb >= 1 && var.redis_memory_gb <= 300
    error_message = "Redis memory must be between 1 and 300 GB."
  }
}

# ───────────────────────────────────────────────────────────────────────────
# CLOUD RUN
# ───────────────────────────────────────────────────────────────────────────

variable "api_image" {
  description = "Docker image for the API (e.g., gcr.io/PROJECT_ID/hrms-api:latest)"
  type        = string
}

variable "api_cpu" {
  description = "CPU allocation for Cloud Run (e.g., '2', '4')"
  type        = string
  default     = "2"

  validation {
    condition     = contains(["1", "2", "4", "6", "8"], var.api_cpu)
    error_message = "API CPU must be one of: 1, 2, 4, 6, 8."
  }
}

variable "api_memory" {
  description = "Memory allocation for Cloud Run (e.g., '2Gi', '4Gi')"
  type        = string
  default     = "2Gi"

  validation {
    condition     = can(regex("^[0-9]+(Mi|Gi)$", var.api_memory))
    error_message = "API memory must be in format like '2Gi' or '512Mi'."
  }
}

variable "min_instances" {
  description = "Minimum number of Cloud Run instances"
  type        = number
  default     = 1

  validation {
    condition     = var.min_instances >= 0 && var.min_instances <= 100
    error_message = "Minimum instances must be between 0 and 100."
  }
}

variable "max_instances" {
  description = "Maximum number of Cloud Run instances"
  type        = number
  default     = 100

  validation {
    condition     = var.max_instances >= 1 && var.max_instances <= 1000
    error_message = "Maximum instances must be between 1 and 1000."
  }
}

# ───────────────────────────────────────────────────────────────────────────
# TAGS
# ───────────────────────────────────────────────────────────────────────────

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    application = "hrms"
    managed-by  = "terraform"
  }
}
