--
-- PostgreSQL database dump
--

\restrict xAKxIzHH3nty7ONLa5fWACewt7Pp8I2bsY0YMemjfxuuZBGeEAKMnTEl06DKYf0

-- Dumped from database version 16.10 (Ubuntu 16.10-0ubuntu0.24.04.1)
-- Dumped by pg_dump version 16.10 (Ubuntu 16.10-0ubuntu0.24.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: hangfire; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA hangfire;


ALTER SCHEMA hangfire OWNER TO postgres;

--
-- Name: master; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA master;


ALTER SCHEMA master OWNER TO postgres;

--
-- Name: tenant_default; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA tenant_default;


ALTER SCHEMA tenant_default OWNER TO postgres;

--
-- Name: tenant_siraaj; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA tenant_siraaj;


ALTER SCHEMA tenant_siraaj OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: aggregatedcounter; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.aggregatedcounter (
    id bigint NOT NULL,
    key text NOT NULL,
    value bigint NOT NULL,
    expireat timestamp with time zone
);


ALTER TABLE hangfire.aggregatedcounter OWNER TO postgres;

--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.aggregatedcounter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.aggregatedcounter_id_seq OWNER TO postgres;

--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.aggregatedcounter_id_seq OWNED BY hangfire.aggregatedcounter.id;


--
-- Name: counter; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.counter (
    id bigint NOT NULL,
    key text NOT NULL,
    value bigint NOT NULL,
    expireat timestamp with time zone
);


ALTER TABLE hangfire.counter OWNER TO postgres;

--
-- Name: counter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.counter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.counter_id_seq OWNER TO postgres;

--
-- Name: counter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.counter_id_seq OWNED BY hangfire.counter.id;


--
-- Name: hash; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.hash (
    id bigint NOT NULL,
    key text NOT NULL,
    field text NOT NULL,
    value text,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.hash OWNER TO postgres;

--
-- Name: hash_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.hash_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.hash_id_seq OWNER TO postgres;

--
-- Name: hash_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.hash_id_seq OWNED BY hangfire.hash.id;


--
-- Name: job; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.job (
    id bigint NOT NULL,
    stateid bigint,
    statename text,
    invocationdata jsonb NOT NULL,
    arguments jsonb NOT NULL,
    createdat timestamp with time zone NOT NULL,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.job OWNER TO postgres;

--
-- Name: job_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.job_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.job_id_seq OWNER TO postgres;

--
-- Name: job_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.job_id_seq OWNED BY hangfire.job.id;


--
-- Name: jobparameter; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.jobparameter (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    name text NOT NULL,
    value text,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.jobparameter OWNER TO postgres;

--
-- Name: jobparameter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.jobparameter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.jobparameter_id_seq OWNER TO postgres;

--
-- Name: jobparameter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.jobparameter_id_seq OWNED BY hangfire.jobparameter.id;


--
-- Name: jobqueue; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.jobqueue (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    queue text NOT NULL,
    fetchedat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.jobqueue OWNER TO postgres;

--
-- Name: jobqueue_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.jobqueue_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.jobqueue_id_seq OWNER TO postgres;

--
-- Name: jobqueue_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.jobqueue_id_seq OWNED BY hangfire.jobqueue.id;


--
-- Name: list; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.list (
    id bigint NOT NULL,
    key text NOT NULL,
    value text,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.list OWNER TO postgres;

--
-- Name: list_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.list_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.list_id_seq OWNER TO postgres;

--
-- Name: list_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.list_id_seq OWNED BY hangfire.list.id;


--
-- Name: lock; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.lock (
    resource text NOT NULL,
    updatecount integer DEFAULT 0 NOT NULL,
    acquired timestamp with time zone
);


ALTER TABLE hangfire.lock OWNER TO postgres;

--
-- Name: schema; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.schema (
    version integer NOT NULL
);


ALTER TABLE hangfire.schema OWNER TO postgres;

--
-- Name: server; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.server (
    id text NOT NULL,
    data jsonb,
    lastheartbeat timestamp with time zone NOT NULL,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.server OWNER TO postgres;

--
-- Name: set; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.set (
    id bigint NOT NULL,
    key text NOT NULL,
    score double precision NOT NULL,
    value text NOT NULL,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.set OWNER TO postgres;

--
-- Name: set_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.set_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.set_id_seq OWNER TO postgres;

--
-- Name: set_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.set_id_seq OWNED BY hangfire.set.id;


--
-- Name: state; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.state (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    name text NOT NULL,
    reason text,
    createdat timestamp with time zone NOT NULL,
    data jsonb,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.state OWNER TO postgres;

--
-- Name: state_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.state_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE hangfire.state_id_seq OWNER TO postgres;

--
-- Name: state_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.state_id_seq OWNED BY hangfire.state.id;


--
-- Name: AdminUsers; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."AdminUsers" (
    "Id" uuid NOT NULL,
    "UserName" character varying(100) NOT NULL,
    "Email" character varying(100) NOT NULL,
    "PasswordHash" text NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "LastLoginDate" timestamp with time zone,
    "PhoneNumber" character varying(20),
    "IsTwoFactorEnabled" boolean NOT NULL,
    "TwoFactorSecret" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    "AccessFailedCount" integer DEFAULT 0,
    "LockoutEnd" timestamp with time zone,
    "LockoutEnabled" boolean DEFAULT false
);


ALTER TABLE master."AdminUsers" OWNER TO postgres;

--
-- Name: AuditLogs; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."AuditLogs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid,
    "Action" character varying(100) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid,
    "OldValues" text,
    "NewValues" text,
    "PerformedBy" character varying(100) NOT NULL,
    "PerformedAt" timestamp with time zone NOT NULL,
    "IpAddress" character varying(50),
    "UserAgent" text,
    "AdditionalInfo" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."AuditLogs" OWNER TO postgres;

--
-- Name: IndustrySectors; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."IndustrySectors" (
    "Id" integer NOT NULL,
    "SectorCode" character varying(100) NOT NULL,
    "SectorName" character varying(300) NOT NULL,
    "SectorNameFrench" character varying(300),
    "Description" text,
    "ParentSectorId" integer,
    "RemunerationOrderReference" character varying(200),
    "RemunerationOrderYear" integer,
    "IsActive" boolean NOT NULL,
    "RequiresSpecialPermits" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE master."IndustrySectors" OWNER TO postgres;

--
-- Name: IndustrySectors_Id_seq; Type: SEQUENCE; Schema: master; Owner: postgres
--

ALTER TABLE master."IndustrySectors" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME master."IndustrySectors_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: SectorComplianceRules; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."SectorComplianceRules" (
    "Id" integer NOT NULL,
    "SectorId" integer NOT NULL,
    "RuleCategory" character varying(50) NOT NULL,
    "RuleName" character varying(200) NOT NULL,
    "RuleConfig" jsonb NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone,
    "LegalReference" text,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE master."SectorComplianceRules" OWNER TO postgres;

--
-- Name: SectorComplianceRules_Id_seq; Type: SEQUENCE; Schema: master; Owner: postgres
--

ALTER TABLE master."SectorComplianceRules" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME master."SectorComplianceRules_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Tenants; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."Tenants" (
    "Id" uuid NOT NULL,
    "CompanyName" character varying(200) NOT NULL,
    "Subdomain" character varying(50) NOT NULL,
    "SchemaName" character varying(50) NOT NULL,
    "ContactEmail" character varying(100) NOT NULL,
    "ContactPhone" character varying(20) NOT NULL,
    "Status" integer NOT NULL,
    "MaxStorageGB" integer NOT NULL,
    "MaxUsers" integer NOT NULL,
    "EmployeeTier" integer NOT NULL,
    "SuspensionReason" text,
    "SuspensionDate" timestamp with time zone,
    "SoftDeleteDate" timestamp with time zone,
    "DeletionReason" text,
    "GracePeriodDays" integer NOT NULL,
    "SubscriptionStartDate" timestamp with time zone NOT NULL,
    "SubscriptionEndDate" timestamp with time zone,
    "TrialEndDate" timestamp with time zone,
    "AdminUserName" character varying(100) NOT NULL,
    "AdminEmail" character varying(100) NOT NULL,
    "CurrentUserCount" integer NOT NULL,
    "SectorId" integer,
    "SectorSelectedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text,
    "ApiCallsPerMonth" integer DEFAULT 0 NOT NULL,
    "CurrentStorageGB" integer DEFAULT 0 NOT NULL,
    "MonthlyPrice" numeric DEFAULT 0.0 NOT NULL
);


ALTER TABLE master."Tenants" OWNER TO postgres;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE public."__EFMigrationsHistory" OWNER TO postgres;

--
-- Name: AttendanceAnomalies; Type: TABLE; Schema: tenant_default; Owner: postgres
--

CREATE TABLE tenant_default."AttendanceAnomalies" (
    "Id" uuid NOT NULL,
    "AttendanceId" uuid,
    "EmployeeId" uuid NOT NULL,
    "DeviceId" uuid,
    "LocationId" uuid,
    "AnomalyType" character varying(50) NOT NULL,
    "AnomalyDate" timestamp with time zone NOT NULL,
    "AnomalySeverity" character varying(20) NOT NULL,
    "Description" character varying(1000),
    "DetectionMethod" character varying(50) NOT NULL,
    "IsResolved" boolean DEFAULT false NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "ResolvedBy" uuid,
    "ResolutionNote" character varying(1000),
    "ActionTaken" character varying(500),
    "AnomalyDetailsJson" jsonb,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_default."AttendanceAnomalies" OWNER TO postgres;

--
-- Name: AttendanceCorrections; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."AttendanceCorrections" OWNER TO postgres;

--
-- Name: AttendanceMachines; Type: TABLE; Schema: tenant_default; Owner: postgres
--

CREATE TABLE tenant_default."AttendanceMachines" (
    "Id" uuid NOT NULL,
    "MachineName" character varying(200) NOT NULL,
    "MachineId" character varying(100) NOT NULL,
    "IpAddress" character varying(50),
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
    "LegacyLocation" text,
    "DeviceCode" character varying(50) DEFAULT ''::character varying,
    "DeviceType" character varying(100) DEFAULT 'ZKTeco'::character varying,
    "LocationId" uuid,
    "MacAddress" character varying(50),
    "FirmwareVersion" character varying(50),
    "ConnectionMethod" character varying(50) DEFAULT 'TCP/IP'::character varying,
    "ConnectionTimeoutSeconds" integer DEFAULT 30,
    "DeviceConfigJson" jsonb,
    "DeviceStatus" character varying(50) DEFAULT 'Active'::character varying,
    "SyncEnabled" boolean DEFAULT true,
    "SyncIntervalMinutes" integer DEFAULT 15,
    "LastSyncTime" timestamp with time zone,
    "LastSyncStatus" character varying(50),
    "LastSyncRecordCount" integer DEFAULT 0,
    "OfflineAlertEnabled" boolean DEFAULT true,
    "OfflineThresholdMinutes" integer DEFAULT 60
);


ALTER TABLE tenant_default."AttendanceMachines" OWNER TO postgres;

--
-- Name: Attendances; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeviceId" uuid,
    "LocationId" uuid,
    "DeviceUserId" character varying(100),
    "PunchSource" character varying(50) DEFAULT 'Biometric'::character varying,
    "VerificationMethod" character varying(50),
    "IsAuthorized" boolean DEFAULT true,
    "AuthorizationNote" character varying(1000)
);


ALTER TABLE tenant_default."Attendances" OWNER TO postgres;

--
-- Name: Departments; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "CostCenterCode" text
);


ALTER TABLE tenant_default."Departments" OWNER TO postgres;

--
-- Name: DeviceSyncLogs; Type: TABLE; Schema: tenant_default; Owner: postgres
--

CREATE TABLE tenant_default."DeviceSyncLogs" (
    "Id" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "SyncStartTime" timestamp with time zone NOT NULL,
    "SyncEndTime" timestamp with time zone,
    "SyncDurationSeconds" integer,
    "SyncStatus" character varying(50) NOT NULL,
    "RecordsFetched" integer DEFAULT 0 NOT NULL,
    "RecordsProcessed" integer DEFAULT 0 NOT NULL,
    "RecordsInserted" integer DEFAULT 0 NOT NULL,
    "RecordsUpdated" integer DEFAULT 0 NOT NULL,
    "RecordsSkipped" integer DEFAULT 0 NOT NULL,
    "RecordsErrored" integer DEFAULT 0 NOT NULL,
    "SyncMethod" character varying(50) NOT NULL,
    "DateRangeFrom" timestamp with time zone,
    "DateRangeTo" timestamp with time zone,
    "ErrorMessage" character varying(2000),
    "ErrorDetailsJson" jsonb,
    "InitiatedBy" uuid,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_default."DeviceSyncLogs" OWNER TO postgres;

--
-- Name: EmergencyContacts; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."EmergencyContacts" OWNER TO postgres;

--
-- Name: EmployeeDeviceAccesses; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "IsActive" boolean DEFAULT true NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_default."EmployeeDeviceAccesses" OWNER TO postgres;

--
-- Name: Employees; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "AccessFailedCount" integer DEFAULT 0 NOT NULL,
    "LockoutEnabled" boolean DEFAULT false NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "BiometricEnrollmentId" character varying(100),
    "BiometricEnrollmentDate" timestamp with time zone,
    "PrimaryLocationId" uuid
);


ALTER TABLE tenant_default."Employees" OWNER TO postgres;

--
-- Name: LeaveApplications; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."LeaveApplications" OWNER TO postgres;

--
-- Name: LeaveApprovals; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."LeaveApprovals" OWNER TO postgres;

--
-- Name: LeaveBalances; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."LeaveBalances" OWNER TO postgres;

--
-- Name: LeaveEncashments; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."LeaveEncashments" OWNER TO postgres;

--
-- Name: LeaveTypes; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."LeaveTypes" OWNER TO postgres;

--
-- Name: Locations; Type: TABLE; Schema: tenant_default; Owner: postgres
--

CREATE TABLE tenant_default."Locations" (
    "Id" uuid NOT NULL,
    "LocationCode" character varying(50) NOT NULL,
    "LocationName" character varying(200) NOT NULL,
    "LocationType" character varying(50) NOT NULL,
    "Address" character varying(500),
    "City" character varying(100),
    "State" character varying(100),
    "PostalCode" character varying(20),
    "Country" character varying(100),
    "Latitude" numeric(10,7),
    "Longitude" numeric(10,7),
    "Timezone" character varying(50),
    "ContactPerson" character varying(200),
    "ContactPhone" character varying(20),
    "ContactEmail" character varying(100),
    "IsHeadOffice" boolean DEFAULT false NOT NULL,
    "IsActive" boolean DEFAULT true NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_default."Locations" OWNER TO postgres;

--
-- Name: PayrollCycles; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."PayrollCycles" OWNER TO postgres;

--
-- Name: Payslips; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "IsCalculatedFromTimesheets" boolean DEFAULT false NOT NULL,
    "TimesheetIdsJson" text,
    "TimesheetsProcessed" integer DEFAULT 0 NOT NULL
);


ALTER TABLE tenant_default."Payslips" OWNER TO postgres;

--
-- Name: PublicHolidays; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."PublicHolidays" OWNER TO postgres;

--
-- Name: SalaryComponents; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."SalaryComponents" OWNER TO postgres;

--
-- Name: TenantCustomComplianceRules; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."TenantCustomComplianceRules" OWNER TO postgres;

--
-- Name: TenantSectorConfigurations; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."TenantSectorConfigurations" OWNER TO postgres;

--
-- Name: TimesheetAdjustments; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."TimesheetAdjustments" OWNER TO postgres;

--
-- Name: TimesheetComments; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."TimesheetComments" OWNER TO postgres;

--
-- Name: TimesheetEntries; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."TimesheetEntries" OWNER TO postgres;

--
-- Name: Timesheets; Type: TABLE; Schema: tenant_default; Owner: postgres
--

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
    "DeletedBy" text
);


ALTER TABLE tenant_default."Timesheets" OWNER TO postgres;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: tenant_default; Owner: postgres
--

CREATE TABLE tenant_default."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE tenant_default."__EFMigrationsHistory" OWNER TO postgres;

--
-- Name: AttendanceAnomalies; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."AttendanceAnomalies" (
    "Id" uuid NOT NULL,
    "AttendanceId" uuid,
    "EmployeeId" uuid NOT NULL,
    "DeviceId" uuid,
    "LocationId" uuid,
    "AnomalyType" character varying(50) NOT NULL,
    "AnomalyDate" timestamp with time zone NOT NULL,
    "AnomalySeverity" character varying(20) NOT NULL,
    "Description" character varying(1000),
    "DetectionMethod" character varying(50) NOT NULL,
    "IsResolved" boolean DEFAULT false NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "ResolvedBy" uuid,
    "ResolutionNote" character varying(1000),
    "ActionTaken" character varying(500),
    "AnomalyDetailsJson" jsonb,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."AttendanceAnomalies" OWNER TO postgres;

--
-- Name: AttendanceCorrections; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."AttendanceCorrections" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."AttendanceCorrections" OWNER TO postgres;

--
-- Name: AttendanceMachines; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."AttendanceMachines" (
    "Id" uuid NOT NULL,
    "MachineName" character varying(200) NOT NULL,
    "MachineId" character varying(100) NOT NULL,
    "IpAddress" character varying(50),
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
    "LegacyLocation" text,
    "DeviceCode" character varying(50) DEFAULT ''::character varying,
    "DeviceType" character varying(100) DEFAULT 'ZKTeco'::character varying,
    "LocationId" uuid,
    "MacAddress" character varying(50),
    "FirmwareVersion" character varying(50),
    "ConnectionMethod" character varying(50) DEFAULT 'TCP/IP'::character varying,
    "ConnectionTimeoutSeconds" integer DEFAULT 30,
    "DeviceConfigJson" jsonb,
    "DeviceStatus" character varying(50) DEFAULT 'Active'::character varying,
    "SyncEnabled" boolean DEFAULT true,
    "SyncIntervalMinutes" integer DEFAULT 15,
    "LastSyncTime" timestamp with time zone,
    "LastSyncStatus" character varying(50),
    "LastSyncRecordCount" integer DEFAULT 0,
    "OfflineAlertEnabled" boolean DEFAULT true,
    "OfflineThresholdMinutes" integer DEFAULT 60
);


ALTER TABLE tenant_siraaj."AttendanceMachines" OWNER TO postgres;

--
-- Name: Attendances; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."Attendances" (
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
    "DeviceId" uuid,
    "LocationId" uuid,
    "DeviceUserId" character varying(100),
    "PunchSource" character varying(50) DEFAULT 'Biometric'::character varying,
    "VerificationMethod" character varying(50),
    "IsAuthorized" boolean DEFAULT true,
    "AuthorizationNote" character varying(1000)
);


ALTER TABLE tenant_siraaj."Attendances" OWNER TO postgres;

--
-- Name: Departments; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."Departments" (
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
    "CostCenterCode" character varying(50)
);


ALTER TABLE tenant_siraaj."Departments" OWNER TO postgres;

--
-- Name: DeviceSyncLogs; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."DeviceSyncLogs" (
    "Id" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "SyncStartTime" timestamp with time zone NOT NULL,
    "SyncEndTime" timestamp with time zone,
    "SyncDurationSeconds" integer,
    "SyncStatus" character varying(50) NOT NULL,
    "RecordsFetched" integer DEFAULT 0 NOT NULL,
    "RecordsProcessed" integer DEFAULT 0 NOT NULL,
    "RecordsInserted" integer DEFAULT 0 NOT NULL,
    "RecordsUpdated" integer DEFAULT 0 NOT NULL,
    "RecordsSkipped" integer DEFAULT 0 NOT NULL,
    "RecordsErrored" integer DEFAULT 0 NOT NULL,
    "SyncMethod" character varying(50) NOT NULL,
    "DateRangeFrom" timestamp with time zone,
    "DateRangeTo" timestamp with time zone,
    "ErrorMessage" character varying(2000),
    "ErrorDetailsJson" jsonb,
    "InitiatedBy" uuid,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."DeviceSyncLogs" OWNER TO postgres;

--
-- Name: EmergencyContacts; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."EmergencyContacts" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."EmergencyContacts" OWNER TO postgres;

--
-- Name: EmployeeDeviceAccesses; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."EmployeeDeviceAccesses" (
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
    "IsActive" boolean DEFAULT true NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."EmployeeDeviceAccesses" OWNER TO postgres;

--
-- Name: Employees; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."Employees" (
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
    "AccessFailedCount" integer DEFAULT 0 NOT NULL,
    "LockoutEnabled" boolean DEFAULT false NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "PasswordHash" text,
    "BloodGroup" text,
    "AnnualLeaveDays" integer DEFAULT 20 NOT NULL,
    "SickLeaveDays" integer DEFAULT 15 NOT NULL,
    "CasualLeaveDays" integer DEFAULT 5 NOT NULL,
    "CarryForwardAllowed" boolean DEFAULT true NOT NULL,
    "HighestQualification" text,
    "University" text,
    "Skills" text,
    "Languages" text,
    "ResumeFilePath" text,
    "IdCopyFilePath" text,
    "CertificatesFilePath" text,
    "ContractFilePath" text,
    "Notes" text,
    "CSGNumber" text,
    "Designation" text DEFAULT ''::text NOT NULL,
    "IndustrySector" text DEFAULT ''::text NOT NULL,
    "WorkLocation" text,
    "ReportingTo" text,
    "ProbationPeriodMonths" integer,
    "EmploymentContractType" text DEFAULT 'Permanent'::text NOT NULL,
    "AlternateEmail" text,
    "EmploymentStatus" text DEFAULT 'Active'::text NOT NULL,
    "PaymentFrequency" text DEFAULT 'Monthly'::text NOT NULL,
    "PRNumber" text,
    "TransportAllowance" numeric(18,2),
    "HousingAllowance" numeric(18,2),
    "MealAllowance" numeric(18,2),
    "WorkPermitType" character varying(100),
    "ResidencePermitNumber" character varying(100),
    "EmergencyContactName" character varying(200),
    "EmergencyContactRelation" character varying(100),
    "EmergencyContactPhone" character varying(20),
    "EmergencyContactAddress" character varying(500),
    "BiometricEnrollmentId" character varying(100),
    "BiometricEnrollmentDate" timestamp with time zone,
    "PrimaryLocationId" uuid
);


ALTER TABLE tenant_siraaj."Employees" OWNER TO postgres;

--
-- Name: LeaveApplications; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."LeaveApplications" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."LeaveApplications" OWNER TO postgres;

--
-- Name: LeaveApprovals; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."LeaveApprovals" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."LeaveApprovals" OWNER TO postgres;

--
-- Name: LeaveBalances; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."LeaveBalances" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."LeaveBalances" OWNER TO postgres;

--
-- Name: LeaveEncashments; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."LeaveEncashments" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."LeaveEncashments" OWNER TO postgres;

--
-- Name: LeaveTypes; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."LeaveTypes" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."LeaveTypes" OWNER TO postgres;

--
-- Name: Locations; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."Locations" (
    "Id" uuid NOT NULL,
    "LocationCode" character varying(50) NOT NULL,
    "LocationName" character varying(200) NOT NULL,
    "LocationType" character varying(50) NOT NULL,
    "Address" character varying(500),
    "City" character varying(100),
    "State" character varying(100),
    "PostalCode" character varying(20),
    "Country" character varying(100),
    "Latitude" numeric(10,7),
    "Longitude" numeric(10,7),
    "Timezone" character varying(50),
    "ContactPerson" character varying(200),
    "ContactPhone" character varying(20),
    "ContactEmail" character varying(100),
    "IsHeadOffice" boolean DEFAULT false NOT NULL,
    "IsActive" boolean DEFAULT true NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."Locations" OWNER TO postgres;

--
-- Name: PayrollCycles; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."PayrollCycles" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."PayrollCycles" OWNER TO postgres;

--
-- Name: Payslips; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."Payslips" (
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
    "IsCalculatedFromTimesheets" boolean DEFAULT false NOT NULL,
    "TimesheetIdsJson" text,
    "TimesheetsProcessed" integer DEFAULT 0 NOT NULL
);


ALTER TABLE tenant_siraaj."Payslips" OWNER TO postgres;

--
-- Name: PublicHolidays; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."PublicHolidays" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."PublicHolidays" OWNER TO postgres;

--
-- Name: SalaryComponents; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."SalaryComponents" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."SalaryComponents" OWNER TO postgres;

--
-- Name: TenantCustomComplianceRules; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."TenantCustomComplianceRules" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."TenantCustomComplianceRules" OWNER TO postgres;

--
-- Name: TenantSectorConfigurations; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."TenantSectorConfigurations" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."TenantSectorConfigurations" OWNER TO postgres;

--
-- Name: TimesheetAdjustments; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."TimesheetAdjustments" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."TimesheetAdjustments" OWNER TO postgres;

--
-- Name: TimesheetComments; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."TimesheetComments" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."TimesheetComments" OWNER TO postgres;

--
-- Name: TimesheetEntries; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."TimesheetEntries" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."TimesheetEntries" OWNER TO postgres;

--
-- Name: Timesheets; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."Timesheets" (
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
    "DeletedBy" text
);


ALTER TABLE tenant_siraaj."Timesheets" OWNER TO postgres;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: tenant_siraaj; Owner: postgres
--

CREATE TABLE tenant_siraaj."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE tenant_siraaj."__EFMigrationsHistory" OWNER TO postgres;

--
-- Name: aggregatedcounter id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.aggregatedcounter ALTER COLUMN id SET DEFAULT nextval('hangfire.aggregatedcounter_id_seq'::regclass);


--
-- Name: counter id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.counter ALTER COLUMN id SET DEFAULT nextval('hangfire.counter_id_seq'::regclass);


--
-- Name: hash id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.hash ALTER COLUMN id SET DEFAULT nextval('hangfire.hash_id_seq'::regclass);


--
-- Name: job id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.job ALTER COLUMN id SET DEFAULT nextval('hangfire.job_id_seq'::regclass);


--
-- Name: jobparameter id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobparameter ALTER COLUMN id SET DEFAULT nextval('hangfire.jobparameter_id_seq'::regclass);


--
-- Name: jobqueue id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobqueue ALTER COLUMN id SET DEFAULT nextval('hangfire.jobqueue_id_seq'::regclass);


--
-- Name: list id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.list ALTER COLUMN id SET DEFAULT nextval('hangfire.list_id_seq'::regclass);


--
-- Name: set id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.set ALTER COLUMN id SET DEFAULT nextval('hangfire.set_id_seq'::regclass);


--
-- Name: state id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.state ALTER COLUMN id SET DEFAULT nextval('hangfire.state_id_seq'::regclass);


--
-- Data for Name: aggregatedcounter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.aggregatedcounter (id, key, value, expireat) FROM stdin;
1	stats:succeeded:2025-11-02	2	2025-12-02 05:00:11.004239+00
7	stats:succeeded:2025-11-03	2	2025-12-03 06:47:53.785366+00
10	stats:succeeded:2025-11-04	2	2025-12-04 05:00:13.076378+00
17	stats:succeeded:2025-11-05	2	2025-12-05 04:59:59.376773+00
22	stats:succeeded:2025-11-06-03	2	2025-11-07 03:49:43.42436+00
23	stats:succeeded:2025-11-06	3	2025-12-06 05:00:02.592577+00
26	stats:succeeded:2025-11-06-05	1	2025-11-07 05:00:03.592577+00
3	stats:succeeded	11	\N
\.


--
-- Data for Name: counter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.counter (id, key, value, expireat) FROM stdin;
\.


--
-- Data for Name: hash; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.hash (id, key, field, value, expireat, updatecount) FROM stdin;
1	recurring-job:document-expiry-alerts	Queue	default	\N	0
2	recurring-job:document-expiry-alerts	Cron	0 9 * * *	\N	0
3	recurring-job:document-expiry-alerts	TimeZoneId	Mauritius Standard Time	\N	0
4	recurring-job:document-expiry-alerts	Job	{"t":"HRMS.BackgroundJobs.Jobs.DocumentExpiryAlertJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
5	recurring-job:document-expiry-alerts	CreatedAt	1761984040510	\N	0
7	recurring-job:document-expiry-alerts	V	2	\N	0
8	recurring-job:absent-marking	Queue	default	\N	0
9	recurring-job:absent-marking	Cron	0 23 * * *	\N	0
10	recurring-job:absent-marking	TimeZoneId	Mauritius Standard Time	\N	0
11	recurring-job:absent-marking	Job	{"t":"HRMS.BackgroundJobs.Jobs.AbsentMarkingJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
12	recurring-job:absent-marking	CreatedAt	1761984040728	\N	0
14	recurring-job:absent-marking	V	2	\N	0
15	recurring-job:leave-accrual	Queue	default	\N	0
16	recurring-job:leave-accrual	Cron	0 1 1 * *	\N	0
17	recurring-job:leave-accrual	TimeZoneId	Mauritius Standard Time	\N	0
18	recurring-job:leave-accrual	Job	{"t":"HRMS.BackgroundJobs.Jobs.LeaveAccrualJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
19	recurring-job:leave-accrual	CreatedAt	1761984040744	\N	0
20	recurring-job:leave-accrual	NextExecution	1764536400000	\N	0
21	recurring-job:leave-accrual	V	2	\N	0
26	recurring-job:delete-expired-drafts	Queue	default	\N	0
27	recurring-job:delete-expired-drafts	Cron	0 2 * * *	\N	0
28	recurring-job:delete-expired-drafts	TimeZoneId	Mauritius Standard Time	\N	0
29	recurring-job:delete-expired-drafts	Job	{"t":"HRMS.BackgroundJobs.Jobs.DeleteExpiredDraftsJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
30	recurring-job:delete-expired-drafts	CreatedAt	1762316317116	\N	0
32	recurring-job:delete-expired-drafts	V	2	\N	0
22	recurring-job:absent-marking	LastExecution	1762400981363	\N	0
13	recurring-job:absent-marking	NextExecution	1762455600000	\N	0
23	recurring-job:absent-marking	LastJobId	9	\N	0
33	recurring-job:delete-expired-drafts	LastExecution	1762400981363	\N	0
31	recurring-job:delete-expired-drafts	NextExecution	1762466400000	\N	0
34	recurring-job:delete-expired-drafts	LastJobId	10	\N	0
24	recurring-job:document-expiry-alerts	LastExecution	1762405203410	\N	0
6	recurring-job:document-expiry-alerts	NextExecution	1762491600000	\N	0
25	recurring-job:document-expiry-alerts	LastJobId	11	\N	0
\.


--
-- Data for Name: job; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.job (id, stateid, statename, invocationdata, arguments, createdat, expireat, updatecount) FROM stdin;
10	53	Succeeded	{"Type": "HRMS.BackgroundJobs.Jobs.DeleteExpiredDraftsJob, HRMS.BackgroundJobs", "Method": "ExecuteAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-06 03:49:41.533852+00	2025-11-07 03:49:42.927916+00	0
9	54	Succeeded	{"Type": "HRMS.BackgroundJobs.Jobs.AbsentMarkingJob, HRMS.BackgroundJobs", "Method": "ExecuteAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-06 03:49:41.475086+00	2025-11-07 03:49:43.42436+00	0
11	57	Succeeded	{"Type": "HRMS.BackgroundJobs.Jobs.DocumentExpiryAlertJob, HRMS.BackgroundJobs", "Method": "ExecuteAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-06 05:00:03.442735+00	2025-11-07 05:00:03.592577+00	0
\.


--
-- Data for Name: jobparameter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.jobparameter (id, jobid, name, value, updatecount) FROM stdin;
26	9	RecurringJobId	"absent-marking"	0
27	9	Time	1762400981	0
28	9	CurrentCulture	""	0
29	10	RecurringJobId	"delete-expired-drafts"	0
30	10	Time	1762400981	0
31	10	CurrentCulture	""	0
32	11	RecurringJobId	"document-expiry-alerts"	0
33	11	Time	1762405203	0
34	11	CurrentCulture	""	0
\.


--
-- Data for Name: jobqueue; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.jobqueue (id, jobid, queue, fetchedat, updatecount) FROM stdin;
\.


--
-- Data for Name: list; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.list (id, key, value, expireat, updatecount) FROM stdin;
\.


--
-- Data for Name: lock; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.lock (resource, updatecount, acquired) FROM stdin;
\.


--
-- Data for Name: schema; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.schema (version) FROM stdin;
23
\.


--
-- Data for Name: server; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.server (id, data, lastheartbeat, updatecount) FROM stdin;
hrms-codespaces-eaf641:159875:8f87f07f-2052-4893-85d7-cb332a34d780	{"Queues": ["default"], "StartedAt": "2025-11-06T06:35:00.1217336Z", "WorkerCount": 5}	2025-11-06 08:43:03.31578+00	0
\.


--
-- Data for Name: set; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.set (id, key, score, value, expireat, updatecount) FROM stdin;
3	recurring-jobs	1764536400	leave-accrual	\N	0
2	recurring-jobs	1762455600	absent-marking	\N	0
23	recurring-jobs	1762466400	delete-expired-drafts	\N	0
1	recurring-jobs	1762491600	document-expiry-alerts	\N	0
\.


--
-- Data for Name: state; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.state (id, jobid, name, reason, createdat, data, updatecount) FROM stdin;
49	9	Enqueued	Triggered by recurring job scheduler	2025-11-06 03:49:41.512136+00	{"Queue": "default", "EnqueuedAt": "1762400981503"}	0
50	10	Enqueued	Triggered by recurring job scheduler	2025-11-06 03:49:41.538828+00	{"Queue": "default", "EnqueuedAt": "1762400981538"}	0
51	9	Processing	\N	2025-11-06 03:49:41.553991+00	{"ServerId": "hrms-codespaces-eaf641:5841:854a2e0c-dedb-49ac-aa1c-0dec3514d7e9", "WorkerId": "f17c1d5a-c5e9-40e1-bd6a-1128cde3332e", "StartedAt": "1762400981532"}	0
52	10	Processing	\N	2025-11-06 03:49:41.555165+00	{"ServerId": "hrms-codespaces-eaf641:5841:854a2e0c-dedb-49ac-aa1c-0dec3514d7e9", "WorkerId": "8e6fdb57-4305-404d-bae8-b8a52b2aa4e4", "StartedAt": "1762400981550"}	0
53	10	Succeeded	\N	2025-11-06 03:49:42.929464+00	{"Latency": "25", "SucceededAt": "1762400982912", "PerformanceDuration": "1352"}	0
54	9	Succeeded	\N	2025-11-06 03:49:43.430667+00	{"Latency": "84", "SucceededAt": "1762400983414", "PerformanceDuration": "1855"}	0
55	11	Enqueued	Triggered by recurring job scheduler	2025-11-06 05:00:03.469543+00	{"Queue": "default", "EnqueuedAt": "1762405203459"}	0
56	11	Processing	\N	2025-11-06 05:00:03.499754+00	{"ServerId": "hrms-codespaces-eaf641:63174:e3a4bd9d-fc34-4dc0-aec2-e9c1a6351b5b", "WorkerId": "8e663d9a-da7d-4183-9e4f-476e28ffd833", "StartedAt": "1762405203486"}	0
57	11	Succeeded	\N	2025-11-06 05:00:03.593758+00	{"Latency": "61", "SucceededAt": "1762405203584", "PerformanceDuration": "79"}	0
\.


--
-- Data for Name: AdminUsers; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."AdminUsers" ("Id", "UserName", "Email", "PasswordHash", "FirstName", "LastName", "IsActive", "LastLoginDate", "PhoneNumber", "IsTwoFactorEnabled", "TwoFactorSecret", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "AccessFailedCount", "LockoutEnd", "LockoutEnabled") FROM stdin;
3017eeb8-e69d-4b26-8842-b66675279a9d	admin	admin@hrms.com	NVXL/VALhkJgf+U31pi6hifCkOQaqgf5XBzBurIisrA4qIyG1DeaVy4MYltZtuQF	Super	Admin	t	2025-11-04 05:54:43.994216+00	\N	f	\N	2025-11-01 08:13:24.214641+00	\N	2025-11-01 08:13:24.214682+00	\N	f	\N	\N	0	\N	f
\.


--
-- Data for Name: AuditLogs; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."AuditLogs" ("Id", "TenantId", "Action", "EntityType", "EntityId", "OldValues", "NewValues", "PerformedBy", "PerformedAt", "IpAddress", "UserAgent", "AdditionalInfo", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: IndustrySectors; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."IndustrySectors" ("Id", "SectorCode", "SectorName", "SectorNameFrench", "Description", "ParentSectorId", "RemunerationOrderReference", "RemunerationOrderYear", "IsActive", "RequiresSpecialPermits", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
1	CAT	Catering & Tourism Industries	Industries de l'Hôtellerie et du Tourisme	\N	\N	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419356+00	\N	f
10	CONST	Construction & Quarrying Industries	Industries de la Construction et des Carrières	\N	\N	GN No. 162 of 2022	2022	t	f	2025-11-02 04:26:43.41951+00	\N	f
11	BPO	Business Process Outsourcing (BPO)	Externalisation des Processus d'Affaires	\N	\N	GN No. 201 of 2023	2023	t	f	2025-11-02 04:26:43.419511+00	\N	f
12	SECURITY	Security Services	Services de Sécurité	\N	\N	GN No. 178 of 2023	2023	t	t	2025-11-02 04:26:43.419511+00	\N	f
13	CLEANING	Cleaning Services	Services de Nettoyage	\N	\N	GN No. 165 of 2022	2022	t	f	2025-11-02 04:26:43.419511+00	\N	f
14	FINANCE	Financial Services	Services Financiers	\N	\N	GN No. 189 of 2023	2023	t	t	2025-11-02 04:26:43.419511+00	\N	f
19	ICT	Information & Communication Technology	Technologies de l'Information et de la Communication	\N	\N	GN No. 195 of 2023	2023	t	f	2025-11-02 04:26:43.419513+00	\N	f
20	MANUFACTURING	Manufacturing Industries	Industries Manufacturières	\N	\N	GN No. 172 of 2022	2022	t	f	2025-11-02 04:26:43.419516+00	\N	f
25	RETAIL	Retail & Distributive Trade	Commerce de Détail et Distribution	\N	\N	GN No. 183 of 2023	2023	t	f	2025-11-02 04:26:43.419518+00	\N	f
29	HEALTHCARE	Healthcare & Medical Services	Services de Santé et Médicaux	\N	\N	GN No. 191 of 2023	2023	t	t	2025-11-02 04:26:43.419519+00	\N	f
30	EDUCATION	Education & Training Services	Services d'Éducation et de Formation	\N	\N	GN No. 187 of 2023	2023	t	f	2025-11-02 04:26:43.41952+00	\N	f
31	TRANSPORT	Transport & Logistics	Transport et Logistique	\N	\N	GN No. 174 of 2022	2022	t	t	2025-11-02 04:26:43.41952+00	\N	f
35	BAKERY	Bakeries & Confectionery	Boulangeries et Confiseries	\N	\N	GN No. 168 of 2022	2022	t	f	2025-11-02 04:26:43.419521+00	\N	f
36	ENTERTAINMENT	Cinema & Entertainment	Cinéma et Divertissement	\N	\N	GN No. 179 of 2023	2023	t	f	2025-11-02 04:26:43.419521+00	\N	f
37	LEGAL	Legal Services (Attorneys & Notaries)	Services Juridiques (Avocats et Notaires)	\N	\N	GN No. 193 of 2023	2023	t	t	2025-11-02 04:26:43.419522+00	\N	f
38	REALESTATE	Real Estate & Property Management	Immobilier et Gestion de Propriétés	\N	\N	GN No. 186 of 2023	2023	t	f	2025-11-02 04:26:43.419522+00	\N	f
39	AGRICULTURE	Agriculture & Fishing	Agriculture et Pêche	\N	\N	GN No. 161 of 2022	2022	t	f	2025-11-02 04:26:43.419522+00	\N	f
40	PRINTING	Printing & Publishing	Imprimerie et Édition	\N	\N	GN No. 177 of 2023	2023	t	f	2025-11-02 04:26:43.419523+00	\N	f
41	TELECOM	Telecommunications	Télécommunications	\N	\N	GN No. 197 of 2023	2023	t	t	2025-11-02 04:26:43.419523+00	\N	f
42	IMPORTEXPORT	Import/Export Trade	Commerce d'Importation/Exportation	\N	\N	GN No. 182 of 2023	2023	t	f	2025-11-02 04:26:43.419523+00	\N	f
43	WAREHOUSE	Warehouse & Storage Services	Services d'Entreposage et de Stockage	\N	\N	GN No. 175 of 2022	2022	t	f	2025-11-02 04:26:43.419524+00	\N	f
44	PROFESSIONAL	Professional Services (Accounting, Consulting)	Services Professionnels (Comptabilité, Conseil)	\N	\N	GN No. 192 of 2023	2023	t	f	2025-11-02 04:26:43.419524+00	\N	f
45	HOSPITALITY	Hospitality Services (excluding Catering)	Services d'Accueil (hors Restauration)	\N	\N	GN No. 188 of 2023	2023	t	f	2025-11-02 04:26:43.419524+00	\N	f
46	BEAUTY	Beauty & Wellness Services	Services de Beauté et de Bien-être	\N	\N	GN No. 169 of 2022	2022	t	f	2025-11-02 04:26:43.419524+00	\N	f
47	EVENTS	Event Management & Wedding Services	Gestion d'Événements et Services de Mariage	\N	\N	GN No. 184 of 2023	2023	t	f	2025-11-02 04:26:43.419525+00	\N	f
48	DOMESTIC	Domestic Services	Services Domestiques	\N	\N	GN No. 166 of 2022	2022	t	f	2025-11-02 04:26:43.419525+00	\N	f
49	GENERAL	General Office & Administrative Services	Services Généraux de Bureau et Administratifs	\N	\N	GN No. 199 of 2023	2023	t	f	2025-11-02 04:26:43.419525+00	\N	f
50	PHARMACY	Pharmacy & Pharmaceutical Services	Pharmacie et Services Pharmaceutiques	\N	\N	GN No. 190 of 2023	2023	t	t	2025-11-02 04:26:43.419526+00	\N	f
51	AUTOMOTIVE	Automotive Services & Repairs	Services Automobiles et Réparations	\N	\N	GN No. 170 of 2022	2022	t	f	2025-11-02 04:26:43.419526+00	\N	f
52	RECRUITMENT	Recruitment & Employment Agencies	Agences de Recrutement et d'Emploi	\N	\N	GN No. 194 of 2023	2023	t	t	2025-11-02 04:26:43.419526+00	\N	f
2	CAT-HOTEL-LARGE	Hotels & Accommodation (40+ covers)	Hôtels et Hébergement (40+ couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419407+00	\N	f
3	CAT-HOTEL-SMALL	Hotels & Accommodation (Under 40 covers)	Hôtels et Hébergement (Moins de 40 couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419407+00	\N	f
4	CAT-RESTAURANT-LARGE	Restaurants & Cafés (40+ covers)	Restaurants et Cafés (40+ couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419408+00	\N	f
5	CAT-RESTAURANT-SMALL	Restaurants & Cafés (Under 40 covers)	Restaurants et Cafés (Moins de 40 couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419408+00	\N	f
6	CAT-FASTFOOD	Fast Food Outlets	Restauration Rapide	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419408+00	\N	f
7	CAT-BARS	Bars & Night Clubs	Bars et Boîtes de Nuit	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419409+00	\N	f
8	CAT-BOARDINGHOUSE	Boarding Houses & Guest Houses	Pensions et Maisons d'Hôtes	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419413+00	\N	f
9	CAT-ATTRACTIONS	Tourist Attractions & Leisure Parks	Attractions Touristiques et Parcs de Loisirs	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-02 04:26:43.419414+00	\N	f
15	FINANCE-BANKING	Banking	Services Bancaires	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-02 04:26:43.419512+00	\N	f
16	FINANCE-INSURANCE	Insurance	Assurance	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-02 04:26:43.419512+00	\N	f
17	FINANCE-FUND	Fund Administration	Administration de Fonds	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-02 04:26:43.419512+00	\N	f
18	FINANCE-WEALTH	Wealth Management	Gestion de Patrimoine	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-02 04:26:43.419513+00	\N	f
21	MFG-TEXTILE	Textiles & Apparel (Export Enterprises)	Textile et Habillement (Entreprises d'Exportation)	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-02 04:26:43.419517+00	\N	f
22	MFG-JEWELRY	Jewelry & Optical Goods	Bijouterie et Optique	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-02 04:26:43.419517+00	\N	f
23	MFG-FURNITURE	Furniture Manufacturing	Fabrication de Meubles	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-02 04:26:43.419517+00	\N	f
24	MFG-FOOD	Food Processing & Beverages	Transformation Alimentaire et Boissons	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-02 04:26:43.419518+00	\N	f
26	RETAIL-SUPERMARKET	Supermarkets & Hypermarkets	Supermarchés et Hypermarchés	\N	25	GN No. 183 of 2023	2023	t	f	2025-11-02 04:26:43.419518+00	\N	f
27	RETAIL-SHOPS	Shops & Stores	Boutiques et Magasins	\N	25	GN No. 183 of 2023	2023	t	f	2025-11-02 04:26:43.419519+00	\N	f
28	RETAIL-WHOLESALE	Wholesale Trade	Commerce de Gros	\N	25	GN No. 183 of 2023	2023	t	f	2025-11-02 04:26:43.419519+00	\N	f
32	TRANSPORT-FREIGHT	Freight & Cargo Services	Services de Fret et Cargaison	\N	31	GN No. 174 of 2022	2022	t	t	2025-11-02 04:26:43.41952+00	\N	f
33	TRANSPORT-TAXI	Taxi Services	Services de Taxi	\N	31	GN No. 174 of 2022	2022	t	t	2025-11-02 04:26:43.41952+00	\N	f
34	TRANSPORT-BUS	Bus Services	Services de Bus	\N	31	GN No. 174 of 2022	2022	t	t	2025-11-02 04:26:43.419521+00	\N	f
\.


--
-- Data for Name: SectorComplianceRules; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."SectorComplianceRules" ("Id", "SectorId", "RuleCategory", "RuleName", "RuleConfig", "EffectiveFrom", "EffectiveTo", "LegalReference", "Notes", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
1	2	OVERTIME	Catering & Tourism - Overtime Rates	{"sunday_rate": 2.0, "night_shift_rate": 1.25, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "time_off_in_lieu_allowed": true, "meal_allowance_amount_mur": 85, "max_overtime_hours_per_day": 4, "meal_allowance_after_hours": 2, "max_overtime_hours_per_week": 20, "cyclone_warning_class_3_rate": 3.0, "public_holiday_after_hours_rate": 3.0, "public_holiday_normal_hours_rate": 2.0}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023 - Catering & Tourism Remuneration Order	\N	2025-11-02 04:31:25.904222+00	\N	f
2	2	MINIMUM_WAGE	Catering & Tourism - Minimum Wage 2025	{"notes": "Minimum wage increased Jan 2025 from MUR 16,500 to MUR 17,110. Salary compensation increased from MUR 500 to MUR 610.", "currency": "MUR", "effective_date": "2025-01-01", "salary_compensation_mur": 610, "monthly_minimum_wage_mur": 17110, "applies_to_basic_salary_up_to_mur": 50000}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023 + National Minimum Wage Regulations 2025	\N	2025-11-02 04:31:25.904312+00	\N	f
3	2	WORKING_HOURS	Catering & Tourism - Working Hours	{"daily_max_hours": 12, "rotational_shifts": true, "unsocial_hours_end": "06:00", "standard_daily_hours": 9, "unsocial_hours_start": "22:00", "standard_weekly_hours": 45, "shift_patterns_allowed": true, "working_pattern_options": ["9 hours x 5 days per week", "8 hours x 5 days + 5 hours x 1 day"], "mandatory_lunch_break_minutes": 60}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023 - Catering & Tourism Remuneration Order	\N	2025-11-02 04:31:25.904312+00	\N	f
4	2	ALLOWANCES	Catering & Tourism - Allowances	{"tips_pooling_allowed": true, "service_charge_distribution": "As per hotel policy", "housing_allowance_applicable": false, "meal_allowance_per_shift_mur": 85, "uniform_allowance_per_year_mur": 500, "transport_allowance_per_month_mur": 0}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023	\N	2025-11-02 04:31:25.904313+00	\N	f
5	2	LEAVE	Catering & Tourism - Leave Entitlements	{"sick_leave_days": 15, "annual_leave_days": 22, "casual_leave_days": 5, "paternity_leave_days": 5, "maternity_leave_weeks": 14, "leave_calculation_basis": "working_days", "encashment_allowed_on_resignation": true, "annual_leave_carry_forward_max_days": 5}	2025-01-01 00:00:00+00	\N	Workers' Rights Act 2019 + GN No. 185 of 2023	\N	2025-11-02 04:31:25.904313+00	\N	f
6	2	GRATUITY	Catering & Tourism - Gratuity Calculation	{"notes": "Gratuity = (Basic Salary / 22) * 15 * Years of Service", "formula": "15 days per year of service", "calculation_basis": "basic_salary", "minimum_service_months": 12, "max_gratuity_amount_mur": null, "pro_rated_for_partial_year": true}	2025-01-01 00:00:00+00	\N	Workers' Rights Act 2019 Section 111	\N	2025-11-02 04:31:25.904314+00	\N	f
7	11	OVERTIME	BPO - Overtime Rates	{"sunday_rate": 2.0, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "time_off_in_lieu_allowed": true, "max_overtime_hours_per_day": 4, "max_overtime_hours_per_week": 20, "public_holiday_after_hours_rate": 3.0, "night_shift_allowance_percentage": 10, "public_holiday_normal_hours_rate": 2.0}	2025-01-01 00:00:00+00	\N	GN No. 201 of 2023 - BPO Remuneration Order	\N	2025-11-02 04:31:25.904314+00	\N	f
8	11	MINIMUM_WAGE	BPO - Minimum Wage 2025	{"currency": "MUR", "effective_date": "2025-01-01", "salary_compensation_mur": 610, "monthly_minimum_wage_mur": 17110, "applies_to_basic_salary_up_to_mur": 50000}	2025-01-01 00:00:00+00	\N	GN No. 201 of 2023 + National Minimum Wage Regulations 2025	\N	2025-11-02 04:31:25.904314+00	\N	f
9	11	WORKING_HOURS	BPO - Working Hours	{"daily_max_hours": 12, "rotational_shifts": true, "night_shifts_allowed": true, "standard_daily_hours": 9, "standard_weekly_hours": 45, "shift_patterns_allowed": true, "mandatory_break_minutes": 60, "working_pattern_options": ["9 hours x 5 days per week", "8 hours x 5 days + 5 hours x 1 day"]}	2025-01-01 00:00:00+00	\N	GN No. 201 of 2023	\N	2025-11-02 04:31:25.904315+00	\N	f
10	12	OVERTIME	Security Services - Overtime Rates	{"sunday_rate": 2.0, "night_shift_rate": 1.1, "weekday_overtime_rate": 1.25, "weekend_overtime_rate": 1.5, "time_off_in_lieu_allowed": false, "max_overtime_hours_per_day": 6, "max_overtime_hours_per_week": 24, "public_holiday_after_hours_rate": 2.5, "public_holiday_normal_hours_rate": 2.0}	2025-01-01 00:00:00+00	\N	GN No. 178 of 2023 - Security Services Remuneration Order	\N	2025-11-02 04:31:25.904315+00	\N	f
11	12	WORKING_HOURS	Security Services - Working Hours	{"daily_max_hours": 14, "rotational_shifts": true, "standard_daily_hours": 8, "standard_weekly_hours": 48, "shift_patterns_allowed": true, "mandatory_break_minutes": 60, "working_pattern_options": ["8 hours x 6 days per week", "12-hour shifts (rotating)"], "continuous_duty_max_hours": 12}	2025-01-01 00:00:00+00	\N	GN No. 178 of 2023	\N	2025-11-02 04:31:25.904315+00	\N	f
12	15	WORKING_HOURS	Banking - Working Hours	{"daily_max_hours": 10, "standard_daily_hours": 8, "standard_weekly_hours": 40, "shift_patterns_allowed": false, "mandatory_break_minutes": 60, "working_pattern_options": ["8 hours x 5 days per week (Monday-Friday)"], "weekend_work_exceptional_only": true}	2025-01-01 00:00:00+00	\N	GN No. 189 of 2023 - Financial Services Remuneration Order	\N	2025-11-02 04:31:25.904316+00	\N	f
13	15	OVERTIME	Banking - Overtime Rates	{"notes": "Banking sector typically minimizes overtime due to strict working hours", "sunday_rate": 2.5, "public_holiday_rate": 3.0, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.5, "time_off_in_lieu_preferred": true, "max_overtime_hours_per_week": 10}	2025-01-01 00:00:00+00	\N	GN No. 189 of 2023	\N	2025-11-02 04:31:25.904316+00	\N	f
14	10	OVERTIME	Construction - Overtime Rates	{"sunday_rate": 2.0, "night_shift_rate": 1.25, "public_holiday_rate": 2.5, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "max_overtime_hours_per_day": 4, "max_overtime_hours_per_week": 20}	2025-01-01 00:00:00+00	\N	GN No. 162 of 2022 - Construction Remuneration Order	\N	2025-11-02 04:31:25.904316+00	\N	f
15	10	ALLOWANCES	Construction - Allowances	{"meal_allowance_per_day_mur": 85, "hazard_allowance_applicable": true, "tool_allowance_per_month_mur": 200, "transport_allowance_per_day_mur": 50, "height_work_allowance_per_day_mur": 100, "safety_equipment_provided_by_employer": true}	2025-01-01 00:00:00+00	\N	GN No. 162 of 2022	\N	2025-11-02 04:31:25.904316+00	\N	f
16	26	WORKING_HOURS	Retail - Working Hours	{"daily_max_hours": 12, "standard_daily_hours": 9, "standard_weekly_hours": 45, "shift_patterns_allowed": true, "mandatory_break_minutes": 60, "working_pattern_options": ["9 hours x 5 days per week", "8 hours x 5 days + 5 hours x 1 day"], "sunday_trading_hours_restricted": true, "public_holiday_trading_requires_approval": true}	2025-01-01 00:00:00+00	\N	GN No. 183 of 2023 - Retail & Distributive Trade Remuneration Order	\N	2025-11-02 04:31:25.904317+00	\N	f
17	26	OVERTIME	Retail - Overtime Rates	{"sunday_rate": 2.5, "public_holiday_rate": 3.0, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "time_off_in_lieu_allowed": true, "max_overtime_hours_per_day": 4, "max_overtime_hours_per_week": 20}	2025-01-01 00:00:00+00	\N	GN No. 183 of 2023	\N	2025-11-02 04:31:25.904317+00	\N	f
\.


--
-- Data for Name: Tenants; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."Tenants" ("Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone", "Status", "MaxStorageGB", "MaxUsers", "EmployeeTier", "SuspensionReason", "SuspensionDate", "SoftDeleteDate", "DeletionReason", "GracePeriodDays", "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate", "AdminUserName", "AdminEmail", "CurrentUserCount", "SectorId", "SectorSelectedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "ApiCallsPerMonth", "CurrentStorageGB", "MonthlyPrice") FROM stdin;
bc11a50a-9227-44c2-946b-a07a46762bf4	Siraaj Processing Plant Co Ltd	siraaj	tenant_siraaj	sppcoltd.mu@gmail.com	+23057686978	1	10	50	1	\N	\N	\N	\N	30	2025-11-04 05:55:40.0071+00	\N	\N	Siraaj	sppcoltd.mu@gmail.com	0	\N	\N	2025-11-04 05:55:40.0071+00	SuperAdmin	\N	\N	f	\N	\N	50000	0	99
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20251031135011_InitialMasterSchema	9.0.0
20251104020635_AddApiCallsPerMonthToTenant	9.0.10
20251101014846_AddEmployeeAndEmergencyContact	9.0.10
20251101025137_AddLeaveManagementSystem	9.0.10
20251101031948_AddTenantSectorConfiguration	9.0.10
20251101043736_AddAttendanceManagement	9.0.10
20251101054015_AddPayrollManagement	9.0.10
20251104042616_SyncTenantModelChanges	9.0.10
20251105121448_AddTimesheetManagement	9.0.10
20251105132015_AddPayslipTimesheetIntegration	9.0.10
\.


--
-- Data for Name: AttendanceAnomalies; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."AttendanceAnomalies" ("Id", "AttendanceId", "EmployeeId", "DeviceId", "LocationId", "AnomalyType", "AnomalyDate", "AnomalySeverity", "Description", "DetectionMethod", "IsResolved", "ResolvedAt", "ResolvedBy", "ResolutionNote", "ActionTaken", "AnomalyDetailsJson", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: AttendanceCorrections; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."AttendanceCorrections" ("Id", "AttendanceId", "EmployeeId", "RequestedBy", "OriginalCheckIn", "OriginalCheckOut", "CorrectedCheckIn", "CorrectedCheckOut", "Reason", "Status", "ApprovedBy", "ApprovedAt", "RejectionReason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: AttendanceMachines; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."AttendanceMachines" ("Id", "MachineName", "MachineId", "IpAddress", "DepartmentId", "IsActive", "SerialNumber", "Model", "ZKTecoDeviceId", "Port", "LastSyncAt", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt", "DeletedBy", "LegacyLocation", "DeviceCode", "DeviceType", "LocationId", "MacAddress", "FirmwareVersion", "ConnectionMethod", "ConnectionTimeoutSeconds", "DeviceConfigJson", "DeviceStatus", "SyncEnabled", "SyncIntervalMinutes", "LastSyncTime", "LastSyncStatus", "LastSyncRecordCount", "OfflineAlertEnabled", "OfflineThresholdMinutes") FROM stdin;
\.


--
-- Data for Name: Attendances; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."Attendances" ("Id", "EmployeeId", "Date", "CheckInTime", "CheckOutTime", "WorkingHours", "OvertimeHours", "Status", "LateArrivalMinutes", "EarlyDepartureMinutes", "Remarks", "IsRegularized", "RegularizedBy", "RegularizedAt", "ShiftId", "AttendanceMachineId", "OvertimeRate", "IsSunday", "IsPublicHoliday", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt", "DeletedBy", "DeviceId", "LocationId", "DeviceUserId", "PunchSource", "VerificationMethod", "IsAuthorized", "AuthorizationNote") FROM stdin;
\.


--
-- Data for Name: Departments; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."Departments" ("Id", "Name", "Code", "Description", "ParentDepartmentId", "DepartmentHeadId", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "CostCenterCode") FROM stdin;
\.


--
-- Data for Name: DeviceSyncLogs; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."DeviceSyncLogs" ("Id", "DeviceId", "SyncStartTime", "SyncEndTime", "SyncDurationSeconds", "SyncStatus", "RecordsFetched", "RecordsProcessed", "RecordsInserted", "RecordsUpdated", "RecordsSkipped", "RecordsErrored", "SyncMethod", "DateRangeFrom", "DateRangeTo", "ErrorMessage", "ErrorDetailsJson", "InitiatedBy", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: EmergencyContacts; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."EmergencyContacts" ("Id", "EmployeeId", "ContactName", "PhoneNumber", "AlternatePhoneNumber", "Email", "Relationship", "ContactType", "Address", "Country", "IsPrimary", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: EmployeeDeviceAccesses; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."EmployeeDeviceAccesses" ("Id", "EmployeeId", "DeviceId", "AccessType", "AccessReason", "ValidFrom", "ValidUntil", "AllowedDaysJson", "AllowedTimeStart", "AllowedTimeEnd", "IsActive", "ApprovedBy", "ApprovedDate", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Employees; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."Employees" ("Id", "EmployeeCode", "FirstName", "LastName", "MiddleName", "Email", "PhoneNumber", "PersonalEmail", "DateOfBirth", "Gender", "MaritalStatus", "Address", "City", "PostalCode", "EmployeeType", "Nationality", "CountryOfOrigin", "NationalIdCard", "PassportNumber", "PassportIssueDate", "PassportExpiryDate", "VisaType", "VisaNumber", "VisaIssueDate", "VisaExpiryDate", "WorkPermitNumber", "WorkPermitExpiryDate", "TaxResidentStatus", "TaxIdNumber", "NPFNumber", "NSFNumber", "PRGFNumber", "IsNPFEligible", "IsNSFEligible", "JobTitle", "DepartmentId", "ManagerId", "JoiningDate", "ProbationEndDate", "ConfirmationDate", "ResignationDate", "LastWorkingDate", "IsActive", "ContractEndDate", "BasicSalary", "BankName", "BankAccountNumber", "BankBranch", "BankSwiftCode", "SalaryCurrency", "AnnualLeaveBalance", "SickLeaveBalance", "CasualLeaveBalance", "OffboardingReason", "IsOffboarded", "OffboardingDate", "OffboardingNotes", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "AccessFailedCount", "LockoutEnabled", "LockoutEnd", "BiometricEnrollmentId", "BiometricEnrollmentDate", "PrimaryLocationId") FROM stdin;
\.


--
-- Data for Name: LeaveApplications; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."LeaveApplications" ("Id", "ApplicationNumber", "EmployeeId", "LeaveTypeId", "StartDate", "EndDate", "TotalDays", "CalculationType", "Reason", "ContactNumber", "ContactAddress", "Status", "AppliedDate", "ApprovedDate", "ApprovedBy", "ApproverComments", "RejectedDate", "RejectedBy", "RejectionReason", "CancelledDate", "CancelledBy", "CancellationReason", "AttachmentPath", "RequiresHRApproval", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveApprovals; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."LeaveApprovals" ("Id", "LeaveApplicationId", "ApprovalLevel", "ApproverRole", "ApproverId", "Status", "ActionDate", "Comments", "RequestedInfo", "IsCurrentLevel", "IsComplete", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveBalances; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."LeaveBalances" ("Id", "EmployeeId", "LeaveTypeId", "Year", "TotalEntitlement", "UsedDays", "PendingDays", "CarriedForward", "Accrued", "LastAccrualDate", "ExpiryDate", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveEncashments; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."LeaveEncashments" ("Id", "EmployeeId", "CalculationDate", "LastWorkingDay", "UnusedAnnualLeaveDays", "UnusedSickLeaveDays", "TotalEncashableDays", "DailySalary", "TotalEncashmentAmount", "CalculationDetails", "IsPaid", "PaidDate", "PaymentReference", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveTypes; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."LeaveTypes" ("Id", "TypeCode", "Name", "Description", "DefaultEntitlement", "RequiresApproval", "IsPaid", "CanCarryForward", "MaxCarryForwardDays", "RequiresDocumentation", "MinDaysNotice", "MaxConsecutiveDays", "IsActive", "ApprovalWorkflow", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Locations; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."Locations" ("Id", "LocationCode", "LocationName", "LocationType", "Address", "City", "State", "PostalCode", "Country", "Latitude", "Longitude", "Timezone", "ContactPerson", "ContactPhone", "ContactEmail", "IsHeadOffice", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: PayrollCycles; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."PayrollCycles" ("Id", "Month", "Year", "Status", "TotalGrossSalary", "TotalDeductions", "TotalNetSalary", "TotalNPFEmployee", "TotalNPFEmployer", "TotalNSFEmployee", "TotalNSFEmployer", "TotalCSGEmployee", "TotalCSGEmployer", "TotalPRGF", "TotalTrainingLevy", "TotalPAYE", "TotalOvertimePay", "ProcessedBy", "ProcessedAt", "ApprovedBy", "ApprovedAt", "PaymentDate", "Notes", "EmployeeCount", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Payslips; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."Payslips" ("Id", "PayrollCycleId", "EmployeeId", "Month", "Year", "PayslipNumber", "BasicSalary", "HousingAllowance", "TransportAllowance", "MealAllowance", "MobileAllowance", "OtherAllowances", "OvertimeHours", "OvertimePay", "ThirteenthMonthBonus", "LeaveEncashment", "GratuityPayment", "Commission", "TotalGrossSalary", "WorkingDays", "ActualDaysWorked", "PaidLeaveDays", "UnpaidLeaveDays", "LeaveDeductions", "NPF_Employee", "NSF_Employee", "CSG_Employee", "PAYE_Tax", "NPF_Employer", "NSF_Employer", "CSG_Employer", "PRGF_Contribution", "TrainingLevy", "LoanDeduction", "AdvanceDeduction", "MedicalInsurance", "OtherDeductions", "TotalDeductions", "NetSalary", "PaymentStatus", "PaidAt", "PaymentMethod", "PaymentReference", "BankAccountNumber", "Remarks", "IsDelivered", "DeliveredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "IsCalculatedFromTimesheets", "TimesheetIdsJson", "TimesheetsProcessed") FROM stdin;
\.


--
-- Data for Name: PublicHolidays; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."PublicHolidays" ("Id", "Name", "Date", "Year", "Type", "Description", "IsRecurring", "Country", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: SalaryComponents; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."SalaryComponents" ("Id", "EmployeeId", "ComponentType", "ComponentName", "Amount", "Currency", "IsRecurring", "IsDeduction", "IsTaxable", "IncludeInStatutory", "EffectiveFrom", "EffectiveTo", "IsActive", "Description", "CalculationMethod", "PercentageBase", "CalculationOrder", "RequiresApproval", "IsApproved", "ApprovedBy", "ApprovedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TenantCustomComplianceRules; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."TenantCustomComplianceRules" ("Id", "SectorComplianceRuleId", "IsUsingDefault", "CustomRuleConfig", "Justification", "ApprovedByUserId", "ApprovedAt", "RuleCategory", "RuleName", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TenantSectorConfigurations; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."TenantSectorConfigurations" ("Id", "SectorId", "SelectedAt", "SelectedByUserId", "Notes", "SectorName", "SectorCode", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TimesheetAdjustments; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."TimesheetAdjustments" ("Id", "TimesheetEntryId", "AdjustmentType", "FieldName", "OldValue", "NewValue", "Reason", "AdjustedBy", "AdjustedByName", "AdjustedAt", "Status", "ApprovedBy", "ApprovedByName", "ApprovedAt", "RejectionReason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TimesheetComments; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."TimesheetComments" ("Id", "TimesheetId", "UserId", "UserName", "Comment", "CommentedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TimesheetEntries; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."TimesheetEntries" ("Id", "TimesheetId", "Date", "AttendanceId", "ClockInTime", "ClockOutTime", "BreakDuration", "ActualHours", "RegularHours", "OvertimeHours", "HolidayHours", "SickLeaveHours", "AnnualLeaveHours", "IsAbsent", "IsHoliday", "IsWeekend", "IsOnLeave", "DayType", "Notes", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Timesheets; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."Timesheets" ("Id", "EmployeeId", "PeriodType", "PeriodStart", "PeriodEnd", "TotalRegularHours", "TotalOvertimeHours", "TotalHolidayHours", "TotalSickLeaveHours", "TotalAnnualLeaveHours", "TotalAbsentHours", "Status", "SubmittedAt", "SubmittedBy", "ApprovedAt", "ApprovedBy", "ApprovedByName", "RejectedAt", "RejectedBy", "RejectionReason", "IsLocked", "LockedAt", "LockedBy", "Notes", "TenantId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: tenant_default; Owner: postgres
--

COPY tenant_default."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20251106053857_AddMultiDeviceBiometricAttendanceSystem	8.0.0
\.


--
-- Data for Name: AttendanceAnomalies; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."AttendanceAnomalies" ("Id", "AttendanceId", "EmployeeId", "DeviceId", "LocationId", "AnomalyType", "AnomalyDate", "AnomalySeverity", "Description", "DetectionMethod", "IsResolved", "ResolvedAt", "ResolvedBy", "ResolutionNote", "ActionTaken", "AnomalyDetailsJson", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: AttendanceCorrections; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."AttendanceCorrections" ("Id", "AttendanceId", "EmployeeId", "RequestedBy", "OriginalCheckIn", "OriginalCheckOut", "CorrectedCheckIn", "CorrectedCheckOut", "Reason", "Status", "ApprovedBy", "ApprovedAt", "RejectionReason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: AttendanceMachines; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."AttendanceMachines" ("Id", "MachineName", "MachineId", "IpAddress", "DepartmentId", "IsActive", "SerialNumber", "Model", "ZKTecoDeviceId", "Port", "LastSyncAt", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt", "DeletedBy", "LegacyLocation", "DeviceCode", "DeviceType", "LocationId", "MacAddress", "FirmwareVersion", "ConnectionMethod", "ConnectionTimeoutSeconds", "DeviceConfigJson", "DeviceStatus", "SyncEnabled", "SyncIntervalMinutes", "LastSyncTime", "LastSyncStatus", "LastSyncRecordCount", "OfflineAlertEnabled", "OfflineThresholdMinutes") FROM stdin;
\.


--
-- Data for Name: Attendances; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."Attendances" ("Id", "EmployeeId", "Date", "CheckInTime", "CheckOutTime", "WorkingHours", "OvertimeHours", "Status", "LateArrivalMinutes", "EarlyDepartureMinutes", "Remarks", "IsRegularized", "RegularizedBy", "RegularizedAt", "ShiftId", "AttendanceMachineId", "OvertimeRate", "IsSunday", "IsPublicHoliday", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt", "DeletedBy", "DeviceId", "LocationId", "DeviceUserId", "PunchSource", "VerificationMethod", "IsAuthorized", "AuthorizationNote") FROM stdin;
ae7dbabe-1c29-4ae8-a342-238ac8f09f63	ed37a477-51ff-4660-a871-216f42f5692a	2025-11-05 00:00:00+00	\N	\N	0.00	0.00	2	\N	\N	Automatically marked absent by system	f	\N	\N	\N	\N	\N	f	f	\N	\N	2025-11-05 02:56:24.477677+00	2025-11-05 02:56:24.477695+00	f	\N	\N	\N	\N	\N	Biometric	\N	t	\N
5e007f34-4c4e-4348-aaf7-43cf58203846	ed37a477-51ff-4660-a871-216f42f5692a	2025-11-06 00:00:00+00	\N	\N	0.00	0.00	2	\N	\N	Automatically marked absent by system	f	\N	\N	\N	\N	\N	f	f	\N	\N	2025-11-06 03:49:43.148346+00	2025-11-06 03:49:43.148366+00	f	\N	\N	\N	\N	\N	Biometric	\N	t	\N
\.


--
-- Data for Name: Departments; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."Departments" ("Id", "Name", "Code", "Description", "ParentDepartmentId", "DepartmentHeadId", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "CostCenterCode") FROM stdin;
b97c5799-a5bc-457a-967c-4fe1f842b037	Human Resources	HR	Human Resources Department	\N	\N	t	2025-11-04 06:11:28.664583+00	System	\N	\N	f	\N	\N	\N
b463f778-7a08-4107-b3d8-4f389dfd4896	Finance	FIN	Finance Department	\N	\N	t	2025-11-04 06:11:28.664583+00	System	\N	\N	f	\N	\N	\N
db9d19e8-f042-40f7-bfe6-859845dbcb4e	IT	IT	Information Technology Department	\N	\N	t	2025-11-04 06:11:28.664583+00	System	\N	\N	f	\N	\N	\N
\.


--
-- Data for Name: DeviceSyncLogs; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."DeviceSyncLogs" ("Id", "DeviceId", "SyncStartTime", "SyncEndTime", "SyncDurationSeconds", "SyncStatus", "RecordsFetched", "RecordsProcessed", "RecordsInserted", "RecordsUpdated", "RecordsSkipped", "RecordsErrored", "SyncMethod", "DateRangeFrom", "DateRangeTo", "ErrorMessage", "ErrorDetailsJson", "InitiatedBy", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: EmergencyContacts; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."EmergencyContacts" ("Id", "EmployeeId", "ContactName", "PhoneNumber", "AlternatePhoneNumber", "Email", "Relationship", "ContactType", "Address", "Country", "IsPrimary", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: EmployeeDeviceAccesses; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."EmployeeDeviceAccesses" ("Id", "EmployeeId", "DeviceId", "AccessType", "AccessReason", "ValidFrom", "ValidUntil", "AllowedDaysJson", "AllowedTimeStart", "AllowedTimeEnd", "IsActive", "ApprovedBy", "ApprovedDate", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Employees; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."Employees" ("Id", "EmployeeCode", "FirstName", "LastName", "MiddleName", "Email", "PhoneNumber", "PersonalEmail", "DateOfBirth", "Gender", "MaritalStatus", "Address", "City", "PostalCode", "EmployeeType", "Nationality", "CountryOfOrigin", "NationalIdCard", "PassportNumber", "PassportIssueDate", "PassportExpiryDate", "VisaType", "VisaNumber", "VisaIssueDate", "VisaExpiryDate", "WorkPermitNumber", "WorkPermitExpiryDate", "TaxResidentStatus", "TaxIdNumber", "NPFNumber", "NSFNumber", "PRGFNumber", "IsNPFEligible", "IsNSFEligible", "JobTitle", "DepartmentId", "ManagerId", "JoiningDate", "ProbationEndDate", "ConfirmationDate", "ResignationDate", "LastWorkingDate", "IsActive", "ContractEndDate", "BasicSalary", "BankName", "BankAccountNumber", "BankBranch", "BankSwiftCode", "SalaryCurrency", "AnnualLeaveBalance", "SickLeaveBalance", "CasualLeaveBalance", "OffboardingReason", "IsOffboarded", "OffboardingDate", "OffboardingNotes", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "AccessFailedCount", "LockoutEnabled", "LockoutEnd", "PasswordHash", "BloodGroup", "AnnualLeaveDays", "SickLeaveDays", "CasualLeaveDays", "CarryForwardAllowed", "HighestQualification", "University", "Skills", "Languages", "ResumeFilePath", "IdCopyFilePath", "CertificatesFilePath", "ContractFilePath", "Notes", "CSGNumber", "Designation", "IndustrySector", "WorkLocation", "ReportingTo", "ProbationPeriodMonths", "EmploymentContractType", "AlternateEmail", "EmploymentStatus", "PaymentFrequency", "PRNumber", "TransportAllowance", "HousingAllowance", "MealAllowance", "WorkPermitType", "ResidencePermitNumber", "EmergencyContactName", "EmergencyContactRelation", "EmergencyContactPhone", "EmergencyContactAddress", "BiometricEnrollmentId", "BiometricEnrollmentDate", "PrimaryLocationId") FROM stdin;
ed37a477-51ff-4660-a871-216f42f5692a	ADMIN001	Siraaj	Administrator		sppcoltd.mu@gmail.com	+230-0000000	\N	1990-01-01 00:00:00+00	0	0	Head Office	\N	\N	0	Mauritius	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	0	\N	\N	\N	\N	t	t	System Administrator	b97c5799-a5bc-457a-967c-4fe1f842b037	\N	2025-11-04 06:17:17.026423+00	\N	\N	\N	\N	t	\N	50000.00	\N	\N	\N	\N	MUR	20.00	15.00	10.00	\N	f	\N	\N	2025-11-04 06:17:17.026423+00	\N	2025-11-06 07:29:26.150404+00	\N	f	\N	\N	0	t	\N	/hbC/IIuUHkt9tw9pUJIqc0kzxJbIOAt8LxP1Gf7TBvJnQuRPq6Ry/Wm4vMlWA5i	\N	20	15	5	t	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N			\N	\N	\N	Permanent	\N	Active	0	\N	5000.00	10000.00	3000.00	\N	\N	\N	\N	\N	\N	\N	\N	\N
\.


--
-- Data for Name: LeaveApplications; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."LeaveApplications" ("Id", "ApplicationNumber", "EmployeeId", "LeaveTypeId", "StartDate", "EndDate", "TotalDays", "CalculationType", "Reason", "ContactNumber", "ContactAddress", "Status", "AppliedDate", "ApprovedDate", "ApprovedBy", "ApproverComments", "RejectedDate", "RejectedBy", "RejectionReason", "CancelledDate", "CancelledBy", "CancellationReason", "AttachmentPath", "RequiresHRApproval", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveApprovals; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."LeaveApprovals" ("Id", "LeaveApplicationId", "ApprovalLevel", "ApproverRole", "ApproverId", "Status", "ActionDate", "Comments", "RequestedInfo", "IsCurrentLevel", "IsComplete", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveBalances; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."LeaveBalances" ("Id", "EmployeeId", "LeaveTypeId", "Year", "TotalEntitlement", "UsedDays", "PendingDays", "CarriedForward", "Accrued", "LastAccrualDate", "ExpiryDate", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveEncashments; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."LeaveEncashments" ("Id", "EmployeeId", "CalculationDate", "LastWorkingDay", "UnusedAnnualLeaveDays", "UnusedSickLeaveDays", "TotalEncashableDays", "DailySalary", "TotalEncashmentAmount", "CalculationDetails", "IsPaid", "PaidDate", "PaymentReference", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: LeaveTypes; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."LeaveTypes" ("Id", "TypeCode", "Name", "Description", "DefaultEntitlement", "RequiresApproval", "IsPaid", "CanCarryForward", "MaxCarryForwardDays", "RequiresDocumentation", "MinDaysNotice", "MaxConsecutiveDays", "IsActive", "ApprovalWorkflow", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Locations; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."Locations" ("Id", "LocationCode", "LocationName", "LocationType", "Address", "City", "State", "PostalCode", "Country", "Latitude", "Longitude", "Timezone", "ContactPerson", "ContactPhone", "ContactEmail", "IsHeadOffice", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: PayrollCycles; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."PayrollCycles" ("Id", "Month", "Year", "Status", "TotalGrossSalary", "TotalDeductions", "TotalNetSalary", "TotalNPFEmployee", "TotalNPFEmployer", "TotalNSFEmployee", "TotalNSFEmployer", "TotalCSGEmployee", "TotalCSGEmployer", "TotalPRGF", "TotalTrainingLevy", "TotalPAYE", "TotalOvertimePay", "ProcessedBy", "ProcessedAt", "ApprovedBy", "ApprovedAt", "PaymentDate", "Notes", "EmployeeCount", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Payslips; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."Payslips" ("Id", "PayrollCycleId", "EmployeeId", "Month", "Year", "PayslipNumber", "BasicSalary", "HousingAllowance", "TransportAllowance", "MealAllowance", "MobileAllowance", "OtherAllowances", "OvertimeHours", "OvertimePay", "ThirteenthMonthBonus", "LeaveEncashment", "GratuityPayment", "Commission", "TotalGrossSalary", "WorkingDays", "ActualDaysWorked", "PaidLeaveDays", "UnpaidLeaveDays", "LeaveDeductions", "NPF_Employee", "NSF_Employee", "CSG_Employee", "PAYE_Tax", "NPF_Employer", "NSF_Employer", "CSG_Employer", "PRGF_Contribution", "TrainingLevy", "LoanDeduction", "AdvanceDeduction", "MedicalInsurance", "OtherDeductions", "TotalDeductions", "NetSalary", "PaymentStatus", "PaidAt", "PaymentMethod", "PaymentReference", "BankAccountNumber", "Remarks", "IsDelivered", "DeliveredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy", "IsCalculatedFromTimesheets", "TimesheetIdsJson", "TimesheetsProcessed") FROM stdin;
\.


--
-- Data for Name: PublicHolidays; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."PublicHolidays" ("Id", "Name", "Date", "Year", "Type", "Description", "IsRecurring", "Country", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: SalaryComponents; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."SalaryComponents" ("Id", "EmployeeId", "ComponentType", "ComponentName", "Amount", "Currency", "IsRecurring", "IsDeduction", "IsTaxable", "IncludeInStatutory", "EffectiveFrom", "EffectiveTo", "IsActive", "Description", "CalculationMethod", "PercentageBase", "CalculationOrder", "RequiresApproval", "IsApproved", "ApprovedBy", "ApprovedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TenantCustomComplianceRules; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."TenantCustomComplianceRules" ("Id", "SectorComplianceRuleId", "IsUsingDefault", "CustomRuleConfig", "Justification", "ApprovedByUserId", "ApprovedAt", "RuleCategory", "RuleName", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TenantSectorConfigurations; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."TenantSectorConfigurations" ("Id", "SectorId", "SelectedAt", "SelectedByUserId", "Notes", "SectorName", "SectorCode", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TimesheetAdjustments; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."TimesheetAdjustments" ("Id", "TimesheetEntryId", "AdjustmentType", "FieldName", "OldValue", "NewValue", "Reason", "AdjustedBy", "AdjustedByName", "AdjustedAt", "Status", "ApprovedBy", "ApprovedByName", "ApprovedAt", "RejectionReason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TimesheetComments; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."TimesheetComments" ("Id", "TimesheetId", "UserId", "UserName", "Comment", "CommentedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: TimesheetEntries; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."TimesheetEntries" ("Id", "TimesheetId", "Date", "AttendanceId", "ClockInTime", "ClockOutTime", "BreakDuration", "ActualHours", "RegularHours", "OvertimeHours", "HolidayHours", "SickLeaveHours", "AnnualLeaveHours", "IsAbsent", "IsHoliday", "IsWeekend", "IsOnLeave", "DayType", "Notes", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Timesheets; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."Timesheets" ("Id", "EmployeeId", "PeriodType", "PeriodStart", "PeriodEnd", "TotalRegularHours", "TotalOvertimeHours", "TotalHolidayHours", "TotalSickLeaveHours", "TotalAnnualLeaveHours", "TotalAbsentHours", "Status", "SubmittedAt", "SubmittedBy", "ApprovedAt", "ApprovedBy", "ApprovedByName", "RejectedAt", "RejectedBy", "RejectionReason", "IsLocked", "LockedAt", "LockedBy", "Notes", "TenantId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: tenant_siraaj; Owner: postgres
--

COPY tenant_siraaj."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20251106053857_AddMultiDeviceBiometricAttendanceSystem	8.0.0
\.


--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.aggregatedcounter_id_seq', 27, true);


--
-- Name: counter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.counter_id_seq', 36, true);


--
-- Name: hash_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.hash_id_seq', 34, true);


--
-- Name: job_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.job_id_seq', 11, true);


--
-- Name: jobparameter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.jobparameter_id_seq', 34, true);


--
-- Name: jobqueue_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.jobqueue_id_seq', 17, true);


--
-- Name: list_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.list_id_seq', 1, false);


--
-- Name: set_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.set_id_seq', 27, true);


--
-- Name: state_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.state_id_seq', 57, true);


--
-- Name: IndustrySectors_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."IndustrySectors_Id_seq"', 1, false);


--
-- Name: SectorComplianceRules_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."SectorComplianceRules_Id_seq"', 1, false);


--
-- Name: aggregatedcounter aggregatedcounter_key_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.aggregatedcounter
    ADD CONSTRAINT aggregatedcounter_key_key UNIQUE (key);


--
-- Name: aggregatedcounter aggregatedcounter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.aggregatedcounter
    ADD CONSTRAINT aggregatedcounter_pkey PRIMARY KEY (id);


--
-- Name: counter counter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.counter
    ADD CONSTRAINT counter_pkey PRIMARY KEY (id);


--
-- Name: hash hash_key_field_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.hash
    ADD CONSTRAINT hash_key_field_key UNIQUE (key, field);


--
-- Name: hash hash_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.hash
    ADD CONSTRAINT hash_pkey PRIMARY KEY (id);


--
-- Name: job job_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.job
    ADD CONSTRAINT job_pkey PRIMARY KEY (id);


--
-- Name: jobparameter jobparameter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobparameter
    ADD CONSTRAINT jobparameter_pkey PRIMARY KEY (id);


--
-- Name: jobqueue jobqueue_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobqueue
    ADD CONSTRAINT jobqueue_pkey PRIMARY KEY (id);


--
-- Name: list list_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.list
    ADD CONSTRAINT list_pkey PRIMARY KEY (id);


--
-- Name: lock lock_resource_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.lock
    ADD CONSTRAINT lock_resource_key UNIQUE (resource);

ALTER TABLE ONLY hangfire.lock REPLICA IDENTITY USING INDEX lock_resource_key;


--
-- Name: schema schema_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.schema
    ADD CONSTRAINT schema_pkey PRIMARY KEY (version);


--
-- Name: server server_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.server
    ADD CONSTRAINT server_pkey PRIMARY KEY (id);


--
-- Name: set set_key_value_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.set
    ADD CONSTRAINT set_key_value_key UNIQUE (key, value);


--
-- Name: set set_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.set
    ADD CONSTRAINT set_pkey PRIMARY KEY (id);


--
-- Name: state state_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.state
    ADD CONSTRAINT state_pkey PRIMARY KEY (id);


--
-- Name: AdminUsers PK_AdminUsers; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."AdminUsers"
    ADD CONSTRAINT "PK_AdminUsers" PRIMARY KEY ("Id");


--
-- Name: AuditLogs PK_AuditLogs; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."AuditLogs"
    ADD CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id");


--
-- Name: IndustrySectors PK_IndustrySectors; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."IndustrySectors"
    ADD CONSTRAINT "PK_IndustrySectors" PRIMARY KEY ("Id");


--
-- Name: SectorComplianceRules PK_SectorComplianceRules; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SectorComplianceRules"
    ADD CONSTRAINT "PK_SectorComplianceRules" PRIMARY KEY ("Id");


--
-- Name: Tenants PK_Tenants; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Tenants"
    ADD CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: AttendanceAnomalies PK_AttendanceAnomalies_tenant_default; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."AttendanceAnomalies"
    ADD CONSTRAINT "PK_AttendanceAnomalies_tenant_default" PRIMARY KEY ("Id");


--
-- Name: AttendanceCorrections PK_AttendanceCorrections; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."AttendanceCorrections"
    ADD CONSTRAINT "PK_AttendanceCorrections" PRIMARY KEY ("Id");


--
-- Name: AttendanceMachines PK_AttendanceMachines; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."AttendanceMachines"
    ADD CONSTRAINT "PK_AttendanceMachines" PRIMARY KEY ("Id");


--
-- Name: Attendances PK_Attendances; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Attendances"
    ADD CONSTRAINT "PK_Attendances" PRIMARY KEY ("Id");


--
-- Name: Departments PK_Departments; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Departments"
    ADD CONSTRAINT "PK_Departments" PRIMARY KEY ("Id");


--
-- Name: DeviceSyncLogs PK_DeviceSyncLogs_tenant_default; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."DeviceSyncLogs"
    ADD CONSTRAINT "PK_DeviceSyncLogs_tenant_default" PRIMARY KEY ("Id");


--
-- Name: EmergencyContacts PK_EmergencyContacts; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."EmergencyContacts"
    ADD CONSTRAINT "PK_EmergencyContacts" PRIMARY KEY ("Id");


--
-- Name: EmployeeDeviceAccesses PK_EmployeeDeviceAccesses_tenant_default; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."EmployeeDeviceAccesses"
    ADD CONSTRAINT "PK_EmployeeDeviceAccesses_tenant_default" PRIMARY KEY ("Id");


--
-- Name: Employees PK_Employees; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Employees"
    ADD CONSTRAINT "PK_Employees" PRIMARY KEY ("Id");


--
-- Name: LeaveApplications PK_LeaveApplications; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApplications"
    ADD CONSTRAINT "PK_LeaveApplications" PRIMARY KEY ("Id");


--
-- Name: LeaveApprovals PK_LeaveApprovals; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApprovals"
    ADD CONSTRAINT "PK_LeaveApprovals" PRIMARY KEY ("Id");


--
-- Name: LeaveBalances PK_LeaveBalances; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveBalances"
    ADD CONSTRAINT "PK_LeaveBalances" PRIMARY KEY ("Id");


--
-- Name: LeaveEncashments PK_LeaveEncashments; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveEncashments"
    ADD CONSTRAINT "PK_LeaveEncashments" PRIMARY KEY ("Id");


--
-- Name: LeaveTypes PK_LeaveTypes; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveTypes"
    ADD CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id");


--
-- Name: Locations PK_Locations_tenant_default; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Locations"
    ADD CONSTRAINT "PK_Locations_tenant_default" PRIMARY KEY ("Id");


--
-- Name: PayrollCycles PK_PayrollCycles; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."PayrollCycles"
    ADD CONSTRAINT "PK_PayrollCycles" PRIMARY KEY ("Id");


--
-- Name: Payslips PK_Payslips; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Payslips"
    ADD CONSTRAINT "PK_Payslips" PRIMARY KEY ("Id");


--
-- Name: PublicHolidays PK_PublicHolidays; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."PublicHolidays"
    ADD CONSTRAINT "PK_PublicHolidays" PRIMARY KEY ("Id");


--
-- Name: SalaryComponents PK_SalaryComponents; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."SalaryComponents"
    ADD CONSTRAINT "PK_SalaryComponents" PRIMARY KEY ("Id");


--
-- Name: TenantCustomComplianceRules PK_TenantCustomComplianceRules; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TenantCustomComplianceRules"
    ADD CONSTRAINT "PK_TenantCustomComplianceRules" PRIMARY KEY ("Id");


--
-- Name: TenantSectorConfigurations PK_TenantSectorConfigurations; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TenantSectorConfigurations"
    ADD CONSTRAINT "PK_TenantSectorConfigurations" PRIMARY KEY ("Id");


--
-- Name: TimesheetAdjustments PK_TimesheetAdjustments; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetAdjustments"
    ADD CONSTRAINT "PK_TimesheetAdjustments" PRIMARY KEY ("Id");


--
-- Name: TimesheetComments PK_TimesheetComments; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetComments"
    ADD CONSTRAINT "PK_TimesheetComments" PRIMARY KEY ("Id");


--
-- Name: TimesheetEntries PK_TimesheetEntries; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetEntries"
    ADD CONSTRAINT "PK_TimesheetEntries" PRIMARY KEY ("Id");


--
-- Name: Timesheets PK_Timesheets; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Timesheets"
    ADD CONSTRAINT "PK_Timesheets" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: AttendanceAnomalies PK_AttendanceAnomalies_tenant_siraaj; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."AttendanceAnomalies"
    ADD CONSTRAINT "PK_AttendanceAnomalies_tenant_siraaj" PRIMARY KEY ("Id");


--
-- Name: AttendanceCorrections PK_AttendanceCorrections; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."AttendanceCorrections"
    ADD CONSTRAINT "PK_AttendanceCorrections" PRIMARY KEY ("Id");


--
-- Name: AttendanceMachines PK_AttendanceMachines; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."AttendanceMachines"
    ADD CONSTRAINT "PK_AttendanceMachines" PRIMARY KEY ("Id");


--
-- Name: Attendances PK_Attendances; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Attendances"
    ADD CONSTRAINT "PK_Attendances" PRIMARY KEY ("Id");


--
-- Name: Departments PK_Departments; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Departments"
    ADD CONSTRAINT "PK_Departments" PRIMARY KEY ("Id");


--
-- Name: DeviceSyncLogs PK_DeviceSyncLogs_tenant_siraaj; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."DeviceSyncLogs"
    ADD CONSTRAINT "PK_DeviceSyncLogs_tenant_siraaj" PRIMARY KEY ("Id");


--
-- Name: EmergencyContacts PK_EmergencyContacts; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."EmergencyContacts"
    ADD CONSTRAINT "PK_EmergencyContacts" PRIMARY KEY ("Id");


--
-- Name: EmployeeDeviceAccesses PK_EmployeeDeviceAccesses_tenant_siraaj; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."EmployeeDeviceAccesses"
    ADD CONSTRAINT "PK_EmployeeDeviceAccesses_tenant_siraaj" PRIMARY KEY ("Id");


--
-- Name: Employees PK_Employees; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Employees"
    ADD CONSTRAINT "PK_Employees" PRIMARY KEY ("Id");


--
-- Name: LeaveApplications PK_LeaveApplications; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApplications"
    ADD CONSTRAINT "PK_LeaveApplications" PRIMARY KEY ("Id");


--
-- Name: LeaveApprovals PK_LeaveApprovals; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApprovals"
    ADD CONSTRAINT "PK_LeaveApprovals" PRIMARY KEY ("Id");


--
-- Name: LeaveBalances PK_LeaveBalances; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveBalances"
    ADD CONSTRAINT "PK_LeaveBalances" PRIMARY KEY ("Id");


--
-- Name: LeaveEncashments PK_LeaveEncashments; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveEncashments"
    ADD CONSTRAINT "PK_LeaveEncashments" PRIMARY KEY ("Id");


--
-- Name: LeaveTypes PK_LeaveTypes; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveTypes"
    ADD CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id");


--
-- Name: Locations PK_Locations_tenant_siraaj; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Locations"
    ADD CONSTRAINT "PK_Locations_tenant_siraaj" PRIMARY KEY ("Id");


--
-- Name: PayrollCycles PK_PayrollCycles; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."PayrollCycles"
    ADD CONSTRAINT "PK_PayrollCycles" PRIMARY KEY ("Id");


--
-- Name: Payslips PK_Payslips; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Payslips"
    ADD CONSTRAINT "PK_Payslips" PRIMARY KEY ("Id");


--
-- Name: PublicHolidays PK_PublicHolidays; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."PublicHolidays"
    ADD CONSTRAINT "PK_PublicHolidays" PRIMARY KEY ("Id");


--
-- Name: SalaryComponents PK_SalaryComponents; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."SalaryComponents"
    ADD CONSTRAINT "PK_SalaryComponents" PRIMARY KEY ("Id");


--
-- Name: TenantCustomComplianceRules PK_TenantCustomComplianceRules; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TenantCustomComplianceRules"
    ADD CONSTRAINT "PK_TenantCustomComplianceRules" PRIMARY KEY ("Id");


--
-- Name: TenantSectorConfigurations PK_TenantSectorConfigurations; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TenantSectorConfigurations"
    ADD CONSTRAINT "PK_TenantSectorConfigurations" PRIMARY KEY ("Id");


--
-- Name: TimesheetAdjustments PK_TimesheetAdjustments; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetAdjustments"
    ADD CONSTRAINT "PK_TimesheetAdjustments" PRIMARY KEY ("Id");


--
-- Name: TimesheetComments PK_TimesheetComments; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetComments"
    ADD CONSTRAINT "PK_TimesheetComments" PRIMARY KEY ("Id");


--
-- Name: TimesheetEntries PK_TimesheetEntries; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetEntries"
    ADD CONSTRAINT "PK_TimesheetEntries" PRIMARY KEY ("Id");


--
-- Name: Timesheets PK_Timesheets; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Timesheets"
    ADD CONSTRAINT "PK_Timesheets" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: ix_hangfire_counter_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_counter_expireat ON hangfire.counter USING btree (expireat);


--
-- Name: ix_hangfire_counter_key; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_counter_key ON hangfire.counter USING btree (key);


--
-- Name: ix_hangfire_hash_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_hash_expireat ON hangfire.hash USING btree (expireat);


--
-- Name: ix_hangfire_job_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_job_expireat ON hangfire.job USING btree (expireat);


--
-- Name: ix_hangfire_job_statename; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_job_statename ON hangfire.job USING btree (statename);


--
-- Name: ix_hangfire_job_statename_is_not_null; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_job_statename_is_not_null ON hangfire.job USING btree (statename) INCLUDE (id) WHERE (statename IS NOT NULL);


--
-- Name: ix_hangfire_jobparameter_jobidandname; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobparameter_jobidandname ON hangfire.jobparameter USING btree (jobid, name);


--
-- Name: ix_hangfire_jobqueue_fetchedat_queue_jobid; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobqueue_fetchedat_queue_jobid ON hangfire.jobqueue USING btree (fetchedat NULLS FIRST, queue, jobid);


--
-- Name: ix_hangfire_jobqueue_jobidandqueue; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobqueue_jobidandqueue ON hangfire.jobqueue USING btree (jobid, queue);


--
-- Name: ix_hangfire_jobqueue_queueandfetchedat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobqueue_queueandfetchedat ON hangfire.jobqueue USING btree (queue, fetchedat);


--
-- Name: ix_hangfire_list_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_list_expireat ON hangfire.list USING btree (expireat);


--
-- Name: ix_hangfire_set_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_set_expireat ON hangfire.set USING btree (expireat);


--
-- Name: ix_hangfire_set_key_score; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_set_key_score ON hangfire.set USING btree (key, score);


--
-- Name: ix_hangfire_state_jobid; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_state_jobid ON hangfire.state USING btree (jobid);


--
-- Name: IX_AdminUsers_Email; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_AdminUsers_Email" ON master."AdminUsers" USING btree ("Email");


--
-- Name: IX_AdminUsers_UserName; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_AdminUsers_UserName" ON master."AdminUsers" USING btree ("UserName");


--
-- Name: IX_AuditLogs_EntityType_EntityId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_EntityType_EntityId" ON master."AuditLogs" USING btree ("EntityType", "EntityId");


--
-- Name: IX_AuditLogs_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_PerformedAt" ON master."AuditLogs" USING btree ("PerformedAt");


--
-- Name: IX_AuditLogs_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_TenantId" ON master."AuditLogs" USING btree ("TenantId");


--
-- Name: IX_IndustrySectors_IsActive; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_IndustrySectors_IsActive" ON master."IndustrySectors" USING btree ("IsActive");


--
-- Name: IX_IndustrySectors_ParentSectorId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_IndustrySectors_ParentSectorId" ON master."IndustrySectors" USING btree ("ParentSectorId");


--
-- Name: IX_IndustrySectors_SectorCode; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_IndustrySectors_SectorCode" ON master."IndustrySectors" USING btree ("SectorCode");


--
-- Name: IX_SectorComplianceRules_EffectiveFrom_EffectiveTo; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SectorComplianceRules_EffectiveFrom_EffectiveTo" ON master."SectorComplianceRules" USING btree ("EffectiveFrom", "EffectiveTo");


--
-- Name: IX_SectorComplianceRules_RuleCategory; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SectorComplianceRules_RuleCategory" ON master."SectorComplianceRules" USING btree ("RuleCategory");


--
-- Name: IX_SectorComplianceRules_SectorId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SectorComplianceRules_SectorId" ON master."SectorComplianceRules" USING btree ("SectorId");


--
-- Name: IX_Tenants_ContactEmail; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Tenants_ContactEmail" ON master."Tenants" USING btree ("ContactEmail");


--
-- Name: IX_Tenants_SchemaName; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Tenants_SchemaName" ON master."Tenants" USING btree ("SchemaName");


--
-- Name: IX_Tenants_SectorId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Tenants_SectorId" ON master."Tenants" USING btree ("SectorId");


--
-- Name: IX_Tenants_Subdomain; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Tenants_Subdomain" ON master."Tenants" USING btree ("Subdomain");


--
-- Name: IX_AttendanceCorrections_AttendanceId_Status; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_AttendanceCorrections_AttendanceId_Status" ON tenant_default."AttendanceCorrections" USING btree ("AttendanceId", "Status");


--
-- Name: IX_AttendanceCorrections_EmployeeId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_AttendanceCorrections_EmployeeId" ON tenant_default."AttendanceCorrections" USING btree ("EmployeeId");


--
-- Name: IX_AttendanceCorrections_RequestedBy; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_AttendanceCorrections_RequestedBy" ON tenant_default."AttendanceCorrections" USING btree ("RequestedBy");


--
-- Name: IX_AttendanceMachines_IpAddress; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_AttendanceMachines_IpAddress" ON tenant_default."AttendanceMachines" USING btree ("IpAddress");


--
-- Name: IX_AttendanceMachines_SerialNumber; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_AttendanceMachines_SerialNumber" ON tenant_default."AttendanceMachines" USING btree ("SerialNumber");


--
-- Name: IX_AttendanceMachines_ZKTecoDeviceId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_AttendanceMachines_ZKTecoDeviceId" ON tenant_default."AttendanceMachines" USING btree ("ZKTecoDeviceId");


--
-- Name: IX_Attendances_Date; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Attendances_Date" ON tenant_default."Attendances" USING btree ("Date");


--
-- Name: IX_Attendances_EmployeeId_Date; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Attendances_EmployeeId_Date" ON tenant_default."Attendances" USING btree ("EmployeeId", "Date");


--
-- Name: IX_Attendances_Status; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Attendances_Status" ON tenant_default."Attendances" USING btree ("Status");


--
-- Name: IX_Departments_Code; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Departments_Code" ON tenant_default."Departments" USING btree ("Code");


--
-- Name: IX_Departments_DepartmentHeadId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Departments_DepartmentHeadId" ON tenant_default."Departments" USING btree ("DepartmentHeadId");


--
-- Name: IX_Departments_ParentDepartmentId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Departments_ParentDepartmentId" ON tenant_default."Departments" USING btree ("ParentDepartmentId");


--
-- Name: IX_EmergencyContacts_EmployeeId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_EmergencyContacts_EmployeeId" ON tenant_default."EmergencyContacts" USING btree ("EmployeeId");


--
-- Name: IX_Employees_DepartmentId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Employees_DepartmentId" ON tenant_default."Employees" USING btree ("DepartmentId");


--
-- Name: IX_Employees_Email; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Employees_Email" ON tenant_default."Employees" USING btree ("Email");


--
-- Name: IX_Employees_EmployeeCode; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Employees_EmployeeCode" ON tenant_default."Employees" USING btree ("EmployeeCode");


--
-- Name: IX_Employees_ManagerId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Employees_ManagerId" ON tenant_default."Employees" USING btree ("ManagerId");


--
-- Name: IX_Employees_NationalIdCard; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Employees_NationalIdCard" ON tenant_default."Employees" USING btree ("NationalIdCard");


--
-- Name: IX_Employees_PassportNumber; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Employees_PassportNumber" ON tenant_default."Employees" USING btree ("PassportNumber");


--
-- Name: IX_LeaveApplications_ApplicationNumber; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_LeaveApplications_ApplicationNumber" ON tenant_default."LeaveApplications" USING btree ("ApplicationNumber");


--
-- Name: IX_LeaveApplications_ApprovedBy; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_ApprovedBy" ON tenant_default."LeaveApplications" USING btree ("ApprovedBy");


--
-- Name: IX_LeaveApplications_EmployeeId_StartDate_EndDate; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_EmployeeId_StartDate_EndDate" ON tenant_default."LeaveApplications" USING btree ("EmployeeId", "StartDate", "EndDate");


--
-- Name: IX_LeaveApplications_LeaveTypeId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_LeaveTypeId" ON tenant_default."LeaveApplications" USING btree ("LeaveTypeId");


--
-- Name: IX_LeaveApplications_RejectedBy; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_RejectedBy" ON tenant_default."LeaveApplications" USING btree ("RejectedBy");


--
-- Name: IX_LeaveApprovals_ApproverId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveApprovals_ApproverId" ON tenant_default."LeaveApprovals" USING btree ("ApproverId");


--
-- Name: IX_LeaveApprovals_LeaveApplicationId_ApprovalLevel; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveApprovals_LeaveApplicationId_ApprovalLevel" ON tenant_default."LeaveApprovals" USING btree ("LeaveApplicationId", "ApprovalLevel");


--
-- Name: IX_LeaveBalances_EmployeeId_LeaveTypeId_Year; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year" ON tenant_default."LeaveBalances" USING btree ("EmployeeId", "LeaveTypeId", "Year");


--
-- Name: IX_LeaveBalances_LeaveTypeId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveBalances_LeaveTypeId" ON tenant_default."LeaveBalances" USING btree ("LeaveTypeId");


--
-- Name: IX_LeaveEncashments_EmployeeId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveEncashments_EmployeeId" ON tenant_default."LeaveEncashments" USING btree ("EmployeeId");


--
-- Name: IX_LeaveTypes_TypeCode; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_LeaveTypes_TypeCode" ON tenant_default."LeaveTypes" USING btree ("TypeCode");


--
-- Name: IX_PayrollCycles_Month_Year; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PayrollCycles_Month_Year" ON tenant_default."PayrollCycles" USING btree ("Month", "Year");


--
-- Name: IX_PayrollCycles_Status; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_PayrollCycles_Status" ON tenant_default."PayrollCycles" USING btree ("Status");


--
-- Name: IX_Payslips_EmployeeId_Month_Year; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Payslips_EmployeeId_Month_Year" ON tenant_default."Payslips" USING btree ("EmployeeId", "Month", "Year");


--
-- Name: IX_Payslips_PaymentStatus; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Payslips_PaymentStatus" ON tenant_default."Payslips" USING btree ("PaymentStatus");


--
-- Name: IX_Payslips_PayrollCycleId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Payslips_PayrollCycleId" ON tenant_default."Payslips" USING btree ("PayrollCycleId");


--
-- Name: IX_Payslips_PayslipNumber; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Payslips_PayslipNumber" ON tenant_default."Payslips" USING btree ("PayslipNumber");


--
-- Name: IX_PublicHolidays_Date_Year; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_PublicHolidays_Date_Year" ON tenant_default."PublicHolidays" USING btree ("Date", "Year");


--
-- Name: IX_SalaryComponents_ComponentType; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_SalaryComponents_ComponentType" ON tenant_default."SalaryComponents" USING btree ("ComponentType");


--
-- Name: IX_SalaryComponents_EmployeeId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_SalaryComponents_EmployeeId" ON tenant_default."SalaryComponents" USING btree ("EmployeeId");


--
-- Name: IX_SalaryComponents_IsActive_EffectiveFrom; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_SalaryComponents_IsActive_EffectiveFrom" ON tenant_default."SalaryComponents" USING btree ("IsActive", "EffectiveFrom");


--
-- Name: IX_TenantCustomComplianceRules_RuleCategory; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TenantCustomComplianceRules_RuleCategory" ON tenant_default."TenantCustomComplianceRules" USING btree ("RuleCategory");


--
-- Name: IX_TenantCustomComplianceRules_SectorComplianceRuleId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TenantCustomComplianceRules_SectorComplianceRuleId" ON tenant_default."TenantCustomComplianceRules" USING btree ("SectorComplianceRuleId");


--
-- Name: IX_TenantSectorConfigurations_SectorId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TenantSectorConfigurations_SectorId" ON tenant_default."TenantSectorConfigurations" USING btree ("SectorId");


--
-- Name: IX_TimesheetAdjustments_AdjustedBy; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetAdjustments_AdjustedBy" ON tenant_default."TimesheetAdjustments" USING btree ("AdjustedBy");


--
-- Name: IX_TimesheetAdjustments_Status; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetAdjustments_Status" ON tenant_default."TimesheetAdjustments" USING btree ("Status");


--
-- Name: IX_TimesheetAdjustments_TimesheetEntryId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetAdjustments_TimesheetEntryId" ON tenant_default."TimesheetAdjustments" USING btree ("TimesheetEntryId");


--
-- Name: IX_TimesheetComments_CommentedAt; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetComments_CommentedAt" ON tenant_default."TimesheetComments" USING btree ("CommentedAt");


--
-- Name: IX_TimesheetComments_TimesheetId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetComments_TimesheetId" ON tenant_default."TimesheetComments" USING btree ("TimesheetId");


--
-- Name: IX_TimesheetEntries_AttendanceId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetEntries_AttendanceId" ON tenant_default."TimesheetEntries" USING btree ("AttendanceId");


--
-- Name: IX_TimesheetEntries_Date; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetEntries_Date" ON tenant_default."TimesheetEntries" USING btree ("Date");


--
-- Name: IX_TimesheetEntries_TimesheetId_Date; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_TimesheetEntries_TimesheetId_Date" ON tenant_default."TimesheetEntries" USING btree ("TimesheetId", "Date");


--
-- Name: IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd" ON tenant_default."Timesheets" USING btree ("EmployeeId", "PeriodStart", "PeriodEnd");


--
-- Name: IX_Timesheets_PeriodStart_PeriodEnd; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Timesheets_PeriodStart_PeriodEnd" ON tenant_default."Timesheets" USING btree ("PeriodStart", "PeriodEnd");


--
-- Name: IX_Timesheets_Status; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Timesheets_Status" ON tenant_default."Timesheets" USING btree ("Status");


--
-- Name: IX_Timesheets_TenantId; Type: INDEX; Schema: tenant_default; Owner: postgres
--

CREATE INDEX "IX_Timesheets_TenantId" ON tenant_default."Timesheets" USING btree ("TenantId");


--
-- Name: IX_AttendanceCorrections_AttendanceId_Status; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_AttendanceCorrections_AttendanceId_Status" ON tenant_siraaj."AttendanceCorrections" USING btree ("AttendanceId", "Status");


--
-- Name: IX_AttendanceCorrections_EmployeeId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_AttendanceCorrections_EmployeeId" ON tenant_siraaj."AttendanceCorrections" USING btree ("EmployeeId");


--
-- Name: IX_AttendanceCorrections_RequestedBy; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_AttendanceCorrections_RequestedBy" ON tenant_siraaj."AttendanceCorrections" USING btree ("RequestedBy");


--
-- Name: IX_AttendanceMachines_IpAddress; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_AttendanceMachines_IpAddress" ON tenant_siraaj."AttendanceMachines" USING btree ("IpAddress");


--
-- Name: IX_AttendanceMachines_SerialNumber; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_AttendanceMachines_SerialNumber" ON tenant_siraaj."AttendanceMachines" USING btree ("SerialNumber");


--
-- Name: IX_AttendanceMachines_ZKTecoDeviceId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_AttendanceMachines_ZKTecoDeviceId" ON tenant_siraaj."AttendanceMachines" USING btree ("ZKTecoDeviceId");


--
-- Name: IX_Attendances_Date; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Attendances_Date" ON tenant_siraaj."Attendances" USING btree ("Date");


--
-- Name: IX_Attendances_EmployeeId_Date; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Attendances_EmployeeId_Date" ON tenant_siraaj."Attendances" USING btree ("EmployeeId", "Date");


--
-- Name: IX_Attendances_Status; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Attendances_Status" ON tenant_siraaj."Attendances" USING btree ("Status");


--
-- Name: IX_Departments_Code; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Departments_Code" ON tenant_siraaj."Departments" USING btree ("Code");


--
-- Name: IX_Departments_DepartmentHeadId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Departments_DepartmentHeadId" ON tenant_siraaj."Departments" USING btree ("DepartmentHeadId");


--
-- Name: IX_Departments_ParentDepartmentId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Departments_ParentDepartmentId" ON tenant_siraaj."Departments" USING btree ("ParentDepartmentId");


--
-- Name: IX_EmergencyContacts_EmployeeId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_EmergencyContacts_EmployeeId" ON tenant_siraaj."EmergencyContacts" USING btree ("EmployeeId");


--
-- Name: IX_Employees_DepartmentId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Employees_DepartmentId" ON tenant_siraaj."Employees" USING btree ("DepartmentId");


--
-- Name: IX_Employees_Email; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Employees_Email" ON tenant_siraaj."Employees" USING btree ("Email");


--
-- Name: IX_Employees_EmployeeCode; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Employees_EmployeeCode" ON tenant_siraaj."Employees" USING btree ("EmployeeCode");


--
-- Name: IX_Employees_ManagerId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Employees_ManagerId" ON tenant_siraaj."Employees" USING btree ("ManagerId");


--
-- Name: IX_Employees_NationalIdCard; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Employees_NationalIdCard" ON tenant_siraaj."Employees" USING btree ("NationalIdCard");


--
-- Name: IX_Employees_PassportNumber; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Employees_PassportNumber" ON tenant_siraaj."Employees" USING btree ("PassportNumber");


--
-- Name: IX_LeaveApplications_ApplicationNumber; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_LeaveApplications_ApplicationNumber" ON tenant_siraaj."LeaveApplications" USING btree ("ApplicationNumber");


--
-- Name: IX_LeaveApplications_ApprovedBy; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_ApprovedBy" ON tenant_siraaj."LeaveApplications" USING btree ("ApprovedBy");


--
-- Name: IX_LeaveApplications_EmployeeId_StartDate_EndDate; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_EmployeeId_StartDate_EndDate" ON tenant_siraaj."LeaveApplications" USING btree ("EmployeeId", "StartDate", "EndDate");


--
-- Name: IX_LeaveApplications_LeaveTypeId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_LeaveTypeId" ON tenant_siraaj."LeaveApplications" USING btree ("LeaveTypeId");


--
-- Name: IX_LeaveApplications_RejectedBy; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveApplications_RejectedBy" ON tenant_siraaj."LeaveApplications" USING btree ("RejectedBy");


--
-- Name: IX_LeaveApprovals_ApproverId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveApprovals_ApproverId" ON tenant_siraaj."LeaveApprovals" USING btree ("ApproverId");


--
-- Name: IX_LeaveApprovals_LeaveApplicationId_ApprovalLevel; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveApprovals_LeaveApplicationId_ApprovalLevel" ON tenant_siraaj."LeaveApprovals" USING btree ("LeaveApplicationId", "ApprovalLevel");


--
-- Name: IX_LeaveBalances_EmployeeId_LeaveTypeId_Year; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year" ON tenant_siraaj."LeaveBalances" USING btree ("EmployeeId", "LeaveTypeId", "Year");


--
-- Name: IX_LeaveBalances_LeaveTypeId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveBalances_LeaveTypeId" ON tenant_siraaj."LeaveBalances" USING btree ("LeaveTypeId");


--
-- Name: IX_LeaveEncashments_EmployeeId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveEncashments_EmployeeId" ON tenant_siraaj."LeaveEncashments" USING btree ("EmployeeId");


--
-- Name: IX_LeaveTypes_TypeCode; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_LeaveTypes_TypeCode" ON tenant_siraaj."LeaveTypes" USING btree ("TypeCode");


--
-- Name: IX_PayrollCycles_Month_Year; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PayrollCycles_Month_Year" ON tenant_siraaj."PayrollCycles" USING btree ("Month", "Year");


--
-- Name: IX_PayrollCycles_Status; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_PayrollCycles_Status" ON tenant_siraaj."PayrollCycles" USING btree ("Status");


--
-- Name: IX_Payslips_EmployeeId_Month_Year; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Payslips_EmployeeId_Month_Year" ON tenant_siraaj."Payslips" USING btree ("EmployeeId", "Month", "Year");


--
-- Name: IX_Payslips_PaymentStatus; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Payslips_PaymentStatus" ON tenant_siraaj."Payslips" USING btree ("PaymentStatus");


--
-- Name: IX_Payslips_PayrollCycleId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Payslips_PayrollCycleId" ON tenant_siraaj."Payslips" USING btree ("PayrollCycleId");


--
-- Name: IX_Payslips_PayslipNumber; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Payslips_PayslipNumber" ON tenant_siraaj."Payslips" USING btree ("PayslipNumber");


--
-- Name: IX_PublicHolidays_Date_Year; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_PublicHolidays_Date_Year" ON tenant_siraaj."PublicHolidays" USING btree ("Date", "Year");


--
-- Name: IX_SalaryComponents_ComponentType; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_SalaryComponents_ComponentType" ON tenant_siraaj."SalaryComponents" USING btree ("ComponentType");


--
-- Name: IX_SalaryComponents_EmployeeId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_SalaryComponents_EmployeeId" ON tenant_siraaj."SalaryComponents" USING btree ("EmployeeId");


--
-- Name: IX_SalaryComponents_IsActive_EffectiveFrom; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_SalaryComponents_IsActive_EffectiveFrom" ON tenant_siraaj."SalaryComponents" USING btree ("IsActive", "EffectiveFrom");


--
-- Name: IX_TenantCustomComplianceRules_RuleCategory; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TenantCustomComplianceRules_RuleCategory" ON tenant_siraaj."TenantCustomComplianceRules" USING btree ("RuleCategory");


--
-- Name: IX_TenantCustomComplianceRules_SectorComplianceRuleId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TenantCustomComplianceRules_SectorComplianceRuleId" ON tenant_siraaj."TenantCustomComplianceRules" USING btree ("SectorComplianceRuleId");


--
-- Name: IX_TenantSectorConfigurations_SectorId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TenantSectorConfigurations_SectorId" ON tenant_siraaj."TenantSectorConfigurations" USING btree ("SectorId");


--
-- Name: IX_TimesheetAdjustments_AdjustedBy; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetAdjustments_AdjustedBy" ON tenant_siraaj."TimesheetAdjustments" USING btree ("AdjustedBy");


--
-- Name: IX_TimesheetAdjustments_Status; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetAdjustments_Status" ON tenant_siraaj."TimesheetAdjustments" USING btree ("Status");


--
-- Name: IX_TimesheetAdjustments_TimesheetEntryId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetAdjustments_TimesheetEntryId" ON tenant_siraaj."TimesheetAdjustments" USING btree ("TimesheetEntryId");


--
-- Name: IX_TimesheetComments_CommentedAt; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetComments_CommentedAt" ON tenant_siraaj."TimesheetComments" USING btree ("CommentedAt");


--
-- Name: IX_TimesheetComments_TimesheetId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetComments_TimesheetId" ON tenant_siraaj."TimesheetComments" USING btree ("TimesheetId");


--
-- Name: IX_TimesheetEntries_AttendanceId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetEntries_AttendanceId" ON tenant_siraaj."TimesheetEntries" USING btree ("AttendanceId");


--
-- Name: IX_TimesheetEntries_Date; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetEntries_Date" ON tenant_siraaj."TimesheetEntries" USING btree ("Date");


--
-- Name: IX_TimesheetEntries_TimesheetId_Date; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_TimesheetEntries_TimesheetId_Date" ON tenant_siraaj."TimesheetEntries" USING btree ("TimesheetId", "Date");


--
-- Name: IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd" ON tenant_siraaj."Timesheets" USING btree ("EmployeeId", "PeriodStart", "PeriodEnd");


--
-- Name: IX_Timesheets_PeriodStart_PeriodEnd; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Timesheets_PeriodStart_PeriodEnd" ON tenant_siraaj."Timesheets" USING btree ("PeriodStart", "PeriodEnd");


--
-- Name: IX_Timesheets_Status; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Timesheets_Status" ON tenant_siraaj."Timesheets" USING btree ("Status");


--
-- Name: IX_Timesheets_TenantId; Type: INDEX; Schema: tenant_siraaj; Owner: postgres
--

CREATE INDEX "IX_Timesheets_TenantId" ON tenant_siraaj."Timesheets" USING btree ("TenantId");


--
-- Name: jobparameter jobparameter_jobid_fkey; Type: FK CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobparameter
    ADD CONSTRAINT jobparameter_jobid_fkey FOREIGN KEY (jobid) REFERENCES hangfire.job(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: state state_jobid_fkey; Type: FK CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.state
    ADD CONSTRAINT state_jobid_fkey FOREIGN KEY (jobid) REFERENCES hangfire.job(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: IndustrySectors FK_IndustrySectors_IndustrySectors_ParentSectorId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."IndustrySectors"
    ADD CONSTRAINT "FK_IndustrySectors_IndustrySectors_ParentSectorId" FOREIGN KEY ("ParentSectorId") REFERENCES master."IndustrySectors"("Id") ON DELETE RESTRICT;


--
-- Name: SectorComplianceRules FK_SectorComplianceRules_IndustrySectors_SectorId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SectorComplianceRules"
    ADD CONSTRAINT "FK_SectorComplianceRules_IndustrySectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES master."IndustrySectors"("Id") ON DELETE CASCADE;


--
-- Name: Tenants FK_Tenants_IndustrySectors_SectorId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Tenants"
    ADD CONSTRAINT "FK_Tenants_IndustrySectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES master."IndustrySectors"("Id") ON DELETE RESTRICT;


--
-- Name: AttendanceCorrections FK_AttendanceCorrections_Attendances_AttendanceId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."AttendanceCorrections"
    ADD CONSTRAINT "FK_AttendanceCorrections_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_default."Attendances"("Id") ON DELETE CASCADE;


--
-- Name: AttendanceCorrections FK_AttendanceCorrections_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."AttendanceCorrections"
    ADD CONSTRAINT "FK_AttendanceCorrections_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: AttendanceMachines FK_AttendanceMachines_Locations; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."AttendanceMachines"
    ADD CONSTRAINT "FK_AttendanceMachines_Locations" FOREIGN KEY ("LocationId") REFERENCES tenant_default."Locations"("Id") ON DELETE SET NULL;


--
-- Name: Attendances FK_Attendances_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Attendances"
    ADD CONSTRAINT "FK_Attendances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: Departments FK_Departments_Departments_ParentDepartmentId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Departments"
    ADD CONSTRAINT "FK_Departments_Departments_ParentDepartmentId" FOREIGN KEY ("ParentDepartmentId") REFERENCES tenant_default."Departments"("Id") ON DELETE RESTRICT;


--
-- Name: Departments FK_Departments_Employees_DepartmentHeadId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Departments"
    ADD CONSTRAINT "FK_Departments_Employees_DepartmentHeadId" FOREIGN KEY ("DepartmentHeadId") REFERENCES tenant_default."Employees"("Id") ON DELETE SET NULL;


--
-- Name: DeviceSyncLogs FK_DeviceSyncLogs_AttendanceMachines; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."DeviceSyncLogs"
    ADD CONSTRAINT "FK_DeviceSyncLogs_AttendanceMachines" FOREIGN KEY ("DeviceId") REFERENCES tenant_default."AttendanceMachines"("Id") ON DELETE CASCADE;


--
-- Name: EmergencyContacts FK_EmergencyContacts_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."EmergencyContacts"
    ADD CONSTRAINT "FK_EmergencyContacts_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: EmployeeDeviceAccesses FK_EmployeeDeviceAccesses_AttendanceMachines; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."EmployeeDeviceAccesses"
    ADD CONSTRAINT "FK_EmployeeDeviceAccesses_AttendanceMachines" FOREIGN KEY ("DeviceId") REFERENCES tenant_default."AttendanceMachines"("Id") ON DELETE CASCADE;


--
-- Name: EmployeeDeviceAccesses FK_EmployeeDeviceAccesses_Employees; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."EmployeeDeviceAccesses"
    ADD CONSTRAINT "FK_EmployeeDeviceAccesses_Employees" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: Employees FK_Employees_Departments_DepartmentId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Employees"
    ADD CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES tenant_default."Departments"("Id") ON DELETE RESTRICT;


--
-- Name: Employees FK_Employees_Employees_ManagerId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Employees"
    ADD CONSTRAINT "FK_Employees_Employees_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES tenant_default."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: Employees FK_Employees_Locations; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Employees"
    ADD CONSTRAINT "FK_Employees_Locations" FOREIGN KEY ("PrimaryLocationId") REFERENCES tenant_default."Locations"("Id") ON DELETE SET NULL;


--
-- Name: LeaveApplications FK_LeaveApplications_Employees_ApprovedBy; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_Employees_ApprovedBy" FOREIGN KEY ("ApprovedBy") REFERENCES tenant_default."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApplications FK_LeaveApplications_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: LeaveApplications FK_LeaveApplications_Employees_RejectedBy; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_Employees_RejectedBy" FOREIGN KEY ("RejectedBy") REFERENCES tenant_default."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApplications FK_LeaveApplications_LeaveTypes_LeaveTypeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES tenant_default."LeaveTypes"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApprovals FK_LeaveApprovals_Employees_ApproverId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApprovals"
    ADD CONSTRAINT "FK_LeaveApprovals_Employees_ApproverId" FOREIGN KEY ("ApproverId") REFERENCES tenant_default."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApprovals FK_LeaveApprovals_LeaveApplications_LeaveApplicationId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveApprovals"
    ADD CONSTRAINT "FK_LeaveApprovals_LeaveApplications_LeaveApplicationId" FOREIGN KEY ("LeaveApplicationId") REFERENCES tenant_default."LeaveApplications"("Id") ON DELETE CASCADE;


--
-- Name: LeaveBalances FK_LeaveBalances_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveBalances"
    ADD CONSTRAINT "FK_LeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: LeaveBalances FK_LeaveBalances_LeaveTypes_LeaveTypeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveBalances"
    ADD CONSTRAINT "FK_LeaveBalances_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES tenant_default."LeaveTypes"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveEncashments FK_LeaveEncashments_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."LeaveEncashments"
    ADD CONSTRAINT "FK_LeaveEncashments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: Payslips FK_Payslips_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Payslips"
    ADD CONSTRAINT "FK_Payslips_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: Payslips FK_Payslips_PayrollCycles_PayrollCycleId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Payslips"
    ADD CONSTRAINT "FK_Payslips_PayrollCycles_PayrollCycleId" FOREIGN KEY ("PayrollCycleId") REFERENCES tenant_default."PayrollCycles"("Id") ON DELETE CASCADE;


--
-- Name: SalaryComponents FK_SalaryComponents_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."SalaryComponents"
    ADD CONSTRAINT "FK_SalaryComponents_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: TimesheetAdjustments FK_TimesheetAdjustments_TimesheetEntries_TimesheetEntryId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetAdjustments"
    ADD CONSTRAINT "FK_TimesheetAdjustments_TimesheetEntries_TimesheetEntryId" FOREIGN KEY ("TimesheetEntryId") REFERENCES tenant_default."TimesheetEntries"("Id") ON DELETE CASCADE;


--
-- Name: TimesheetComments FK_TimesheetComments_Timesheets_TimesheetId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetComments"
    ADD CONSTRAINT "FK_TimesheetComments_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES tenant_default."Timesheets"("Id") ON DELETE CASCADE;


--
-- Name: TimesheetEntries FK_TimesheetEntries_Attendances_AttendanceId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetEntries"
    ADD CONSTRAINT "FK_TimesheetEntries_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_default."Attendances"("Id") ON DELETE SET NULL;


--
-- Name: TimesheetEntries FK_TimesheetEntries_Timesheets_TimesheetId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."TimesheetEntries"
    ADD CONSTRAINT "FK_TimesheetEntries_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES tenant_default."Timesheets"("Id") ON DELETE CASCADE;


--
-- Name: Timesheets FK_Timesheets_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_default; Owner: postgres
--

ALTER TABLE ONLY tenant_default."Timesheets"
    ADD CONSTRAINT "FK_Timesheets_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_default."Employees"("Id") ON DELETE CASCADE;


--
-- Name: AttendanceCorrections FK_AttendanceCorrections_Attendances_AttendanceId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."AttendanceCorrections"
    ADD CONSTRAINT "FK_AttendanceCorrections_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_siraaj."Attendances"("Id") ON DELETE CASCADE;


--
-- Name: AttendanceCorrections FK_AttendanceCorrections_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."AttendanceCorrections"
    ADD CONSTRAINT "FK_AttendanceCorrections_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: AttendanceMachines FK_AttendanceMachines_Locations; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."AttendanceMachines"
    ADD CONSTRAINT "FK_AttendanceMachines_Locations" FOREIGN KEY ("LocationId") REFERENCES tenant_siraaj."Locations"("Id") ON DELETE SET NULL;


--
-- Name: Attendances FK_Attendances_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Attendances"
    ADD CONSTRAINT "FK_Attendances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: Departments FK_Departments_Departments_ParentDepartmentId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Departments"
    ADD CONSTRAINT "FK_Departments_Departments_ParentDepartmentId" FOREIGN KEY ("ParentDepartmentId") REFERENCES tenant_siraaj."Departments"("Id") ON DELETE RESTRICT;


--
-- Name: Departments FK_Departments_Employees_DepartmentHeadId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Departments"
    ADD CONSTRAINT "FK_Departments_Employees_DepartmentHeadId" FOREIGN KEY ("DepartmentHeadId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE SET NULL;


--
-- Name: DeviceSyncLogs FK_DeviceSyncLogs_AttendanceMachines; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."DeviceSyncLogs"
    ADD CONSTRAINT "FK_DeviceSyncLogs_AttendanceMachines" FOREIGN KEY ("DeviceId") REFERENCES tenant_siraaj."AttendanceMachines"("Id") ON DELETE CASCADE;


--
-- Name: EmergencyContacts FK_EmergencyContacts_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."EmergencyContacts"
    ADD CONSTRAINT "FK_EmergencyContacts_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: EmployeeDeviceAccesses FK_EmployeeDeviceAccesses_AttendanceMachines; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."EmployeeDeviceAccesses"
    ADD CONSTRAINT "FK_EmployeeDeviceAccesses_AttendanceMachines" FOREIGN KEY ("DeviceId") REFERENCES tenant_siraaj."AttendanceMachines"("Id") ON DELETE CASCADE;


--
-- Name: EmployeeDeviceAccesses FK_EmployeeDeviceAccesses_Employees; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."EmployeeDeviceAccesses"
    ADD CONSTRAINT "FK_EmployeeDeviceAccesses_Employees" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: Employees FK_Employees_Departments_DepartmentId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Employees"
    ADD CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES tenant_siraaj."Departments"("Id") ON DELETE RESTRICT;


--
-- Name: Employees FK_Employees_Employees_ManagerId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Employees"
    ADD CONSTRAINT "FK_Employees_Employees_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: Employees FK_Employees_Locations; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Employees"
    ADD CONSTRAINT "FK_Employees_Locations" FOREIGN KEY ("PrimaryLocationId") REFERENCES tenant_siraaj."Locations"("Id") ON DELETE SET NULL;


--
-- Name: LeaveApplications FK_LeaveApplications_Employees_ApprovedBy; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_Employees_ApprovedBy" FOREIGN KEY ("ApprovedBy") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApplications FK_LeaveApplications_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: LeaveApplications FK_LeaveApplications_Employees_RejectedBy; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_Employees_RejectedBy" FOREIGN KEY ("RejectedBy") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApplications FK_LeaveApplications_LeaveTypes_LeaveTypeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApplications"
    ADD CONSTRAINT "FK_LeaveApplications_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES tenant_siraaj."LeaveTypes"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApprovals FK_LeaveApprovals_Employees_ApproverId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApprovals"
    ADD CONSTRAINT "FK_LeaveApprovals_Employees_ApproverId" FOREIGN KEY ("ApproverId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveApprovals FK_LeaveApprovals_LeaveApplications_LeaveApplicationId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveApprovals"
    ADD CONSTRAINT "FK_LeaveApprovals_LeaveApplications_LeaveApplicationId" FOREIGN KEY ("LeaveApplicationId") REFERENCES tenant_siraaj."LeaveApplications"("Id") ON DELETE CASCADE;


--
-- Name: LeaveBalances FK_LeaveBalances_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveBalances"
    ADD CONSTRAINT "FK_LeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: LeaveBalances FK_LeaveBalances_LeaveTypes_LeaveTypeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveBalances"
    ADD CONSTRAINT "FK_LeaveBalances_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES tenant_siraaj."LeaveTypes"("Id") ON DELETE RESTRICT;


--
-- Name: LeaveEncashments FK_LeaveEncashments_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."LeaveEncashments"
    ADD CONSTRAINT "FK_LeaveEncashments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: Payslips FK_Payslips_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Payslips"
    ADD CONSTRAINT "FK_Payslips_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE RESTRICT;


--
-- Name: Payslips FK_Payslips_PayrollCycles_PayrollCycleId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Payslips"
    ADD CONSTRAINT "FK_Payslips_PayrollCycles_PayrollCycleId" FOREIGN KEY ("PayrollCycleId") REFERENCES tenant_siraaj."PayrollCycles"("Id") ON DELETE CASCADE;


--
-- Name: SalaryComponents FK_SalaryComponents_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."SalaryComponents"
    ADD CONSTRAINT "FK_SalaryComponents_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- Name: TimesheetAdjustments FK_TimesheetAdjustments_TimesheetEntries_TimesheetEntryId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetAdjustments"
    ADD CONSTRAINT "FK_TimesheetAdjustments_TimesheetEntries_TimesheetEntryId" FOREIGN KEY ("TimesheetEntryId") REFERENCES tenant_siraaj."TimesheetEntries"("Id") ON DELETE CASCADE;


--
-- Name: TimesheetComments FK_TimesheetComments_Timesheets_TimesheetId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetComments"
    ADD CONSTRAINT "FK_TimesheetComments_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES tenant_siraaj."Timesheets"("Id") ON DELETE CASCADE;


--
-- Name: TimesheetEntries FK_TimesheetEntries_Attendances_AttendanceId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetEntries"
    ADD CONSTRAINT "FK_TimesheetEntries_Attendances_AttendanceId" FOREIGN KEY ("AttendanceId") REFERENCES tenant_siraaj."Attendances"("Id") ON DELETE SET NULL;


--
-- Name: TimesheetEntries FK_TimesheetEntries_Timesheets_TimesheetId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."TimesheetEntries"
    ADD CONSTRAINT "FK_TimesheetEntries_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES tenant_siraaj."Timesheets"("Id") ON DELETE CASCADE;


--
-- Name: Timesheets FK_Timesheets_Employees_EmployeeId; Type: FK CONSTRAINT; Schema: tenant_siraaj; Owner: postgres
--

ALTER TABLE ONLY tenant_siraaj."Timesheets"
    ADD CONSTRAINT "FK_Timesheets_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES tenant_siraaj."Employees"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict xAKxIzHH3nty7ONLa5fWACewt7Pp8I2bsY0YMemjfxuuZBGeEAKMnTEl06DKYf0

