# ════════════════════════════════════════════════════════════════════════════
# HRMS Fortune 500 - Terraform Outputs
# ════════════════════════════════════════════════════════════════════════════

# ───────────────────────────────────────────────────────────────────────────
# LOAD BALANCER
# ───────────────────────────────────────────────────────────────────────────

output "load_balancer_ip" {
  description = "Global load balancer IP address"
  value       = google_compute_global_address.lb_ip.address
}

output "application_url" {
  description = "Application URL"
  value       = "https://${var.domain}"
}

# ───────────────────────────────────────────────────────────────────────────
# CLOUD SQL
# ───────────────────────────────────────────────────────────────────────────

output "db_instance_name" {
  description = "Cloud SQL instance name"
  value       = google_sql_database_instance.postgres.name
}

output "db_connection_name" {
  description = "Cloud SQL connection name"
  value       = google_sql_database_instance.postgres.connection_name
}

output "db_private_ip" {
  description = "Cloud SQL private IP address"
  value       = google_sql_database_instance.postgres.private_ip_address
}

output "db_master_database" {
  description = "Master database name"
  value       = google_sql_database.master.name
}

output "db_tenant_database" {
  description = "Tenant template database name"
  value       = google_sql_database.tenant_default.name
}

# ───────────────────────────────────────────────────────────────────────────
# REDIS
# ───────────────────────────────────────────────────────────────────────────

output "redis_host" {
  description = "Redis instance host"
  value       = google_redis_instance.cache.host
}

output "redis_port" {
  description = "Redis instance port"
  value       = google_redis_instance.cache.port
}

output "redis_connection_string" {
  description = "Redis connection string"
  value       = "${google_redis_instance.cache.host}:${google_redis_instance.cache.port}"
}

# ───────────────────────────────────────────────────────────────────────────
# CLOUD RUN
# ───────────────────────────────────────────────────────────────────────────

output "cloud_run_service_name" {
  description = "Cloud Run service name"
  value       = google_cloud_run_v2_service.api.name
}

output "cloud_run_service_url" {
  description = "Cloud Run service URL"
  value       = google_cloud_run_v2_service.api.uri
}

# ───────────────────────────────────────────────────────────────────────────
# STORAGE
# ───────────────────────────────────────────────────────────────────────────

output "uploads_bucket_name" {
  description = "Uploads bucket name"
  value       = google_storage_bucket.uploads.name
}

output "uploads_bucket_url" {
  description = "Uploads bucket URL"
  value       = google_storage_bucket.uploads.url
}

output "backups_bucket_name" {
  description = "Backups bucket name"
  value       = google_storage_bucket.backups.name
}

output "backups_bucket_url" {
  description = "Backups bucket URL"
  value       = google_storage_bucket.backups.url
}

# ───────────────────────────────────────────────────────────────────────────
# SECRETS
# ───────────────────────────────────────────────────────────────────────────

output "secret_db_password_id" {
  description = "Database password secret ID"
  value       = google_secret_manager_secret.db_password.secret_id
}

output "secret_jwt_id" {
  description = "JWT secret ID"
  value       = google_secret_manager_secret.jwt_secret.secret_id
}

output "secret_encryption_id" {
  description = "Encryption key secret ID"
  value       = google_secret_manager_secret.encryption_key.secret_id
}

# ───────────────────────────────────────────────────────────────────────────
# NETWORKING
# ───────────────────────────────────────────────────────────────────────────

output "vpc_network_name" {
  description = "VPC network name"
  value       = google_compute_network.vpc.name
}

output "vpc_subnet_name" {
  description = "VPC subnet name"
  value       = google_compute_subnetwork.subnet.name
}

output "vpc_connector_name" {
  description = "VPC Access Connector name"
  value       = google_vpc_access_connector.connector.name
}

# ───────────────────────────────────────────────────────────────────────────
# IAM
# ───────────────────────────────────────────────────────────────────────────

output "service_account_email" {
  description = "API service account email"
  value       = google_service_account.api.email
}

# ───────────────────────────────────────────────────────────────────────────
# DNS CONFIGURATION INSTRUCTIONS
# ───────────────────────────────────────────────────────────────────────────

output "dns_instructions" {
  description = "DNS configuration instructions"
  value       = <<-EOT
    ════════════════════════════════════════════════════════════════════════════
    DNS CONFIGURATION REQUIRED
    ════════════════════════════════════════════════════════════════════════════

    Add the following A record to your DNS provider:

    Type: A
    Name: ${var.domain}
    Value: ${google_compute_global_address.lb_ip.address}
    TTL: 300 (5 minutes)

    After adding the DNS record, wait for propagation (usually 5-15 minutes).
    Then verify with: dig ${var.domain}

    SSL certificate will be automatically provisioned by Google once DNS is configured.
    This may take up to 15 minutes after DNS propagation.

    Monitor certificate status with:
    gcloud compute ssl-certificates describe ${var.environment}-hrms-cert
    ════════════════════════════════════════════════════════════════════════════
  EOT
}

# ───────────────────────────────────────────────────────────────────────────
# SUMMARY
# ───────────────────────────────────────────────────────────────────────────

output "deployment_summary" {
  description = "Deployment summary"
  value       = <<-EOT
    ════════════════════════════════════════════════════════════════════════════
    HRMS DEPLOYMENT SUMMARY
    ════════════════════════════════════════════════════════════════════════════

    Environment: ${var.environment}
    Region: ${var.region}

    Application URL: https://${var.domain}
    Load Balancer IP: ${google_compute_global_address.lb_ip.address}

    Database: ${google_sql_database_instance.postgres.name}
    Redis: ${google_redis_instance.cache.name}
    Cloud Run: ${google_cloud_run_v2_service.api.name}

    Next Steps:
    1. Configure DNS (see dns_instructions output)
    2. Wait for SSL certificate provisioning
    3. Run database migrations
    4. Deploy frontend to Cloud Storage/CDN
    5. Configure monitoring alerts

    Access secrets with:
    gcloud secrets versions access latest --secret="${google_secret_manager_secret.db_password.secret_id}"
    gcloud secrets versions access latest --secret="${google_secret_manager_secret.jwt_secret.secret_id}"

    View logs with:
    gcloud run services logs read ${google_cloud_run_v2_service.api.name} --region=${var.region}
    ════════════════════════════════════════════════════════════════════════════
  EOT
}
