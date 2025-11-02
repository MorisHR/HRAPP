using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddPayrollManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollCycles",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalGrossSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNetSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNPFEmployee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNPFEmployer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNSFEmployee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNSFEmployer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCSGEmployee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCSGEmployer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPRGF = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTrainingLevy = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPAYE = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalOvertimePay = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProcessedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EmployeeCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryComponents",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentType = table.Column<int>(type: "integer", nullable: false),
                    ComponentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeduction = table.Column<bool>(type: "boolean", nullable: false),
                    IsTaxable = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeInStatutory = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CalculationMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PercentageBase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CalculationOrder = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryComponents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payslips",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    PayslipNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BasicSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HousingAllowance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransportAllowance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MealAllowance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MobileAllowance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OvertimePay = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ThirteenthMonthBonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LeaveEncashment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GratuityPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Commission = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalGrossSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WorkingDays = table.Column<int>(type: "integer", nullable: false),
                    ActualDaysWorked = table.Column<int>(type: "integer", nullable: false),
                    PaidLeaveDays = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    UnpaidLeaveDays = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LeaveDeductions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NPF_Employee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NSF_Employee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CSG_Employee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PAYE_Tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NPF_Employer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NSF_Employer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CSG_Employer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PRGF_Contribution = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TrainingLevy = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LoanDeduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AdvanceDeduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MedicalInsurance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDelivered = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payslips_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payslips_PayrollCycles_PayrollCycleId",
                        column: x => x.PayrollCycleId,
                        principalSchema: "tenant_default",
                        principalTable: "PayrollCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_Month_Year",
                schema: "tenant_default",
                table: "PayrollCycles",
                columns: new[] { "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_Status",
                schema: "tenant_default",
                table: "PayrollCycles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId_Month_Year",
                schema: "tenant_default",
                table: "Payslips",
                columns: new[] { "EmployeeId", "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PaymentStatus",
                schema: "tenant_default",
                table: "Payslips",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PayrollCycleId",
                schema: "tenant_default",
                table: "Payslips",
                column: "PayrollCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PayslipNumber",
                schema: "tenant_default",
                table: "Payslips",
                column: "PayslipNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryComponents_ComponentType",
                schema: "tenant_default",
                table: "SalaryComponents",
                column: "ComponentType");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryComponents_EmployeeId",
                schema: "tenant_default",
                table: "SalaryComponents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryComponents_IsActive_EffectiveFrom",
                schema: "tenant_default",
                table: "SalaryComponents",
                columns: new[] { "IsActive", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payslips",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "SalaryComponents",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "PayrollCycles",
                schema: "tenant_default");
        }
    }
}
