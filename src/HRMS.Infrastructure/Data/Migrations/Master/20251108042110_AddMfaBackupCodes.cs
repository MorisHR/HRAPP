using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddMfaBackupCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                schema: "master",
                table: "AdminUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackupCodes",
                schema: "master",
                table: "AdminUsers",
                type: "jsonb",
                nullable: true,
                comment: "JSON array of SHA256-hashed backup codes for MFA recovery");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupCodes",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
