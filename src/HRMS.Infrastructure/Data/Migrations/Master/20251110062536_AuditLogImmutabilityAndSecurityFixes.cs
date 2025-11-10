using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AuditLogImmutabilityAndSecurityFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // CRITICAL FIX #1: Audit Log Immutability Trigger
            // Enforces immutability at database level for Fortune 500 compliance
            // ============================================

            // Create function to prevent audit log modifications
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION master.prevent_audit_log_modification()
                RETURNS TRIGGER AS $$
                BEGIN
                    -- Block UPDATE operations
                    IF TG_OP = 'UPDATE' THEN
                        RAISE EXCEPTION 'AUDIT_LOG_IMMUTABLE: Audit logs are immutable and cannot be modified. Log ID: %', OLD.""Id""
                            USING ERRCODE = '23502',
                                  HINT = 'Audit logs must remain unchanged for compliance. Create a new audit log entry instead.';
                    END IF;

                    -- Block DELETE operations
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'AUDIT_LOG_IMMUTABLE: Audit logs are immutable and cannot be deleted. Log ID: %', OLD.""Id""
                            USING ERRCODE = '23502',
                                  HINT = 'Audit logs must be retained for 10+ years for compliance. Use archival instead.';
                    END IF;

                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create trigger on AuditLogs table
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS audit_log_immutability_trigger ON master.""AuditLogs"";

                CREATE TRIGGER audit_log_immutability_trigger
                BEFORE UPDATE OR DELETE ON master.""AuditLogs""
                FOR EACH ROW
                EXECUTE FUNCTION master.prevent_audit_log_modification();
            ");

            // Add comment explaining the trigger
            migrationBuilder.Sql(@"
                COMMENT ON TRIGGER audit_log_immutability_trigger ON master.""AuditLogs""
                IS 'Enforces immutability of audit logs for Fortune 500 compliance. Blocks all UPDATE and DELETE operations.';

                COMMENT ON FUNCTION master.prevent_audit_log_modification()
                IS 'Prevents modification or deletion of audit logs to maintain compliance with Fortune 500 audit requirements';
            ");

            // ============================================
            // HIGH FIX #1: Add CorrelationId Index to SecurityAlerts
            // Improves performance when correlating alerts with audit logs
            // ============================================

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_CorrelationId",
                schema: "master",
                table: "SecurityAlerts",
                column: "CorrelationId");

            // Log this security enhancement in audit trail
            migrationBuilder.Sql(@"
                INSERT INTO master.""AuditLogs"" (
                    ""Id"", ""ActionType"", ""Category"", ""Severity"", ""EntityType"",
                    ""Success"", ""PerformedAt"", ""UserEmail"", ""Reason"",
                    ""AdditionalMetadata""
                ) VALUES (
                    gen_random_uuid(),
                    20, -- SYSTEM_CONFIGURATION_CHANGED
                    6,  -- SYSTEM_ADMINISTRATION
                    1,  -- INFO
                    'AuditLog',
                    true,
                    NOW(),
                    'system@hrms.com',
                    'Security fixes applied: audit log immutability trigger + CorrelationId index',
                    '{""trigger"": ""prevent_audit_log_modification"", ""operations"": [""UPDATE"", ""DELETE""], ""compliance"": ""Fortune 500"", ""index"": ""SecurityAlerts.CorrelationId""}'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop CorrelationId index
            migrationBuilder.DropIndex(
                name: "IX_SecurityAlerts_CorrelationId",
                schema: "master",
                table: "SecurityAlerts");

            // Drop trigger
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS audit_log_immutability_trigger ON master.""AuditLogs"";
            ");

            // Drop function
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS master.prevent_audit_log_modification();
            ");
        }
    }
}
