CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'tenant_default') THEN
        CREATE SCHEMA tenant_default;
    END IF;
END $EF$;

CREATE TABLE tenant_default."Departments" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Description" text,
    "ParentDepartmentId" uuid,
    "DepartmentHeadId" uuid,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Departments_Departments_ParentDepartmentId" FOREIGN KEY ("ParentDepartmentId") REFERENCES tenant_default."Departments" ("Id") ON DELETE RESTRICT
);

CREATE TABLE tenant_default."Employees" (
    "Id" uuid NOT NULL,
    "EmployeeCode" character varying(50) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "MiddleName" character varying(100) NOT NULL,
    "Email" character varying(100) NOT NULL,
    "PhoneNumber" character varying(20) NOT NULL,
    "PersonalEmail" text,
    "DateOfBirth" timestamp with time zone NOT NULL,
    "Gender" integer NOT NULL,
    "MaritalStatus" integer NOT NULL,
    "Address" character varying(500) NOT NULL,
    "City" character varying(100),
    "PostalCode" character varying(20),
    "EmployeeType" integer NOT NULL,
    "Nationality" character varying(100) NOT NULL,
    "CountryOfOrigin" character varying(100),
    "NationalIdCard" character varying(50),
    "PassportNumber" character varying(50),
    "PassportIssueDate" timestamp with time zone,
    "PassportExpiryDate" timestamp with time zone,
    "VisaType" integer,
    "VisaNumber" character varying(100),
    "VisaIssueDate" timestamp with time zone,
    "VisaExpiryDate" timestamp with time zone,
    "WorkPermitNumber" character varying(100),
    "WorkPermitExpiryDate" timestamp with time zone,
    "TaxResidentStatus" integer NOT NULL,
    "TaxIdNumber" character varying(50),
    "NPFNumber" character varying(50),
    "NSFNumber" character varying(50),
    "PRGFNumber" character varying(50),
    "IsNPFEligible" boolean NOT NULL,
    "IsNSFEligible" boolean NOT NULL,
    "JobTitle" character varying(200) NOT NULL,
    "DepartmentId" uuid NOT NULL,
    "ManagerId" uuid,
    "JoiningDate" timestamp with time zone NOT NULL,
    "ProbationEndDate" timestamp with time zone,
    "ConfirmationDate" timestamp with time zone,
    "ResignationDate" timestamp with time zone,
    "LastWorkingDate" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "ContractEndDate" timestamp with time zone,
    "BasicSalary" numeric(18,2) NOT NULL,
    "BankName" character varying(200),
    "BankAccountNumber" character varying(100),
    "BankBranch" character varying(200),
    "BankSwiftCode" character varying(50),
    "SalaryCurrency" character varying(10) NOT NULL,
    "AnnualLeaveBalance" numeric(10,2) NOT NULL,
    "SickLeaveBalance" numeric(10,2) NOT NULL,
    "CasualLeaveBalance" numeric(10,2) NOT NULL,
    "OffboardingReason" character varying(500),
    "IsOffboarded" boolean NOT NULL,
    "OffboardingDate" timestamp with time zone,
    "OffboardingNotes" character varying(2000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES tenant_default."Departments" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Employees_Employees_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES tenant_default."Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE tenant_default."EmergencyContacts" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "ContactName" character varying(200) NOT NULL,
    "PhoneNumber" character varying(20) NOT NULL,
    "AlternatePhoneNumber" character varying(20),
    "Email" character varying(100),
    "Relationship" character varying(100) NOT NULL,
    "ContactType" character varying(50) NOT NULL,
    "Address" character varying(500),
    "Country" character varying(100),
    "IsPrimary" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_EmergencyContacts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmergencyContacts_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_Departments_Code" ON tenant_default."Departments" ("Code");

CREATE INDEX "IX_Departments_DepartmentHeadId" ON tenant_default."Departments" ("DepartmentHeadId");

CREATE INDEX "IX_Departments_ParentDepartmentId" ON tenant_default."Departments" ("ParentDepartmentId");

CREATE INDEX "IX_EmergencyContacts_EmployeeId" ON tenant_default."EmergencyContacts" ("EmployeeId");

CREATE INDEX "IX_Employees_DepartmentId" ON tenant_default."Employees" ("DepartmentId");

CREATE UNIQUE INDEX "IX_Employees_Email" ON tenant_default."Employees" ("Email");

CREATE UNIQUE INDEX "IX_Employees_EmployeeCode" ON tenant_default."Employees" ("EmployeeCode");

CREATE INDEX "IX_Employees_ManagerId" ON tenant_default."Employees" ("ManagerId");

CREATE INDEX "IX_Employees_NationalIdCard" ON tenant_default."Employees" ("NationalIdCard");

CREATE INDEX "IX_Employees_PassportNumber" ON tenant_default."Employees" ("PassportNumber");

ALTER TABLE tenant_default."Departments" ADD CONSTRAINT "FK_Departments_Employees_DepartmentHeadId" FOREIGN KEY ("DepartmentHeadId") REFERENCES tenant_default."Employees" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251101014846_AddEmployeeAndEmergencyContact', '9.0.10');

CREATE TABLE tenant_default."LeaveEncashments" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "CalculationDate" timestamp with time zone NOT NULL,
    "LastWorkingDay" timestamp with time zone NOT NULL,
    "UnusedAnnualLeaveDays" numeric(10,2) NOT NULL,
    "UnusedSickLeaveDays" numeric(10,2) NOT NULL,
    "TotalEncashableDays" numeric(10,2) NOT NULL,
    "DailySalary" numeric(18,2) NOT NULL,
    "TotalEncashmentAmount" numeric(18,2) NOT NULL,
    "CalculationDetails" text,
    "IsPaid" boolean NOT NULL,
    "PaidDate" timestamp with time zone,
    "PaymentReference" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_LeaveEncashments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveEncashments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."LeaveTypes" (
    "Id" uuid NOT NULL,
    "TypeCode" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" text,
    "DefaultEntitlement" numeric(10,2) NOT NULL,
    "RequiresApproval" boolean NOT NULL,
    "IsPaid" boolean NOT NULL,
    "CanCarryForward" boolean NOT NULL,
    "MaxCarryForwardDays" integer NOT NULL,
    "RequiresDocumentation" boolean NOT NULL,
    "MinDaysNotice" integer NOT NULL,
    "MaxConsecutiveDays" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "ApprovalWorkflow" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id")
);

CREATE TABLE tenant_default."PublicHolidays" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "Year" integer NOT NULL,
    "Type" integer NOT NULL,
    "Description" text,
    "IsRecurring" boolean NOT NULL,
    "Country" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_PublicHolidays" PRIMARY KEY ("Id")
);

CREATE TABLE tenant_default."LeaveApplications" (
    "Id" uuid NOT NULL,
    "ApplicationNumber" character varying(50) NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LeaveTypeId" uuid NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "TotalDays" numeric(10,2) NOT NULL,
    "CalculationType" integer NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "ContactNumber" text,
    "ContactAddress" text,
    "Status" integer NOT NULL,
    "AppliedDate" timestamp with time zone,
    "ApprovedDate" timestamp with time zone,
    "ApprovedBy" uuid,
    "ApproverComments" text,
    "RejectedDate" timestamp with time zone,
    "RejectedBy" uuid,
    "RejectionReason" text,
    "CancelledDate" timestamp with time zone,
    "CancelledBy" uuid,
    "CancellationReason" text,
    "AttachmentPath" text,
    "RequiresHRApproval" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_LeaveApplications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveApplications_Employees_ApprovedBy" FOREIGN KEY ("ApprovedBy") REFERENCES tenant_default."Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveApplications_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveApplications_Employees_RejectedBy" FOREIGN KEY ("RejectedBy") REFERENCES tenant_default."Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveApplications_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES tenant_default."LeaveTypes" ("Id") ON DELETE RESTRICT
);

CREATE TABLE tenant_default."LeaveBalances" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LeaveTypeId" uuid NOT NULL,
    "Year" integer NOT NULL,
    "TotalEntitlement" numeric(10,2) NOT NULL,
    "UsedDays" numeric(10,2) NOT NULL,
    "PendingDays" numeric(10,2) NOT NULL,
    "CarriedForward" numeric(10,2) NOT NULL,
    "Accrued" numeric(10,2) NOT NULL,
    "LastAccrualDate" timestamp with time zone,
    "ExpiryDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_LeaveBalances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveBalances_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES tenant_default."LeaveTypes" ("Id") ON DELETE RESTRICT
);

CREATE TABLE tenant_default."LeaveApprovals" (
    "Id" uuid NOT NULL,
    "LeaveApplicationId" uuid NOT NULL,
    "ApprovalLevel" integer NOT NULL,
    "ApproverRole" character varying(100) NOT NULL,
    "ApproverId" uuid,
    "Status" integer NOT NULL,
    "ActionDate" timestamp with time zone,
    "Comments" text,
    "RequestedInfo" text,
    "IsCurrentLevel" boolean NOT NULL,
    "IsComplete" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_LeaveApprovals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveApprovals_Employees_ApproverId" FOREIGN KEY ("ApproverId") REFERENCES tenant_default."Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveApprovals_LeaveApplications_LeaveApplicationId" FOREIGN KEY ("LeaveApplicationId") REFERENCES tenant_default."LeaveApplications" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_LeaveApplications_ApplicationNumber" ON tenant_default."LeaveApplications" ("ApplicationNumber");

CREATE INDEX "IX_LeaveApplications_ApprovedBy" ON tenant_default."LeaveApplications" ("ApprovedBy");

CREATE INDEX "IX_LeaveApplications_EmployeeId_StartDate_EndDate" ON tenant_default."LeaveApplications" ("EmployeeId", "StartDate", "EndDate");

CREATE INDEX "IX_LeaveApplications_LeaveTypeId" ON tenant_default."LeaveApplications" ("LeaveTypeId");

CREATE INDEX "IX_LeaveApplications_RejectedBy" ON tenant_default."LeaveApplications" ("RejectedBy");

CREATE INDEX "IX_LeaveApprovals_ApproverId" ON tenant_default."LeaveApprovals" ("ApproverId");

CREATE INDEX "IX_LeaveApprovals_LeaveApplicationId_ApprovalLevel" ON tenant_default."LeaveApprovals" ("LeaveApplicationId", "ApprovalLevel");

CREATE UNIQUE INDEX "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year" ON tenant_default."LeaveBalances" ("EmployeeId", "LeaveTypeId", "Year");

CREATE INDEX "IX_LeaveBalances_LeaveTypeId" ON tenant_default."LeaveBalances" ("LeaveTypeId");

CREATE INDEX "IX_LeaveEncashments_EmployeeId" ON tenant_default."LeaveEncashments" ("EmployeeId");

CREATE INDEX "IX_LeaveTypes_TypeCode" ON tenant_default."LeaveTypes" ("TypeCode");

CREATE INDEX "IX_PublicHolidays_Date_Year" ON tenant_default."PublicHolidays" ("Date", "Year");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251101025137_AddLeaveManagementSystem', '9.0.10');

CREATE TABLE tenant_default."TenantCustomComplianceRules" (
    "Id" uuid NOT NULL,
    "SectorComplianceRuleId" integer NOT NULL,
    "IsUsingDefault" boolean NOT NULL,
    "CustomRuleConfig" jsonb,
    "Justification" text,
    "ApprovedByUserId" uuid,
    "ApprovedAt" timestamp with time zone,
    "RuleCategory" character varying(50),
    "RuleName" character varying(200),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_TenantCustomComplianceRules" PRIMARY KEY ("Id")
);

CREATE TABLE tenant_default."TenantSectorConfigurations" (
    "Id" uuid NOT NULL,
    "SectorId" integer NOT NULL,
    "SelectedAt" timestamp with time zone NOT NULL,
    "SelectedByUserId" uuid,
    "Notes" text,
    "SectorName" character varying(300),
    "SectorCode" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_TenantSectorConfigurations" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_TenantCustomComplianceRules_RuleCategory" ON tenant_default."TenantCustomComplianceRules" ("RuleCategory");

CREATE INDEX "IX_TenantCustomComplianceRules_SectorComplianceRuleId" ON tenant_default."TenantCustomComplianceRules" ("SectorComplianceRuleId");

CREATE INDEX "IX_TenantSectorConfigurations_SectorId" ON tenant_default."TenantSectorConfigurations" ("SectorId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251101031948_AddTenantSectorConfiguration', '9.0.10');

CREATE TABLE tenant_default."AttendanceMachines" (
    "Id" uuid NOT NULL,
    "MachineName" character varying(200) NOT NULL,
    "MachineId" character varying(100) NOT NULL,
    "IpAddress" character varying(50),
    "Location" character varying(200),
    "DepartmentId" uuid,
    "IsActive" boolean NOT NULL,
    "SerialNumber" character varying(100),
    "Model" character varying(100),
    "ZKTecoDeviceId" character varying(50),
    "Port" integer,
    "LastSyncAt" timestamp with time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_AttendanceMachines" PRIMARY KEY ("Id")
);

CREATE TABLE tenant_default."Attendances" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "CheckInTime" timestamp with time zone,
    "CheckOutTime" timestamp with time zone,
    "WorkingHours" numeric(10,2) NOT NULL,
    "OvertimeHours" numeric(10,2) NOT NULL,
    "Status" integer NOT NULL,
    "LateArrivalMinutes" integer,
    "EarlyDepartureMinutes" integer,
    "Remarks" character varying(500),
    "IsRegularized" boolean NOT NULL,
    "RegularizedBy" uuid,
    "RegularizedAt" timestamp with time zone,
    "ShiftId" uuid,
    "AttendanceMachineId" uuid,
    "OvertimeRate" numeric(10,2),
    "IsSunday" boolean NOT NULL,
    "IsPublicHoliday" boolean NOT NULL,
    "CreatedBy" text,
    "UpdatedBy" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_Attendances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Attendances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."AttendanceCorrections" (
    "Id" uuid NOT NULL,
    "AttendanceId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "RequestedBy" uuid NOT NULL,
    "OriginalCheckIn" timestamp with time zone,
    "OriginalCheckOut" timestamp with time zone,
    "CorrectedCheckIn" timestamp with time zone,
    "CorrectedCheckOut" timestamp with time zone,
    "Reason" character varying(500) NOT NULL,
    "Status" integer NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "RejectionReason" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_AttendanceCorrections" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AttendanceCorrections_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_default."Attendances" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AttendanceCorrections_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AttendanceCorrections_AttendanceId_Status" ON tenant_default."AttendanceCorrections" ("AttendanceId", "Status");

CREATE INDEX "IX_AttendanceCorrections_EmployeeId" ON tenant_default."AttendanceCorrections" ("EmployeeId");

CREATE INDEX "IX_AttendanceCorrections_RequestedBy" ON tenant_default."AttendanceCorrections" ("RequestedBy");

CREATE INDEX "IX_AttendanceMachines_IpAddress" ON tenant_default."AttendanceMachines" ("IpAddress");

CREATE INDEX "IX_AttendanceMachines_SerialNumber" ON tenant_default."AttendanceMachines" ("SerialNumber");

CREATE INDEX "IX_AttendanceMachines_ZKTecoDeviceId" ON tenant_default."AttendanceMachines" ("ZKTecoDeviceId");

CREATE INDEX "IX_Attendances_Date" ON tenant_default."Attendances" ("Date");

CREATE UNIQUE INDEX "IX_Attendances_EmployeeId_Date" ON tenant_default."Attendances" ("EmployeeId", "Date");

CREATE INDEX "IX_Attendances_Status" ON tenant_default."Attendances" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251101043736_AddAttendanceManagement', '9.0.10');

CREATE TABLE tenant_default."PayrollCycles" (
    "Id" uuid NOT NULL,
    "Month" integer NOT NULL,
    "Year" integer NOT NULL,
    "Status" integer NOT NULL,
    "TotalGrossSalary" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "TotalNetSalary" numeric(18,2) NOT NULL,
    "TotalNPFEmployee" numeric(18,2) NOT NULL,
    "TotalNPFEmployer" numeric(18,2) NOT NULL,
    "TotalNSFEmployee" numeric(18,2) NOT NULL,
    "TotalNSFEmployer" numeric(18,2) NOT NULL,
    "TotalCSGEmployee" numeric(18,2) NOT NULL,
    "TotalCSGEmployer" numeric(18,2) NOT NULL,
    "TotalPRGF" numeric(18,2) NOT NULL,
    "TotalTrainingLevy" numeric(18,2) NOT NULL,
    "TotalPAYE" numeric(18,2) NOT NULL,
    "TotalOvertimePay" numeric(18,2) NOT NULL,
    "ProcessedBy" uuid,
    "ProcessedAt" timestamp with time zone,
    "ApprovedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "PaymentDate" timestamp with time zone,
    "Notes" character varying(2000),
    "EmployeeCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_PayrollCycles" PRIMARY KEY ("Id")
);

CREATE TABLE tenant_default."SalaryComponents" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "ComponentType" integer NOT NULL,
    "ComponentName" character varying(200) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character varying(10) NOT NULL,
    "IsRecurring" boolean NOT NULL,
    "IsDeduction" boolean NOT NULL,
    "IsTaxable" boolean NOT NULL,
    "IncludeInStatutory" boolean NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "Description" character varying(1000),
    "CalculationMethod" character varying(50) NOT NULL,
    "PercentageBase" character varying(50),
    "CalculationOrder" integer NOT NULL,
    "RequiresApproval" boolean NOT NULL,
    "IsApproved" boolean NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_SalaryComponents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalaryComponents_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."Payslips" (
    "Id" uuid NOT NULL,
    "PayrollCycleId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Month" integer NOT NULL,
    "Year" integer NOT NULL,
    "PayslipNumber" character varying(100) NOT NULL,
    "BasicSalary" numeric(18,2) NOT NULL,
    "HousingAllowance" numeric(18,2) NOT NULL,
    "TransportAllowance" numeric(18,2) NOT NULL,
    "MealAllowance" numeric(18,2) NOT NULL,
    "MobileAllowance" numeric(18,2) NOT NULL,
    "OtherAllowances" numeric(18,2) NOT NULL,
    "OvertimeHours" numeric(10,2) NOT NULL,
    "OvertimePay" numeric(18,2) NOT NULL,
    "ThirteenthMonthBonus" numeric(18,2) NOT NULL,
    "LeaveEncashment" numeric(18,2) NOT NULL,
    "GratuityPayment" numeric(18,2) NOT NULL,
    "Commission" numeric(18,2) NOT NULL,
    "TotalGrossSalary" numeric(18,2) NOT NULL,
    "WorkingDays" integer NOT NULL,
    "ActualDaysWorked" integer NOT NULL,
    "PaidLeaveDays" numeric(10,2) NOT NULL,
    "UnpaidLeaveDays" numeric(10,2) NOT NULL,
    "LeaveDeductions" numeric(18,2) NOT NULL,
    "NPF_Employee" numeric(18,2) NOT NULL,
    "NSF_Employee" numeric(18,2) NOT NULL,
    "CSG_Employee" numeric(18,2) NOT NULL,
    "PAYE_Tax" numeric(18,2) NOT NULL,
    "NPF_Employer" numeric(18,2) NOT NULL,
    "NSF_Employer" numeric(18,2) NOT NULL,
    "CSG_Employer" numeric(18,2) NOT NULL,
    "PRGF_Contribution" numeric(18,2) NOT NULL,
    "TrainingLevy" numeric(18,2) NOT NULL,
    "LoanDeduction" numeric(18,2) NOT NULL,
    "AdvanceDeduction" numeric(18,2) NOT NULL,
    "MedicalInsurance" numeric(18,2) NOT NULL,
    "OtherDeductions" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "NetSalary" numeric(18,2) NOT NULL,
    "PaymentStatus" integer NOT NULL,
    "PaidAt" timestamp with time zone,
    "PaymentMethod" character varying(50),
    "PaymentReference" character varying(200),
    "BankAccountNumber" character varying(100),
    "Remarks" character varying(1000),
    "IsDelivered" boolean NOT NULL,
    "DeliveredAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_Payslips" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payslips_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Payslips_PayrollCycles_PayrollCycleId" FOREIGN KEY ("PayrollCycleId") REFERENCES tenant_default."PayrollCycles" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_PayrollCycles_Month_Year" ON tenant_default."PayrollCycles" ("Month", "Year");

CREATE INDEX "IX_PayrollCycles_Status" ON tenant_default."PayrollCycles" ("Status");

CREATE INDEX "IX_Payslips_EmployeeId_Month_Year" ON tenant_default."Payslips" ("EmployeeId", "Month", "Year");

CREATE INDEX "IX_Payslips_PaymentStatus" ON tenant_default."Payslips" ("PaymentStatus");

CREATE INDEX "IX_Payslips_PayrollCycleId" ON tenant_default."Payslips" ("PayrollCycleId");

CREATE UNIQUE INDEX "IX_Payslips_PayslipNumber" ON tenant_default."Payslips" ("PayslipNumber");

CREATE INDEX "IX_SalaryComponents_ComponentType" ON tenant_default."SalaryComponents" ("ComponentType");

CREATE INDEX "IX_SalaryComponents_EmployeeId" ON tenant_default."SalaryComponents" ("EmployeeId");

CREATE INDEX "IX_SalaryComponents_IsActive_EffectiveFrom" ON tenant_default."SalaryComponents" ("IsActive", "EffectiveFrom");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251101054015_AddPayrollManagement', '9.0.10');

ALTER TABLE tenant_default."Employees" ADD "AccessFailedCount" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."Employees" ADD "LockoutEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE tenant_default."Employees" ADD "LockoutEnd" timestamp with time zone;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251104042616_SyncTenantModelChanges', '9.0.10');

ALTER TABLE tenant_default."Employees" ADD "PasswordHash" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251104061546_AddPasswordHashToEmployee', '9.0.10');

ALTER TABLE tenant_default."Employees" ADD "AnnualLeaveDays" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."Employees" ADD "BloodGroup" text;

ALTER TABLE tenant_default."Employees" ADD "CSGNumber" text;

ALTER TABLE tenant_default."Employees" ADD "CarryForwardAllowed" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE tenant_default."Employees" ADD "CasualLeaveDays" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."Employees" ADD "CertificatesFilePath" text;

ALTER TABLE tenant_default."Employees" ADD "ContractFilePath" text;

ALTER TABLE tenant_default."Employees" ADD "Designation" text NOT NULL DEFAULT '';

ALTER TABLE tenant_default."Employees" ADD "EmploymentContractType" text NOT NULL DEFAULT '';

ALTER TABLE tenant_default."Employees" ADD "EmploymentStatus" text NOT NULL DEFAULT '';

ALTER TABLE tenant_default."Employees" ADD "HighestQualification" text;

ALTER TABLE tenant_default."Employees" ADD "HousingAllowance" numeric;

ALTER TABLE tenant_default."Employees" ADD "IdCopyFilePath" text;

ALTER TABLE tenant_default."Employees" ADD "IndustrySector" text NOT NULL DEFAULT '';

ALTER TABLE tenant_default."Employees" ADD "Languages" text;

ALTER TABLE tenant_default."Employees" ADD "MealAllowance" numeric;

ALTER TABLE tenant_default."Employees" ADD "Notes" text;

ALTER TABLE tenant_default."Employees" ADD "PaymentFrequency" text;

ALTER TABLE tenant_default."Employees" ADD "ProbationPeriodMonths" integer;

ALTER TABLE tenant_default."Employees" ADD "ResumeFilePath" text;

ALTER TABLE tenant_default."Employees" ADD "SickLeaveDays" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."Employees" ADD "Skills" text;

ALTER TABLE tenant_default."Employees" ADD "TransportAllowance" numeric;

ALTER TABLE tenant_default."Employees" ADD "University" text;

ALTER TABLE tenant_default."Employees" ADD "WorkLocation" text;

CREATE TABLE tenant_default."EmployeeDrafts" (
    "Id" uuid NOT NULL,
    "FormDataJson" text NOT NULL,
    "DraftName" text NOT NULL,
    "CompletionPercentage" integer NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedByName" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastEditedBy" uuid,
    "LastEditedByName" text,
    "LastEditedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "DeletedBy" text,
    CONSTRAINT "PK_EmployeeDrafts" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251105034809_ExpandedEmployeeModelAndDrafts', '9.0.10');

CREATE TABLE tenant_default."Timesheets" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PeriodType" integer NOT NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "TotalRegularHours" numeric(10,2) NOT NULL,
    "TotalOvertimeHours" numeric(10,2) NOT NULL,
    "TotalHolidayHours" numeric(10,2) NOT NULL,
    "TotalSickLeaveHours" numeric(10,2) NOT NULL,
    "TotalAnnualLeaveHours" numeric(10,2) NOT NULL,
    "TotalAbsentHours" numeric(10,2) NOT NULL,
    "Status" integer NOT NULL,
    "SubmittedAt" timestamp with time zone,
    "SubmittedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "ApprovedBy" uuid,
    "ApprovedByName" character varying(200),
    "RejectedAt" timestamp with time zone,
    "RejectedBy" uuid,
    "RejectionReason" character varying(1000),
    "IsLocked" boolean NOT NULL,
    "LockedAt" timestamp with time zone,
    "LockedBy" uuid,
    "Notes" character varying(2000),
    "TenantId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_Timesheets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Timesheets_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."TimesheetComments" (
    "Id" uuid NOT NULL,
    "TimesheetId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "UserName" character varying(200) NOT NULL,
    "Comment" character varying(2000) NOT NULL,
    "CommentedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_TimesheetComments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TimesheetComments_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES tenant_default."Timesheets" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."TimesheetEntries" (
    "Id" uuid NOT NULL,
    "TimesheetId" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "AttendanceId" uuid,
    "ClockInTime" timestamp with time zone,
    "ClockOutTime" timestamp with time zone,
    "BreakDuration" integer NOT NULL,
    "ActualHours" numeric(10,2) NOT NULL,
    "RegularHours" numeric(10,2) NOT NULL,
    "OvertimeHours" numeric(10,2) NOT NULL,
    "HolidayHours" numeric(10,2) NOT NULL,
    "SickLeaveHours" numeric(10,2) NOT NULL,
    "AnnualLeaveHours" numeric(10,2) NOT NULL,
    "IsAbsent" boolean NOT NULL,
    "IsHoliday" boolean NOT NULL,
    "IsWeekend" boolean NOT NULL,
    "IsOnLeave" boolean NOT NULL,
    "DayType" integer NOT NULL,
    "Notes" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_TimesheetEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TimesheetEntries_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_default."Attendances" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_TimesheetEntries_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES tenant_default."Timesheets" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."TimesheetAdjustments" (
    "Id" uuid NOT NULL,
    "TimesheetEntryId" uuid NOT NULL,
    "AdjustmentType" integer NOT NULL,
    "FieldName" character varying(100) NOT NULL,
    "OldValue" character varying(500),
    "NewValue" character varying(500),
    "Reason" character varying(1000) NOT NULL,
    "AdjustedBy" uuid NOT NULL,
    "AdjustedByName" character varying(200),
    "AdjustedAt" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedByName" character varying(200),
    "ApprovedAt" timestamp with time zone,
    "RejectionReason" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_TimesheetAdjustments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TimesheetAdjustments_TimesheetEntries_TimesheetEntryId" FOREIGN KEY ("TimesheetEntryId") REFERENCES tenant_default."TimesheetEntries" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_TimesheetAdjustments_AdjustedBy" ON tenant_default."TimesheetAdjustments" ("AdjustedBy");

CREATE INDEX "IX_TimesheetAdjustments_Status" ON tenant_default."TimesheetAdjustments" ("Status");

CREATE INDEX "IX_TimesheetAdjustments_TimesheetEntryId" ON tenant_default."TimesheetAdjustments" ("TimesheetEntryId");

CREATE INDEX "IX_TimesheetComments_CommentedAt" ON tenant_default."TimesheetComments" ("CommentedAt");

CREATE INDEX "IX_TimesheetComments_TimesheetId" ON tenant_default."TimesheetComments" ("TimesheetId");

CREATE INDEX "IX_TimesheetEntries_AttendanceId" ON tenant_default."TimesheetEntries" ("AttendanceId");

CREATE INDEX "IX_TimesheetEntries_Date" ON tenant_default."TimesheetEntries" ("Date");

CREATE INDEX "IX_TimesheetEntries_TimesheetId_Date" ON tenant_default."TimesheetEntries" ("TimesheetId", "Date");

CREATE INDEX "IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd" ON tenant_default."Timesheets" ("EmployeeId", "PeriodStart", "PeriodEnd");

CREATE INDEX "IX_Timesheets_PeriodStart_PeriodEnd" ON tenant_default."Timesheets" ("PeriodStart", "PeriodEnd");

CREATE INDEX "IX_Timesheets_Status" ON tenant_default."Timesheets" ("Status");

CREATE INDEX "IX_Timesheets_TenantId" ON tenant_default."Timesheets" ("TenantId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251105121448_AddTimesheetManagement', '9.0.10');

ALTER TABLE tenant_default."Payslips" ADD "IsCalculatedFromTimesheets" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE tenant_default."Payslips" ADD "TimesheetIdsJson" text;

ALTER TABLE tenant_default."Payslips" ADD "TimesheetsProcessed" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251105132015_AddPayslipTimesheetIntegration', '9.0.10');

ALTER TABLE tenant_default."AttendanceMachines" RENAME COLUMN "Location" TO "LegacyLocation";

ALTER TABLE tenant_default."Employees" ADD "BiometricEnrollmentDate" timestamp with time zone;

ALTER TABLE tenant_default."Employees" ADD "BiometricEnrollmentId" character varying(100);

ALTER TABLE tenant_default."Employees" ADD "PrimaryLocationId" uuid;

ALTER TABLE tenant_default."Departments" ADD "CostCenterCode" text;

ALTER TABLE tenant_default."Attendances" ADD "AuthorizationNote" character varying(1000);

ALTER TABLE tenant_default."Attendances" ADD "DeviceId" uuid;

ALTER TABLE tenant_default."Attendances" ADD "DeviceUserId" character varying(100);

ALTER TABLE tenant_default."Attendances" ADD "IsAuthorized" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE tenant_default."Attendances" ADD "LocationId" uuid;

ALTER TABLE tenant_default."Attendances" ADD "PunchSource" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE tenant_default."Attendances" ADD "VerificationMethod" character varying(50);

UPDATE tenant_default."AttendanceMachines" SET "Port" = 0 WHERE "Port" IS NULL;
ALTER TABLE tenant_default."AttendanceMachines" ALTER COLUMN "Port" SET NOT NULL;
ALTER TABLE tenant_default."AttendanceMachines" ALTER COLUMN "Port" SET DEFAULT 0;

ALTER TABLE tenant_default."AttendanceMachines" ADD "ConnectionMethod" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE tenant_default."AttendanceMachines" ADD "ConnectionTimeoutSeconds" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."AttendanceMachines" ADD "DeviceCode" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE tenant_default."AttendanceMachines" ADD "DeviceConfigJson" jsonb;

ALTER TABLE tenant_default."AttendanceMachines" ADD "DeviceStatus" character varying(50) NOT NULL DEFAULT '';

ALTER TABLE tenant_default."AttendanceMachines" ADD "DeviceType" character varying(100) NOT NULL DEFAULT '';

ALTER TABLE tenant_default."AttendanceMachines" ADD "FirmwareVersion" character varying(50);

ALTER TABLE tenant_default."AttendanceMachines" ADD "LastSyncRecordCount" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."AttendanceMachines" ADD "LastSyncStatus" character varying(50);

ALTER TABLE tenant_default."AttendanceMachines" ADD "LastSyncTime" timestamp with time zone;

ALTER TABLE tenant_default."AttendanceMachines" ADD "LocationId" uuid;

ALTER TABLE tenant_default."AttendanceMachines" ADD "MacAddress" character varying(50);

ALTER TABLE tenant_default."AttendanceMachines" ADD "OfflineAlertEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE tenant_default."AttendanceMachines" ADD "OfflineThresholdMinutes" integer NOT NULL DEFAULT 0;

ALTER TABLE tenant_default."AttendanceMachines" ADD "SyncEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE tenant_default."AttendanceMachines" ADD "SyncIntervalMinutes" integer NOT NULL DEFAULT 0;

CREATE TABLE tenant_default."DeviceSyncLogs" (
    "Id" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "SyncStartTime" timestamp with time zone NOT NULL,
    "SyncEndTime" timestamp with time zone,
    "SyncDurationSeconds" integer,
    "SyncStatus" character varying(50) NOT NULL,
    "RecordsFetched" integer NOT NULL,
    "RecordsProcessed" integer NOT NULL,
    "RecordsInserted" integer NOT NULL,
    "RecordsUpdated" integer NOT NULL,
    "RecordsSkipped" integer NOT NULL,
    "RecordsErrored" integer NOT NULL,
    "SyncMethod" character varying(50) NOT NULL,
    "DateRangeFrom" timestamp with time zone,
    "DateRangeTo" timestamp with time zone,
    "ErrorMessage" character varying(2000),
    "ErrorDetailsJson" jsonb,
    "InitiatedBy" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_DeviceSyncLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeviceSyncLogs_AttendanceMachines_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES tenant_default."AttendanceMachines" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."EmployeeDeviceAccesses" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "AccessType" character varying(50) NOT NULL,
    "AccessReason" character varying(500),
    "ValidFrom" timestamp with time zone,
    "ValidUntil" timestamp with time zone,
    "AllowedDaysJson" jsonb,
    "AllowedTimeStart" interval,
    "AllowedTimeEnd" interval,
    "IsActive" boolean NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_EmployeeDeviceAccesses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeDeviceAccesses_AttendanceMachines_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES tenant_default."AttendanceMachines" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EmployeeDeviceAccesses_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE tenant_default."Locations" (
    "Id" uuid NOT NULL,
    "LocationCode" character varying(50) NOT NULL,
    "LocationName" character varying(200) NOT NULL,
    "LocationType" character varying(100),
    "AddressLine1" character varying(500),
    "AddressLine2" character varying(500),
    "City" character varying(100),
    "Region" character varying(100),
    "PostalCode" character varying(20),
    "Country" character varying(100) NOT NULL,
    "Phone" character varying(20),
    "Email" character varying(100),
    "WorkingHoursJson" jsonb,
    "Timezone" character varying(100) NOT NULL,
    "LocationManagerId" uuid,
    "CapacityHeadcount" integer,
    "Latitude" numeric(10,8),
    "Longitude" numeric(11,8),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_Locations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Locations_Employees_LocationManagerId" FOREIGN KEY ("LocationManagerId") REFERENCES tenant_default."Employees" ("Id") ON DELETE SET NULL
);

CREATE TABLE tenant_default."AttendanceAnomalies" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "AttendanceId" uuid,
    "AnomalyType" character varying(100) NOT NULL,
    "AnomalySeverity" character varying(50) NOT NULL,
    "AnomalyDate" timestamp with time zone NOT NULL,
    "AnomalyTime" timestamp with time zone NOT NULL,
    "AnomalyDescription" character varying(1000),
    "AnomalyDetailsJson" jsonb,
    "DeviceId" uuid,
    "LocationId" uuid,
    "ExpectedLocationId" uuid,
    "ResolutionStatus" character varying(50) NOT NULL,
    "ResolutionDate" timestamp with time zone,
    "ResolutionNote" character varying(1000),
    "ResolvedBy" uuid,
    "NotificationSent" boolean NOT NULL,
    "NotificationSentAt" timestamp with time zone,
    "NotificationRecipientsJson" jsonb,
    "AutoResolved" boolean NOT NULL,
    "AutoResolutionRule" character varying(200),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    CONSTRAINT "PK_AttendanceAnomalies" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AttendanceAnomalies_AttendanceMachines_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES tenant_default."AttendanceMachines" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_AttendanceAnomalies_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_default."Attendances" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AttendanceAnomalies_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AttendanceAnomalies_Locations_ExpectedLocationId" FOREIGN KEY ("ExpectedLocationId") REFERENCES tenant_default."Locations" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_AttendanceAnomalies_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES tenant_default."Locations" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_Employees_PrimaryLocationId" ON tenant_default."Employees" ("PrimaryLocationId");

CREATE INDEX "IX_Attendances_DeviceId" ON tenant_default."Attendances" ("DeviceId");

CREATE INDEX "IX_Attendances_EmployeeId_DeviceId_Date" ON tenant_default."Attendances" ("EmployeeId", "DeviceId", "Date");

CREATE INDEX "IX_Attendances_LocationId" ON tenant_default."Attendances" ("LocationId");

CREATE UNIQUE INDEX "IX_AttendanceMachines_DeviceCode" ON tenant_default."AttendanceMachines" ("DeviceCode");

CREATE INDEX "IX_AttendanceMachines_LocationId" ON tenant_default."AttendanceMachines" ("LocationId");

CREATE INDEX "IX_AttendanceMachines_LocationId_DeviceStatus" ON tenant_default."AttendanceMachines" ("LocationId", "DeviceStatus");

CREATE INDEX "IX_AttendanceAnomalies_AnomalyDate" ON tenant_default."AttendanceAnomalies" ("AnomalyDate");

CREATE INDEX "IX_AttendanceAnomalies_AnomalyType_AnomalySeverity" ON tenant_default."AttendanceAnomalies" ("AnomalyType", "AnomalySeverity");

CREATE INDEX "IX_AttendanceAnomalies_AttendanceId" ON tenant_default."AttendanceAnomalies" ("AttendanceId");

CREATE INDEX "IX_AttendanceAnomalies_DeviceId" ON tenant_default."AttendanceAnomalies" ("DeviceId");

CREATE INDEX "IX_AttendanceAnomalies_EmployeeId" ON tenant_default."AttendanceAnomalies" ("EmployeeId");

CREATE INDEX "IX_AttendanceAnomalies_EmployeeId_AnomalyDate" ON tenant_default."AttendanceAnomalies" ("EmployeeId", "AnomalyDate");

CREATE INDEX "IX_AttendanceAnomalies_ExpectedLocationId" ON tenant_default."AttendanceAnomalies" ("ExpectedLocationId");

CREATE INDEX "IX_AttendanceAnomalies_LocationId" ON tenant_default."AttendanceAnomalies" ("LocationId");

CREATE INDEX "IX_AttendanceAnomalies_ResolutionStatus" ON tenant_default."AttendanceAnomalies" ("ResolutionStatus");

CREATE INDEX "IX_DeviceSyncLogs_DeviceId" ON tenant_default."DeviceSyncLogs" ("DeviceId");

CREATE INDEX "IX_DeviceSyncLogs_DeviceId_SyncStartTime" ON tenant_default."DeviceSyncLogs" ("DeviceId", "SyncStartTime");

CREATE INDEX "IX_DeviceSyncLogs_SyncStartTime" ON tenant_default."DeviceSyncLogs" ("SyncStartTime");

CREATE INDEX "IX_DeviceSyncLogs_SyncStatus" ON tenant_default."DeviceSyncLogs" ("SyncStatus");

CREATE INDEX "IX_EmployeeDeviceAccesses_AccessType" ON tenant_default."EmployeeDeviceAccesses" ("AccessType");

CREATE INDEX "IX_EmployeeDeviceAccesses_DeviceId" ON tenant_default."EmployeeDeviceAccesses" ("DeviceId");

CREATE INDEX "IX_EmployeeDeviceAccesses_EmployeeId_DeviceId_IsActive" ON tenant_default."EmployeeDeviceAccesses" ("EmployeeId", "DeviceId", "IsActive");

CREATE INDEX "IX_EmployeeDeviceAccesses_ValidFrom_ValidUntil" ON tenant_default."EmployeeDeviceAccesses" ("ValidFrom", "ValidUntil");

CREATE INDEX "IX_Locations_Latitude_Longitude" ON tenant_default."Locations" ("Latitude", "Longitude");

CREATE UNIQUE INDEX "IX_Locations_LocationCode" ON tenant_default."Locations" ("LocationCode");

CREATE INDEX "IX_Locations_LocationManagerId" ON tenant_default."Locations" ("LocationManagerId");

CREATE INDEX "IX_Locations_LocationName" ON tenant_default."Locations" ("LocationName");

CREATE INDEX "IX_Locations_LocationType" ON tenant_default."Locations" ("LocationType");

ALTER TABLE tenant_default."AttendanceMachines" ADD CONSTRAINT "FK_AttendanceMachines_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES tenant_default."Locations" ("Id") ON DELETE SET NULL;

ALTER TABLE tenant_default."Attendances" ADD CONSTRAINT "FK_Attendances_AttendanceMachines_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES tenant_default."AttendanceMachines" ("Id") ON DELETE SET NULL;

ALTER TABLE tenant_default."Attendances" ADD CONSTRAINT "FK_Attendances_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES tenant_default."Locations" ("Id") ON DELETE SET NULL;

ALTER TABLE tenant_default."Employees" ADD CONSTRAINT "FK_Employees_Locations_PrimaryLocationId" FOREIGN KEY ("PrimaryLocationId") REFERENCES tenant_default."Locations" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251106053857_AddMultiDeviceBiometricAttendanceSystem', '9.0.10');

ALTER TABLE tenant_default."Locations" RENAME COLUMN "State" TO "Region";

ALTER TABLE tenant_default."Locations" RENAME COLUMN "Address" TO "AddressLine1";

ALTER TABLE tenant_default."Locations" RENAME COLUMN "ContactPhone" TO "Phone";

ALTER TABLE tenant_default."Locations" RENAME COLUMN "ContactEmail" TO "Email";

ALTER TABLE tenant_default."Locations" ADD "AddressLine2" character varying(500);

ALTER TABLE tenant_default."Locations" ADD "District" character varying(100);

ALTER TABLE tenant_default."Locations" ADD "WorkingHoursJson" jsonb;

ALTER TABLE tenant_default."Locations" ADD "LocationManagerId" uuid;

ALTER TABLE tenant_default."Locations" ADD "CapacityHeadcount" integer;

ALTER TABLE tenant_default."Locations" ALTER COLUMN "LocationType" TYPE character varying(100);
ALTER TABLE tenant_default."Locations" ALTER COLUMN "LocationType" DROP NOT NULL;

ALTER TABLE tenant_default."Locations" ALTER COLUMN "Latitude" TYPE numeric(10,8);

ALTER TABLE tenant_default."Locations" ALTER COLUMN "Longitude" TYPE numeric(11,8);

ALTER TABLE tenant_default."Locations" DROP COLUMN "ContactPerson";

ALTER TABLE tenant_default."Locations" DROP COLUMN "IsHeadOffice";

CREATE INDEX "IX_Locations_District_tenant_default" ON tenant_default."Locations" ("District");

CREATE INDEX "IX_Locations_Region_tenant_default" ON tenant_default."Locations" ("Region");

CREATE INDEX "IX_Locations_LocationManagerId_tenant_default" ON tenant_default."Locations" ("LocationManagerId");

ALTER TABLE tenant_default."Locations" ADD CONSTRAINT "FK_Locations_Employees_LocationManagerId_tenant_default" FOREIGN KEY ("LocationManagerId") REFERENCES tenant_default."Employees" ("Id") ON DELETE SET NULL;

ALTER TABLE tenant_siraaj."Locations" RENAME COLUMN "State" TO "Region";

ALTER TABLE tenant_siraaj."Locations" RENAME COLUMN "Address" TO "AddressLine1";

ALTER TABLE tenant_siraaj."Locations" RENAME COLUMN "ContactPhone" TO "Phone";

ALTER TABLE tenant_siraaj."Locations" RENAME COLUMN "ContactEmail" TO "Email";

ALTER TABLE tenant_siraaj."Locations" ADD "AddressLine2" character varying(500);

ALTER TABLE tenant_siraaj."Locations" ADD "District" character varying(100);

ALTER TABLE tenant_siraaj."Locations" ADD "WorkingHoursJson" jsonb;

ALTER TABLE tenant_siraaj."Locations" ADD "LocationManagerId" uuid;

ALTER TABLE tenant_siraaj."Locations" ADD "CapacityHeadcount" integer;

ALTER TABLE tenant_siraaj."Locations" ALTER COLUMN "LocationType" TYPE character varying(100);
ALTER TABLE tenant_siraaj."Locations" ALTER COLUMN "LocationType" DROP NOT NULL;

ALTER TABLE tenant_siraaj."Locations" ALTER COLUMN "Latitude" TYPE numeric(10,8);

ALTER TABLE tenant_siraaj."Locations" ALTER COLUMN "Longitude" TYPE numeric(11,8);

ALTER TABLE tenant_siraaj."Locations" DROP COLUMN "ContactPerson";

ALTER TABLE tenant_siraaj."Locations" DROP COLUMN "IsHeadOffice";

CREATE INDEX "IX_Locations_District_tenant_siraaj" ON tenant_siraaj."Locations" ("District");

CREATE INDEX "IX_Locations_Region_tenant_siraaj" ON tenant_siraaj."Locations" ("Region");

CREATE INDEX "IX_Locations_LocationManagerId_tenant_siraaj" ON tenant_siraaj."Locations" ("LocationManagerId");

ALTER TABLE tenant_siraaj."Locations" ADD CONSTRAINT "FK_Locations_Employees_LocationManagerId_tenant_siraaj" FOREIGN KEY ("LocationManagerId") REFERENCES tenant_siraaj."Employees" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251106113723_AlignLocationSchemaAndAddMauritiusSupport', '9.0.10');

COMMIT;

