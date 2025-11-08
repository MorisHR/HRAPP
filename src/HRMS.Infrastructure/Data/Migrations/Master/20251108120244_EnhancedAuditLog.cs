using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class EnhancedAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AdditionalInfo",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PerformedBy",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.AlterTable(
                name: "AuditLogs",
                schema: "master",
                comment: "Production-grade audit log with 10+ year retention. IMMUTABLE - no UPDATE/DELETE allowed (enforced by DB triggers). Partitioned by PerformedAt (monthly). Meets Mauritius compliance requirements.");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "User agent string (browser/device information)",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                comment: "Tenant ID (null for SuperAdmin platform-level actions)",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PerformedAt",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Timestamp when action was performed (UTC)",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "OldValues",
                schema: "master",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true,
                comment: "Old values before change (JSON format)",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValues",
                schema: "master",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true,
                comment: "New values after change (JSON format)",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                comment: "IP address of the user (IPv4 or IPv6)",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Entity type affected (Employee, LeaveRequest, Payroll, etc.)",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "EntityId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                comment: "Entity ID affected (if applicable)",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionType",
                schema: "master",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Standardized action type (enum stored as integer)");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalMetadata",
                schema: "master",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true,
                comment: "Additional metadata in JSON format (flexible for future extensions)");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalReference",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Approval reference if action required approval");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Archival date (when moved to cold storage)");

            migrationBuilder.AddColumn<DateTime>(
                name: "BusinessDate",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Business date for payroll/leave actions");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "master",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "High-level category for filtering (enum stored as integer)");

            migrationBuilder.AddColumn<string>(
                name: "ChangedFields",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "Comma-separated list of changed field names");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                comment: "SHA256 checksum for tamper detection");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Correlation ID for distributed tracing");

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Parsed device information (mobile, desktop, tablet, OS, browser)");

            migrationBuilder.AddColumn<string>(
                name: "DocumentationLink",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Link to related documentation or policy");

            migrationBuilder.AddColumn<int>(
                name: "DurationMs",
                schema: "master",
                table: "AuditLogs",
                type: "integer",
                nullable: true,
                comment: "Action duration in milliseconds (for performance tracking)");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "Error message if action failed");

            migrationBuilder.AddColumn<string>(
                name: "Geolocation",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Geolocation information (city, country, coordinates)");

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                comment: "HTTP method (GET, POST, PUT, DELETE)");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "master",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Flag indicating if entry has been archived to cold storage");

            migrationBuilder.AddColumn<string>(
                name: "NetworkInfo",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Network information (ISP, organization)");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentActionId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                comment: "Parent action ID for multi-step operations");

            migrationBuilder.AddColumn<string>(
                name: "PolicyReference",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Policy reference that triggered this action");

            migrationBuilder.AddColumn<string>(
                name: "QueryString",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "Query string parameters");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "User-provided reason for the action");

            migrationBuilder.AddColumn<string>(
                name: "RequestPath",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Request path/endpoint");

            migrationBuilder.AddColumn<int>(
                name: "ResponseCode",
                schema: "master",
                table: "AuditLogs",
                type: "integer",
                nullable: true,
                comment: "HTTP response status code");

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Session ID for tracking related actions");

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                schema: "master",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Severity level for alerting (enum stored as integer)");

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                schema: "master",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Whether the action succeeded");

            migrationBuilder.AddColumn<string>(
                name: "TenantName",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Denormalized tenant name for reporting");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "User email address");

            migrationBuilder.AddColumn<string>(
                name: "UserFullName",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "User full name for audit trail readability");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                comment: "User ID who performed the action");

            migrationBuilder.AddColumn<string>(
                name: "UserRole",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "User role at time of action (SuperAdmin, TenantAdmin, HR, Manager, Employee)");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionType",
                schema: "master",
                table: "AuditLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                schema: "master",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category_PerformedAt",
                schema: "master",
                table: "AuditLogs",
                columns: new[] { "Category", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                schema: "master",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsArchived_PerformedAt",
                schema: "master",
                table: "AuditLogs",
                columns: new[] { "IsArchived", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SessionId",
                schema: "master",
                table: "AuditLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Severity",
                schema: "master",
                table: "AuditLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Success_PerformedAt",
                schema: "master",
                table: "AuditLogs",
                columns: new[] { "Success", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_Category_PerformedAt",
                schema: "master",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Category", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_PerformedAt",
                schema: "master",
                table: "AuditLogs",
                columns: new[] { "TenantId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "master",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_PerformedAt",
                schema: "master",
                table: "AuditLogs",
                columns: new[] { "UserId", "PerformedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActionType",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Category",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Category_PerformedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CorrelationId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IsArchived_PerformedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_SessionId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Severity",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Success_PerformedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_Category_PerformedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_PerformedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_PerformedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActionType",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AdditionalMetadata",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ApprovalReference",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BusinessDate",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ChangedFields",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Checksum",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DocumentationLink",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Geolocation",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NetworkInfo",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ParentActionId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PolicyReference",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "QueryString",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RequestPath",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ResponseCode",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Severity",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Success",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantName",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserFullName",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserRole",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.AlterTable(
                name: "AuditLogs",
                schema: "master",
                oldComment: "Production-grade audit log with 10+ year retention. IMMUTABLE - no UPDATE/DELETE allowed (enforced by DB triggers). Partitioned by PerformedAt (monthly). Meets Mauritius compliance requirements.");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "User agent string (browser/device information)");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Tenant ID (null for SuperAdmin platform-level actions)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PerformedAt",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Timestamp when action was performed (UTC)");

            migrationBuilder.AlterColumn<string>(
                name: "OldValues",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Old values before change (JSON format)");

            migrationBuilder.AlterColumn<string>(
                name: "NewValues",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "New values after change (JSON format)");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true,
                oldComment: "IP address of the user (IPv4 or IPv6)");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Entity type affected (Employee, LeaveRequest, Payroll, etc.)");

            migrationBuilder.AlterColumn<Guid>(
                name: "EntityId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Entity ID affected (if applicable)");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfo",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "master",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PerformedBy",
                schema: "master",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "master",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "master",
                table: "AuditLogs",
                type: "text",
                nullable: true);
        }
    }
}
