--
-- PostgreSQL database dump
--

\restrict h4rsubtdJPb8FHPBWH7Ty7ni7n5gy8rLlsFwwHklg4NZEVdLaJE1Ogvx1BH9oJx

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

DROP DATABASE IF EXISTS hrms_master;
--
-- Name: hrms_master; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE hrms_master WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'C.UTF-8';


ALTER DATABASE hrms_master OWNER TO postgres;

\unrestrict h4rsubtdJPb8FHPBWH7Ty7ni7n5gy8rLlsFwwHklg4NZEVdLaJE1Ogvx1BH9oJx
\connect hrms_master
\restrict h4rsubtdJPb8FHPBWH7Ty7ni7n5gy8rLlsFwwHklg4NZEVdLaJE1Ogvx1BH9oJx

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
-- Name: ActivationResendLogs; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."ActivationResendLogs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "RequestedFromIp" character varying(45),
    "RequestedByEmail" character varying(255),
    "TokenGenerated" character varying(32),
    "TokenExpiry" timestamp with time zone NOT NULL,
    "Success" boolean DEFAULT true NOT NULL,
    "FailureReason" character varying(2000),
    "UserAgent" character varying(500),
    "DeviceInfo" character varying(200),
    "Geolocation" character varying(500),
    "EmailDelivered" boolean DEFAULT false NOT NULL,
    "EmailSendError" character varying(2000),
    "ResendCountLastHour" integer DEFAULT 0 NOT NULL,
    "WasRateLimited" boolean DEFAULT false NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."ActivationResendLogs" OWNER TO postgres;

--
-- Name: TABLE "ActivationResendLogs"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON TABLE master."ActivationResendLogs" IS 'Fortune 500 activation resend audit log for multi-tenant SaaS. Enables rate limiting (max 3 per hour), security monitoring, and GDPR compliance. IMMUTABLE logs with cascade delete on tenant deletion.';


--
-- Name: COLUMN "ActivationResendLogs"."TenantId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."TenantId" IS 'Tenant ID (enables per-tenant rate limiting)';


--
-- Name: COLUMN "ActivationResendLogs"."RequestedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."RequestedAt" IS 'When resend was requested (UTC) - used for sliding window rate limits';


--
-- Name: COLUMN "ActivationResendLogs"."RequestedFromIp"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."RequestedFromIp" IS 'IP address of requester (IPv4 or IPv6) for security monitoring';


--
-- Name: COLUMN "ActivationResendLogs"."RequestedByEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."RequestedByEmail" IS 'Email address used in request (must match tenant email)';


--
-- Name: COLUMN "ActivationResendLogs"."TokenGenerated"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."TokenGenerated" IS 'New token generated (truncated for security - never full token!)';


--
-- Name: COLUMN "ActivationResendLogs"."TokenExpiry"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."TokenExpiry" IS 'Token expiration timestamp (UTC) - typically 24 hours from RequestedAt';


--
-- Name: COLUMN "ActivationResendLogs"."Success"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."Success" IS 'Was resend successful? (false if rate limited or email send failed)';


--
-- Name: COLUMN "ActivationResendLogs"."FailureReason"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."FailureReason" IS 'Failure reason if Success=false (rate limit, email error, validation failure, etc.)';


--
-- Name: COLUMN "ActivationResendLogs"."UserAgent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."UserAgent" IS 'User agent string for fraud detection';


--
-- Name: COLUMN "ActivationResendLogs"."DeviceInfo"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."DeviceInfo" IS 'Parsed device info (Mobile/Desktop, browser, OS)';


--
-- Name: COLUMN "ActivationResendLogs"."Geolocation"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."Geolocation" IS 'City, country for fraud detection';


--
-- Name: COLUMN "ActivationResendLogs"."EmailDelivered"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."EmailDelivered" IS 'Was activation email delivered successfully?';


--
-- Name: COLUMN "ActivationResendLogs"."EmailSendError"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."EmailSendError" IS 'SMTP error or bounce reason if delivery failed';


--
-- Name: COLUMN "ActivationResendLogs"."ResendCountLastHour"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."ResendCountLastHour" IS 'Number of resend attempts in last hour (real-time rate limit tracking)';


--
-- Name: COLUMN "ActivationResendLogs"."WasRateLimited"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."ActivationResendLogs"."WasRateLimited" IS 'Was this request blocked by rate limiting?';


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
    "TwoFactorSecret" character varying(500),
    "BackupCodes" jsonb,
    "LockoutEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "AccessFailedCount" integer NOT NULL,
    "LastPasswordChangeDate" timestamp with time zone,
    "PasswordExpiresAt" timestamp with time zone,
    "MustChangePassword" boolean NOT NULL,
    "AllowedIPAddresses" text,
    "SessionTimeoutMinutes" integer NOT NULL,
    "Permissions" text,
    "LastLoginIPAddress" text,
    "LastFailedLoginAttempt" timestamp with time zone,
    "IsInitialSetupAccount" boolean NOT NULL,
    "PasswordHistory" text,
    "CreatedBySuperAdminId" uuid,
    "LastModifiedBySuperAdminId" uuid,
    "AllowedLoginHours" text,
    "StatusNotes" text,
    "PasswordResetToken" text,
    "PasswordResetTokenExpiry" timestamp with time zone,
    "ActivationTokenExpiry" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."AdminUsers" OWNER TO postgres;

--
-- Name: COLUMN "AdminUsers"."BackupCodes"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AdminUsers"."BackupCodes" IS 'JSON array of SHA256-hashed backup codes for MFA recovery';


--
-- Name: AuditLogs; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."AuditLogs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid,
    "TenantName" character varying(200),
    "UserId" uuid,
    "UserEmail" character varying(100),
    "UserFullName" character varying(200),
    "UserRole" character varying(50),
    "SessionId" character varying(100),
    "ActionType" integer NOT NULL,
    "Category" integer NOT NULL,
    "Severity" integer NOT NULL,
    "EntityType" character varying(100),
    "EntityId" uuid,
    "Success" boolean NOT NULL,
    "ErrorMessage" character varying(2000),
    "OldValues" jsonb,
    "NewValues" jsonb,
    "ChangedFields" character varying(1000),
    "Reason" character varying(1000),
    "ApprovalReference" character varying(100),
    "IpAddress" character varying(45),
    "Geolocation" character varying(500),
    "UserAgent" character varying(500),
    "DeviceInfo" character varying(500),
    "NetworkInfo" character varying(500),
    "PerformedAt" timestamp with time zone NOT NULL,
    "DurationMs" integer,
    "BusinessDate" timestamp with time zone,
    "PolicyReference" character varying(200),
    "DocumentationLink" character varying(500),
    "HttpMethod" character varying(10),
    "RequestPath" character varying(500),
    "QueryString" character varying(2000),
    "ResponseCode" integer,
    "CorrelationId" character varying(100),
    "ParentActionId" uuid,
    "AdditionalMetadata" jsonb,
    "Checksum" character varying(64),
    "IsArchived" boolean DEFAULT false NOT NULL,
    "ArchivedAt" timestamp with time zone,
    "IsUnderLegalHold" boolean NOT NULL,
    "LegalHoldId" uuid
);


ALTER TABLE master."AuditLogs" OWNER TO postgres;

--
-- Name: TABLE "AuditLogs"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON TABLE master."AuditLogs" IS 'Production-grade audit log with 10+ year retention. IMMUTABLE - no UPDATE/DELETE allowed (enforced by DB triggers). Partitioned by PerformedAt (monthly). Meets Mauritius compliance requirements.';


--
-- Name: COLUMN "AuditLogs"."TenantId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."TenantId" IS 'Tenant ID (null for SuperAdmin platform-level actions)';


--
-- Name: COLUMN "AuditLogs"."TenantName"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."TenantName" IS 'Denormalized tenant name for reporting';


--
-- Name: COLUMN "AuditLogs"."UserId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."UserId" IS 'User ID who performed the action';


--
-- Name: COLUMN "AuditLogs"."UserEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."UserEmail" IS 'User email address';


--
-- Name: COLUMN "AuditLogs"."UserFullName"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."UserFullName" IS 'User full name for audit trail readability';


--
-- Name: COLUMN "AuditLogs"."UserRole"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."UserRole" IS 'User role at time of action (SuperAdmin, TenantAdmin, HR, Manager, Employee)';


--
-- Name: COLUMN "AuditLogs"."SessionId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."SessionId" IS 'Session ID for tracking related actions';


--
-- Name: COLUMN "AuditLogs"."ActionType"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ActionType" IS 'Standardized action type (enum stored as integer)';


--
-- Name: COLUMN "AuditLogs"."Category"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."Category" IS 'High-level category for filtering (enum stored as integer)';


--
-- Name: COLUMN "AuditLogs"."Severity"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."Severity" IS 'Severity level for alerting (enum stored as integer)';


--
-- Name: COLUMN "AuditLogs"."EntityType"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."EntityType" IS 'Entity type affected (Employee, LeaveRequest, Payroll, etc.)';


--
-- Name: COLUMN "AuditLogs"."EntityId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."EntityId" IS 'Entity ID affected (if applicable)';


--
-- Name: COLUMN "AuditLogs"."Success"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."Success" IS 'Whether the action succeeded';


--
-- Name: COLUMN "AuditLogs"."ErrorMessage"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ErrorMessage" IS 'Error message if action failed';


--
-- Name: COLUMN "AuditLogs"."OldValues"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."OldValues" IS 'Old values before change (JSON format)';


--
-- Name: COLUMN "AuditLogs"."NewValues"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."NewValues" IS 'New values after change (JSON format)';


--
-- Name: COLUMN "AuditLogs"."ChangedFields"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ChangedFields" IS 'Comma-separated list of changed field names';


--
-- Name: COLUMN "AuditLogs"."Reason"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."Reason" IS 'User-provided reason for the action';


--
-- Name: COLUMN "AuditLogs"."ApprovalReference"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ApprovalReference" IS 'Approval reference if action required approval';


--
-- Name: COLUMN "AuditLogs"."IpAddress"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."IpAddress" IS 'IP address of the user (IPv4 or IPv6)';


--
-- Name: COLUMN "AuditLogs"."Geolocation"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."Geolocation" IS 'Geolocation information (city, country, coordinates)';


--
-- Name: COLUMN "AuditLogs"."UserAgent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."UserAgent" IS 'User agent string (browser/device information)';


--
-- Name: COLUMN "AuditLogs"."DeviceInfo"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."DeviceInfo" IS 'Parsed device information (mobile, desktop, tablet, OS, browser)';


--
-- Name: COLUMN "AuditLogs"."NetworkInfo"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."NetworkInfo" IS 'Network information (ISP, organization)';


--
-- Name: COLUMN "AuditLogs"."PerformedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."PerformedAt" IS 'Timestamp when action was performed (UTC)';


--
-- Name: COLUMN "AuditLogs"."DurationMs"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."DurationMs" IS 'Action duration in milliseconds (for performance tracking)';


--
-- Name: COLUMN "AuditLogs"."BusinessDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."BusinessDate" IS 'Business date for payroll/leave actions';


--
-- Name: COLUMN "AuditLogs"."PolicyReference"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."PolicyReference" IS 'Policy reference that triggered this action';


--
-- Name: COLUMN "AuditLogs"."DocumentationLink"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."DocumentationLink" IS 'Link to related documentation or policy';


--
-- Name: COLUMN "AuditLogs"."HttpMethod"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."HttpMethod" IS 'HTTP method (GET, POST, PUT, DELETE)';


--
-- Name: COLUMN "AuditLogs"."RequestPath"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."RequestPath" IS 'Request path/endpoint';


--
-- Name: COLUMN "AuditLogs"."QueryString"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."QueryString" IS 'Query string parameters';


--
-- Name: COLUMN "AuditLogs"."ResponseCode"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ResponseCode" IS 'HTTP response status code';


--
-- Name: COLUMN "AuditLogs"."CorrelationId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."CorrelationId" IS 'Correlation ID for distributed tracing';


--
-- Name: COLUMN "AuditLogs"."ParentActionId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ParentActionId" IS 'Parent action ID for multi-step operations';


--
-- Name: COLUMN "AuditLogs"."AdditionalMetadata"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."AdditionalMetadata" IS 'Additional metadata in JSON format (flexible for future extensions)';


--
-- Name: COLUMN "AuditLogs"."Checksum"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."Checksum" IS 'SHA256 checksum for tamper detection';


--
-- Name: COLUMN "AuditLogs"."IsArchived"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."IsArchived" IS 'Flag indicating if entry has been archived to cold storage';


--
-- Name: COLUMN "AuditLogs"."ArchivedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."AuditLogs"."ArchivedAt" IS 'Archival date (when moved to cold storage)';


--
-- Name: DetectedAnomalies; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."DetectedAnomalies" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "AnomalyType" integer NOT NULL,
    "RiskLevel" integer NOT NULL,
    "Status" integer NOT NULL,
    "RiskScore" integer NOT NULL,
    "UserId" uuid,
    "UserEmail" character varying(100),
    "IpAddress" character varying(45),
    "Location" character varying(500),
    "DetectedAt" timestamp with time zone NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Evidence" jsonb NOT NULL,
    "RelatedAuditLogIds" text,
    "DetectionRule" character varying(200) NOT NULL,
    "ModelVersion" text,
    "InvestigatedBy" uuid,
    "InvestigatedAt" timestamp with time zone,
    "InvestigationNotes" text,
    "Resolution" text,
    "ResolvedAt" timestamp with time zone,
    "NotificationSent" boolean NOT NULL,
    "NotificationSentAt" timestamp with time zone,
    "NotificationRecipients" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone
);


ALTER TABLE master."DetectedAnomalies" OWNER TO postgres;

--
-- Name: Districts; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."Districts" (
    "Id" integer NOT NULL,
    "DistrictCode" character varying(10) NOT NULL,
    "DistrictName" character varying(100) NOT NULL,
    "DistrictNameFrench" character varying(100),
    "Region" character varying(50) NOT NULL,
    "AreaSqKm" numeric,
    "Population" integer,
    "DisplayOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE master."Districts" OWNER TO postgres;

--
-- Name: Districts_Id_seq; Type: SEQUENCE; Schema: master; Owner: postgres
--

ALTER TABLE master."Districts" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME master."Districts_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: FeatureFlags; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."FeatureFlags" (
    "Id" uuid NOT NULL,
    "TenantId" uuid,
    "Module" character varying(100) NOT NULL,
    "IsEnabled" boolean DEFAULT false NOT NULL,
    "RolloutPercentage" integer DEFAULT 0 NOT NULL,
    "Description" character varying(500),
    "Tags" character varying(200),
    "MinimumTier" character varying(50),
    "IsEmergencyDisabled" boolean DEFAULT false NOT NULL,
    "EmergencyDisabledReason" character varying(1000),
    "EmergencyDisabledAt" timestamp with time zone,
    "EmergencyDisabledBy" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."FeatureFlags" OWNER TO postgres;

--
-- Name: TABLE "FeatureFlags"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON TABLE master."FeatureFlags" IS 'Fortune 500 feature flag system for per-tenant control. Enables canary deployment, gradual rollout, emergency rollback, A/B testing. NULL TenantId = global default, NON-NULL = tenant override.';


--
-- Name: COLUMN "FeatureFlags"."TenantId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."TenantId" IS 'Tenant ID (NULL = global default, NON-NULL = tenant override)';


--
-- Name: COLUMN "FeatureFlags"."Module"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."Module" IS 'Module name (auth, dashboard, employees, payroll, etc.)';


--
-- Name: COLUMN "FeatureFlags"."IsEnabled"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."IsEnabled" IS 'Whether feature is enabled (default FALSE for safety)';


--
-- Name: COLUMN "FeatureFlags"."RolloutPercentage"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."RolloutPercentage" IS 'Rollout percentage 0-100 (0=disabled, 100=fully enabled)';


--
-- Name: COLUMN "FeatureFlags"."Description"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."Description" IS 'Feature description for documentation';


--
-- Name: COLUMN "FeatureFlags"."Tags"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."Tags" IS 'Tags for categorization (comma-separated)';


--
-- Name: COLUMN "FeatureFlags"."MinimumTier"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."MinimumTier" IS 'Minimum tier required (NULL = all tiers)';


--
-- Name: COLUMN "FeatureFlags"."IsEmergencyDisabled"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."IsEmergencyDisabled" IS 'Emergency rollback flag for quick disable';


--
-- Name: COLUMN "FeatureFlags"."EmergencyDisabledReason"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."EmergencyDisabledReason" IS 'Reason for emergency rollback (audit trail)';


--
-- Name: COLUMN "FeatureFlags"."EmergencyDisabledAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."EmergencyDisabledAt" IS 'Emergency rollback timestamp';


--
-- Name: COLUMN "FeatureFlags"."EmergencyDisabledBy"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."FeatureFlags"."EmergencyDisabledBy" IS 'SuperAdmin who triggered emergency rollback';


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
-- Name: LegalHolds; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."LegalHolds" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "CaseNumber" character varying(100) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Reason" text,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone,
    "Status" integer NOT NULL,
    "UserIds" jsonb,
    "EntityTypes" jsonb,
    "SearchKeywords" text,
    "RequestedBy" uuid NOT NULL,
    "LegalRepresentative" character varying(200),
    "LegalRepresentativeEmail" text,
    "LawFirm" text,
    "CourtOrder" character varying(500),
    "ReleasedBy" uuid,
    "ReleasedAt" timestamp with time zone,
    "ReleaseNotes" text,
    "AffectedAuditLogCount" integer NOT NULL,
    "AffectedEntityCount" integer NOT NULL,
    "NotifiedUsers" text,
    "NotificationSentAt" timestamp with time zone,
    "ComplianceFrameworks" text,
    "RetentionPeriodDays" integer,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" uuid,
    "AdditionalMetadata" text
);


ALTER TABLE master."LegalHolds" OWNER TO postgres;

--
-- Name: PostalCodes; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."PostalCodes" (
    "Id" integer NOT NULL,
    "Code" character varying(10) NOT NULL,
    "VillageName" character varying(200) NOT NULL,
    "DistrictName" character varying(100) NOT NULL,
    "Region" character varying(50) NOT NULL,
    "VillageId" integer NOT NULL,
    "DistrictId" integer NOT NULL,
    "LocalityType" character varying(50),
    "IsPrimary" boolean NOT NULL,
    "Notes" character varying(500),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE master."PostalCodes" OWNER TO postgres;

--
-- Name: PostalCodes_Id_seq; Type: SEQUENCE; Schema: master; Owner: postgres
--

ALTER TABLE master."PostalCodes" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME master."PostalCodes_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: RefreshTokens; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."RefreshTokens" (
    "Id" uuid NOT NULL,
    "AdminUserId" uuid,
    "TenantId" uuid,
    "EmployeeId" uuid,
    "Token" character varying(500) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedByIp" character varying(45) NOT NULL,
    "RevokedAt" timestamp with time zone,
    "RevokedByIp" character varying(45),
    "ReplacedByToken" character varying(500),
    "ReasonRevoked" character varying(200),
    "SessionTimeoutMinutes" integer NOT NULL,
    "LastActivityAt" timestamp with time zone NOT NULL
);


ALTER TABLE master."RefreshTokens" OWNER TO postgres;

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
-- Name: SecurityAlerts; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."SecurityAlerts" (
    "Id" uuid NOT NULL,
    "AlertType" integer NOT NULL,
    "Severity" integer NOT NULL,
    "Category" integer NOT NULL,
    "Status" integer NOT NULL,
    "Title" character varying(500) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "RecommendedActions" character varying(2000),
    "RiskScore" integer NOT NULL,
    "AuditLogId" uuid,
    "AuditActionType" integer,
    "TenantId" uuid,
    "TenantName" character varying(200),
    "UserId" uuid,
    "UserEmail" character varying(100),
    "UserFullName" character varying(200),
    "UserRole" character varying(50),
    "IpAddress" character varying(45),
    "Geolocation" character varying(500),
    "UserAgent" character varying(500),
    "DeviceInfo" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "DetectedAt" timestamp with time zone NOT NULL,
    "AcknowledgedAt" timestamp with time zone,
    "ResolvedAt" timestamp with time zone,
    "AcknowledgedBy" uuid,
    "AcknowledgedByEmail" character varying(100),
    "ResolvedBy" uuid,
    "ResolvedByEmail" character varying(100),
    "ResolutionNotes" character varying(2000),
    "AssignedTo" uuid,
    "AssignedToEmail" character varying(100),
    "EmailSent" boolean DEFAULT false NOT NULL,
    "EmailSentAt" timestamp with time zone,
    "EmailRecipients" character varying(1000),
    "SmsSent" boolean DEFAULT false NOT NULL,
    "SmsSentAt" timestamp with time zone,
    "SmsRecipients" character varying(500),
    "SlackSent" boolean DEFAULT false NOT NULL,
    "SlackSentAt" timestamp with time zone,
    "SlackChannels" character varying(500),
    "SiemSent" boolean DEFAULT false NOT NULL,
    "SiemSentAt" timestamp with time zone,
    "SiemSystem" character varying(100),
    "DetectionRule" jsonb,
    "BaselineMetrics" jsonb,
    "CurrentMetrics" jsonb,
    "DeviationPercentage" numeric(5,2),
    "CorrelationId" character varying(100),
    "AdditionalMetadata" jsonb,
    "Tags" character varying(500),
    "ComplianceFrameworks" character varying(200),
    "RequiresEscalation" boolean DEFAULT false NOT NULL,
    "EscalatedTo" character varying(200),
    "EscalatedAt" timestamp with time zone,
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedAt" timestamp with time zone
);


ALTER TABLE master."SecurityAlerts" OWNER TO postgres;

--
-- Name: TABLE "SecurityAlerts"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON TABLE master."SecurityAlerts" IS 'Production-grade security alert system for real-time threat detection. Supports SOX, GDPR, ISO 27001, PCI-DSS compliance. Integrates with Email, SMS, Slack, and SIEM systems.';


--
-- Name: COLUMN "SecurityAlerts"."AlertType"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AlertType" IS 'Type of security alert (enum stored as integer)';


--
-- Name: COLUMN "SecurityAlerts"."Severity"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Severity" IS 'Alert severity level (CRITICAL, EMERGENCY, HIGH, MEDIUM, LOW)';


--
-- Name: COLUMN "SecurityAlerts"."Category"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Category" IS 'Alert category for classification';


--
-- Name: COLUMN "SecurityAlerts"."Status"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Status" IS 'Alert status (NEW, ACKNOWLEDGED, IN_PROGRESS, RESOLVED, etc.)';


--
-- Name: COLUMN "SecurityAlerts"."Title"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Title" IS 'Alert title/summary';


--
-- Name: COLUMN "SecurityAlerts"."Description"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Description" IS 'Detailed alert description';


--
-- Name: COLUMN "SecurityAlerts"."RecommendedActions"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."RecommendedActions" IS 'Recommended actions to address the alert';


--
-- Name: COLUMN "SecurityAlerts"."RiskScore"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."RiskScore" IS 'Risk score 0-100 calculated by anomaly detection';


--
-- Name: COLUMN "SecurityAlerts"."AuditLogId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AuditLogId" IS 'Related audit log entry ID';


--
-- Name: COLUMN "SecurityAlerts"."AuditActionType"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AuditActionType" IS 'Related audit log action type';


--
-- Name: COLUMN "SecurityAlerts"."TenantId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."TenantId" IS 'Tenant ID (null for platform-level alerts)';


--
-- Name: COLUMN "SecurityAlerts"."TenantName"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."TenantName" IS 'Tenant name for reporting';


--
-- Name: COLUMN "SecurityAlerts"."UserId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."UserId" IS 'User ID who triggered the alert';


--
-- Name: COLUMN "SecurityAlerts"."UserEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."UserEmail" IS 'User email address';


--
-- Name: COLUMN "SecurityAlerts"."UserFullName"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."UserFullName" IS 'User full name';


--
-- Name: COLUMN "SecurityAlerts"."UserRole"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."UserRole" IS 'User role at time of alert';


--
-- Name: COLUMN "SecurityAlerts"."IpAddress"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."IpAddress" IS 'IP address associated with alert';


--
-- Name: COLUMN "SecurityAlerts"."Geolocation"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Geolocation" IS 'Geolocation information';


--
-- Name: COLUMN "SecurityAlerts"."UserAgent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."UserAgent" IS 'User agent string';


--
-- Name: COLUMN "SecurityAlerts"."DeviceInfo"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."DeviceInfo" IS 'Device information';


--
-- Name: COLUMN "SecurityAlerts"."CreatedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."CreatedAt" IS 'When alert was created (UTC)';


--
-- Name: COLUMN "SecurityAlerts"."DetectedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."DetectedAt" IS 'When alert was first detected (UTC)';


--
-- Name: COLUMN "SecurityAlerts"."AcknowledgedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AcknowledgedAt" IS 'When alert was acknowledged (UTC)';


--
-- Name: COLUMN "SecurityAlerts"."ResolvedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."ResolvedAt" IS 'When alert was resolved (UTC)';


--
-- Name: COLUMN "SecurityAlerts"."AcknowledgedBy"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AcknowledgedBy" IS 'User ID who acknowledged the alert';


--
-- Name: COLUMN "SecurityAlerts"."AcknowledgedByEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AcknowledgedByEmail" IS 'Email of user who acknowledged';


--
-- Name: COLUMN "SecurityAlerts"."ResolvedBy"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."ResolvedBy" IS 'User ID who resolved the alert';


--
-- Name: COLUMN "SecurityAlerts"."ResolvedByEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."ResolvedByEmail" IS 'Email of user who resolved';


--
-- Name: COLUMN "SecurityAlerts"."ResolutionNotes"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."ResolutionNotes" IS 'Resolution notes';


--
-- Name: COLUMN "SecurityAlerts"."AssignedTo"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AssignedTo" IS 'Assigned to user ID';


--
-- Name: COLUMN "SecurityAlerts"."AssignedToEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AssignedToEmail" IS 'Assigned to user email';


--
-- Name: COLUMN "SecurityAlerts"."EmailSent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."EmailSent" IS 'Whether email notification was sent';


--
-- Name: COLUMN "SecurityAlerts"."EmailSentAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."EmailSentAt" IS 'Email sent timestamp';


--
-- Name: COLUMN "SecurityAlerts"."EmailRecipients"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."EmailRecipients" IS 'Email recipients (comma-separated)';


--
-- Name: COLUMN "SecurityAlerts"."SmsSent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SmsSent" IS 'Whether SMS notification was sent';


--
-- Name: COLUMN "SecurityAlerts"."SmsSentAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SmsSentAt" IS 'SMS sent timestamp';


--
-- Name: COLUMN "SecurityAlerts"."SmsRecipients"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SmsRecipients" IS 'SMS recipients (comma-separated)';


--
-- Name: COLUMN "SecurityAlerts"."SlackSent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SlackSent" IS 'Whether Slack notification was sent';


--
-- Name: COLUMN "SecurityAlerts"."SlackSentAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SlackSentAt" IS 'Slack sent timestamp';


--
-- Name: COLUMN "SecurityAlerts"."SlackChannels"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SlackChannels" IS 'Slack channels notified';


--
-- Name: COLUMN "SecurityAlerts"."SiemSent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SiemSent" IS 'Whether SIEM notification was sent';


--
-- Name: COLUMN "SecurityAlerts"."SiemSentAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SiemSentAt" IS 'SIEM sent timestamp';


--
-- Name: COLUMN "SecurityAlerts"."SiemSystem"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."SiemSystem" IS 'SIEM system name';


--
-- Name: COLUMN "SecurityAlerts"."DetectionRule"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."DetectionRule" IS 'Detection rule that triggered alert (JSON)';


--
-- Name: COLUMN "SecurityAlerts"."BaselineMetrics"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."BaselineMetrics" IS 'Baseline metrics (JSON)';


--
-- Name: COLUMN "SecurityAlerts"."CurrentMetrics"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."CurrentMetrics" IS 'Current metrics that triggered alert (JSON)';


--
-- Name: COLUMN "SecurityAlerts"."DeviationPercentage"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."DeviationPercentage" IS 'Deviation percentage from baseline';


--
-- Name: COLUMN "SecurityAlerts"."CorrelationId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."CorrelationId" IS 'Correlation ID for distributed tracing';


--
-- Name: COLUMN "SecurityAlerts"."AdditionalMetadata"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."AdditionalMetadata" IS 'Additional metadata (JSON)';


--
-- Name: COLUMN "SecurityAlerts"."Tags"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."Tags" IS 'Tags for categorization';


--
-- Name: COLUMN "SecurityAlerts"."ComplianceFrameworks"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."ComplianceFrameworks" IS 'Related compliance frameworks';


--
-- Name: COLUMN "SecurityAlerts"."RequiresEscalation"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."RequiresEscalation" IS 'Whether alert requires escalation';


--
-- Name: COLUMN "SecurityAlerts"."EscalatedTo"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."EscalatedTo" IS 'Escalated to (email or system)';


--
-- Name: COLUMN "SecurityAlerts"."EscalatedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."EscalatedAt" IS 'Escalation timestamp';


--
-- Name: COLUMN "SecurityAlerts"."IsDeleted"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."IsDeleted" IS 'Soft delete flag';


--
-- Name: COLUMN "SecurityAlerts"."DeletedAt"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SecurityAlerts"."DeletedAt" IS 'When alert was soft-deleted';


--
-- Name: SubscriptionNotificationLogs; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."SubscriptionNotificationLogs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "NotificationType" integer NOT NULL,
    "SentDate" timestamp with time zone NOT NULL,
    "RecipientEmail" character varying(100) NOT NULL,
    "EmailSubject" character varying(500) NOT NULL,
    "DeliverySuccess" boolean DEFAULT false NOT NULL,
    "DeliveryError" character varying(2000),
    "SubscriptionEndDateAtNotification" timestamp with time zone NOT NULL,
    "DaysUntilExpiryAtNotification" integer NOT NULL,
    "SubscriptionPaymentId" uuid,
    "RequiresFollowUp" boolean DEFAULT false NOT NULL,
    "FollowUpCompletedDate" timestamp with time zone,
    "FollowUpNotes" character varying(2000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."SubscriptionNotificationLogs" OWNER TO postgres;

--
-- Name: TABLE "SubscriptionNotificationLogs"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON TABLE master."SubscriptionNotificationLogs" IS 'Production-grade subscription notification audit log. Prevents duplicate email sends (Stripe, Chargebee pattern). IMMUTABLE - logs are historical records for compliance. Indexed on TenantId, NotificationType, SentDate for fast duplicate checks.';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."TenantId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."TenantId" IS 'Foreign key to Tenant';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."NotificationType"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."NotificationType" IS 'Type of notification sent (30d, 15d, 7d, expiry, etc.)';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."SentDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."SentDate" IS 'Date/time when notification was sent (UTC)';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."RecipientEmail"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."RecipientEmail" IS 'Recipient email address (audit trail)';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."EmailSubject"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."EmailSubject" IS 'Email subject line';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."DeliverySuccess"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."DeliverySuccess" IS 'Whether email was delivered successfully';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."DeliveryError"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."DeliveryError" IS 'Error message if delivery failed';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."SubscriptionEndDateAtNotification"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."SubscriptionEndDateAtNotification" IS 'Subscription end date at time of notification (audit trail)';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."DaysUntilExpiryAtNotification"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."DaysUntilExpiryAtNotification" IS 'Days until expiry at time of notification (audit trail)';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."SubscriptionPaymentId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."SubscriptionPaymentId" IS 'Related subscription payment ID (if applicable)';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."RequiresFollowUp"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."RequiresFollowUp" IS 'Does this notification require follow-up action?';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."FollowUpCompletedDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."FollowUpCompletedDate" IS 'When follow-up action was completed';


--
-- Name: COLUMN "SubscriptionNotificationLogs"."FollowUpNotes"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionNotificationLogs"."FollowUpNotes" IS 'Notes about follow-up action taken';


--
-- Name: SubscriptionPayments; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."SubscriptionPayments" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "PeriodStartDate" timestamp with time zone NOT NULL,
    "PeriodEndDate" timestamp with time zone NOT NULL,
    "AmountMUR" numeric(18,2) NOT NULL,
    "SubtotalMUR" numeric NOT NULL,
    "TaxRate" numeric NOT NULL,
    "TaxAmountMUR" numeric NOT NULL,
    "TotalMUR" numeric NOT NULL,
    "IsTaxExempt" boolean NOT NULL,
    "Status" integer NOT NULL,
    "PaidDate" timestamp with time zone,
    "ProcessedBy" character varying(100),
    "PaymentReference" character varying(200),
    "PaymentMethod" character varying(100),
    "DueDate" timestamp with time zone NOT NULL,
    "Notes" character varying(1000),
    "EmployeeTier" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."SubscriptionPayments" OWNER TO postgres;

--
-- Name: TABLE "SubscriptionPayments"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON TABLE master."SubscriptionPayments" IS 'Production-grade yearly subscription payment history. IMMUTABLE - payments are historical records. Manual payment processing by SuperAdmin with full audit trail. Indexed on TenantId, PaymentDate, Status for fast queries.';


--
-- Name: COLUMN "SubscriptionPayments"."TenantId"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."TenantId" IS 'Foreign key to Tenant';


--
-- Name: COLUMN "SubscriptionPayments"."PeriodStartDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."PeriodStartDate" IS 'Subscription period start date';


--
-- Name: COLUMN "SubscriptionPayments"."PeriodEndDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."PeriodEndDate" IS 'Subscription period end date (usually +365 days)';


--
-- Name: COLUMN "SubscriptionPayments"."AmountMUR"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."AmountMUR" IS 'Amount in Mauritian Rupees (MUR) - decimal(18,2)';


--
-- Name: COLUMN "SubscriptionPayments"."Status"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."Status" IS 'Payment status (Pending, Paid, Overdue, Failed, etc.)';


--
-- Name: COLUMN "SubscriptionPayments"."PaidDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."PaidDate" IS 'Date when payment was marked as paid';


--
-- Name: COLUMN "SubscriptionPayments"."ProcessedBy"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."ProcessedBy" IS 'SuperAdmin who confirmed the payment (audit trail)';


--
-- Name: COLUMN "SubscriptionPayments"."PaymentReference"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."PaymentReference" IS 'Invoice, receipt, or bank transaction reference';


--
-- Name: COLUMN "SubscriptionPayments"."PaymentMethod"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."PaymentMethod" IS 'Payment method (Bank Transfer, Cash, Cheque, etc.)';


--
-- Name: COLUMN "SubscriptionPayments"."DueDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."DueDate" IS 'Payment due date for grace period calculations';


--
-- Name: COLUMN "SubscriptionPayments"."Notes"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."Notes" IS 'Additional notes about the payment';


--
-- Name: COLUMN "SubscriptionPayments"."EmployeeTier"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."SubscriptionPayments"."EmployeeTier" IS 'Employee tier at time of payment (audit trail)';


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
    "EmployeeTier" integer NOT NULL,
    "YearlyPriceMUR" numeric(18,2) NOT NULL,
    "MaxUsers" integer NOT NULL,
    "MaxStorageGB" integer NOT NULL,
    "ApiCallsPerMonth" integer NOT NULL,
    "SuspensionReason" text,
    "SuspensionDate" timestamp with time zone,
    "SoftDeleteDate" timestamp with time zone,
    "DeletionReason" text,
    "GracePeriodDays" integer NOT NULL,
    "SubscriptionStartDate" timestamp with time zone NOT NULL,
    "SubscriptionEndDate" timestamp with time zone,
    "TrialEndDate" timestamp with time zone,
    "LastNotificationSent" timestamp with time zone,
    "LastNotificationType" integer,
    "GracePeriodStartDate" timestamp with time zone,
    "AdminUserName" character varying(100) NOT NULL,
    "AdminEmail" character varying(100) NOT NULL,
    "AdminFirstName" text NOT NULL,
    "AdminLastName" text NOT NULL,
    "ActivationToken" text,
    "ActivationTokenExpiry" timestamp with time zone,
    "ActivatedAt" timestamp with time zone,
    "ActivatedBy" text,
    "IsGovernmentEntity" boolean NOT NULL,
    "CurrentUserCount" integer NOT NULL,
    "CurrentStorageGB" integer NOT NULL,
    "SectorId" integer,
    "SectorSelectedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" text
);


ALTER TABLE master."Tenants" OWNER TO postgres;

--
-- Name: COLUMN "Tenants"."YearlyPriceMUR"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."Tenants"."YearlyPriceMUR" IS 'Yearly subscription price in Mauritian Rupees (MUR)';


--
-- Name: COLUMN "Tenants"."LastNotificationSent"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."Tenants"."LastNotificationSent" IS 'Last subscription notification sent (timestamp)';


--
-- Name: COLUMN "Tenants"."LastNotificationType"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."Tenants"."LastNotificationType" IS 'Type of last notification sent (prevents duplicates)';


--
-- Name: COLUMN "Tenants"."GracePeriodStartDate"; Type: COMMENT; Schema: master; Owner: postgres
--

COMMENT ON COLUMN master."Tenants"."GracePeriodStartDate" IS 'Grace period start date (when subscription expired)';


--
-- Name: Villages; Type: TABLE; Schema: master; Owner: postgres
--

CREATE TABLE master."Villages" (
    "Id" integer NOT NULL,
    "VillageCode" character varying(10) NOT NULL,
    "VillageName" character varying(200) NOT NULL,
    "VillageNameFrench" character varying(200),
    "PostalCode" character varying(10) NOT NULL,
    "DistrictId" integer NOT NULL,
    "LocalityType" character varying(50),
    "Latitude" numeric,
    "Longitude" numeric,
    "DisplayOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE master."Villages" OWNER TO postgres;

--
-- Name: Villages_Id_seq; Type: SEQUENCE; Schema: master; Owner: postgres
--

ALTER TABLE master."Villages" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME master."Villages_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE public."__EFMigrationsHistory" OWNER TO postgres;

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
4	stats:failed:2025-11-19	2	2025-12-19 12:09:40.014152+00
1	stats:succeeded:2025-11-19-12	2	2025-11-20 12:10:24.841566+00
2	stats:succeeded:2025-11-19	2	2025-12-19 12:10:23.841566+00
7	stats:failed:2025-11-19-12	2	2025-11-20 12:09:41.014152+00
3	stats:succeeded	2	\N
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
1	recurring-job:daily-mv-refresh	Queue	default	\N	0
2	recurring-job:daily-mv-refresh	Cron	0 3 * * *	\N	0
3	recurring-job:daily-mv-refresh	TimeZoneId	UTC	\N	0
4	recurring-job:daily-mv-refresh	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.DatabaseMaintenanceJobs, HRMS.Infrastructure","m":"RefreshMaterializedViewsAsync"}	\N	0
5	recurring-job:daily-mv-refresh	CreatedAt	1763553687855	\N	0
6	recurring-job:daily-mv-refresh	NextExecution	1763607600000	\N	0
7	recurring-job:daily-mv-refresh	V	2	\N	0
8	recurring-job:daily-token-cleanup	Queue	default	\N	0
9	recurring-job:daily-token-cleanup	Cron	0 4 * * *	\N	0
10	recurring-job:daily-token-cleanup	TimeZoneId	UTC	\N	0
11	recurring-job:daily-token-cleanup	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.DatabaseMaintenanceJobs, HRMS.Infrastructure","m":"CleanupExpiredTokensAsync"}	\N	0
12	recurring-job:daily-token-cleanup	CreatedAt	1763553687990	\N	0
13	recurring-job:daily-token-cleanup	NextExecution	1763611200000	\N	0
14	recurring-job:daily-token-cleanup	V	2	\N	0
15	recurring-job:weekly-vacuum-maintenance	Queue	default	\N	0
16	recurring-job:weekly-vacuum-maintenance	Cron	0 4 * * 0	\N	0
17	recurring-job:weekly-vacuum-maintenance	TimeZoneId	UTC	\N	0
18	recurring-job:weekly-vacuum-maintenance	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.DatabaseMaintenanceJobs, HRMS.Infrastructure","m":"WeeklyVacuumMaintenanceAsync"}	\N	0
19	recurring-job:weekly-vacuum-maintenance	CreatedAt	1763553688001	\N	0
20	recurring-job:weekly-vacuum-maintenance	NextExecution	1763870400000	\N	0
21	recurring-job:weekly-vacuum-maintenance	V	2	\N	0
22	recurring-job:monthly-partition-maintenance	Queue	default	\N	0
23	recurring-job:monthly-partition-maintenance	Cron	0 2 1 * *	\N	0
24	recurring-job:monthly-partition-maintenance	TimeZoneId	UTC	\N	0
25	recurring-job:monthly-partition-maintenance	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.DatabaseMaintenanceJobs, HRMS.Infrastructure","m":"MonthlyPartitionMaintenanceAsync"}	\N	0
26	recurring-job:monthly-partition-maintenance	CreatedAt	1763553688010	\N	0
27	recurring-job:monthly-partition-maintenance	NextExecution	1764554400000	\N	0
28	recurring-job:monthly-partition-maintenance	V	2	\N	0
29	recurring-job:daily-health-check	Queue	default	\N	0
30	recurring-job:daily-health-check	Cron	0 6 * * *	\N	0
31	recurring-job:daily-health-check	TimeZoneId	UTC	\N	0
32	recurring-job:daily-health-check	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.DatabaseMaintenanceJobs, HRMS.Infrastructure","m":"DailyHealthCheckAsync"}	\N	0
33	recurring-job:daily-health-check	CreatedAt	1763553688020	\N	0
34	recurring-job:daily-health-check	NextExecution	1763618400000	\N	0
35	recurring-job:daily-health-check	V	2	\N	0
36	recurring-job:document-expiry-alerts	Queue	default	\N	0
37	recurring-job:document-expiry-alerts	Cron	0 9 * * *	\N	0
38	recurring-job:document-expiry-alerts	TimeZoneId	Mauritius Standard Time	\N	0
39	recurring-job:document-expiry-alerts	Job	{"t":"HRMS.BackgroundJobs.Jobs.DocumentExpiryAlertJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
40	recurring-job:document-expiry-alerts	CreatedAt	1763553688028	\N	0
41	recurring-job:document-expiry-alerts	NextExecution	1763614800000	\N	0
42	recurring-job:document-expiry-alerts	V	2	\N	0
43	recurring-job:absent-marking	Queue	default	\N	0
44	recurring-job:absent-marking	Cron	0 23 * * *	\N	0
45	recurring-job:absent-marking	TimeZoneId	Mauritius Standard Time	\N	0
46	recurring-job:absent-marking	Job	{"t":"HRMS.BackgroundJobs.Jobs.AbsentMarkingJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
47	recurring-job:absent-marking	CreatedAt	1763553688039	\N	0
48	recurring-job:absent-marking	NextExecution	1763578800000	\N	0
49	recurring-job:absent-marking	V	2	\N	0
50	recurring-job:leave-accrual	Queue	default	\N	0
51	recurring-job:leave-accrual	Cron	0 1 1 * *	\N	0
52	recurring-job:leave-accrual	TimeZoneId	Mauritius Standard Time	\N	0
53	recurring-job:leave-accrual	Job	{"t":"HRMS.BackgroundJobs.Jobs.LeaveAccrualJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
54	recurring-job:leave-accrual	CreatedAt	1763553688049	\N	0
55	recurring-job:leave-accrual	NextExecution	1764536400000	\N	0
56	recurring-job:leave-accrual	V	2	\N	0
57	recurring-job:delete-expired-drafts	Queue	default	\N	0
58	recurring-job:delete-expired-drafts	Cron	0 2 * * *	\N	0
59	recurring-job:delete-expired-drafts	TimeZoneId	Mauritius Standard Time	\N	0
60	recurring-job:delete-expired-drafts	Job	{"t":"HRMS.BackgroundJobs.Jobs.DeleteExpiredDraftsJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
61	recurring-job:delete-expired-drafts	CreatedAt	1763553688059	\N	0
62	recurring-job:delete-expired-drafts	NextExecution	1763589600000	\N	0
63	recurring-job:delete-expired-drafts	V	2	\N	0
64	recurring-job:audit-log-archival	Queue	default	\N	0
65	recurring-job:audit-log-archival	Cron	0 3 1 * *	\N	0
66	recurring-job:audit-log-archival	TimeZoneId	Mauritius Standard Time	\N	0
67	recurring-job:audit-log-archival	Job	{"t":"HRMS.BackgroundJobs.Jobs.AuditLogArchivalJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
68	recurring-job:audit-log-archival	CreatedAt	1763553688067	\N	0
69	recurring-job:audit-log-archival	NextExecution	1764543600000	\N	0
70	recurring-job:audit-log-archival	V	2	\N	0
71	recurring-job:audit-log-checksum-verification	Queue	default	\N	0
72	recurring-job:audit-log-checksum-verification	Cron	0 4 * * 0	\N	0
73	recurring-job:audit-log-checksum-verification	TimeZoneId	Mauritius Standard Time	\N	0
74	recurring-job:audit-log-checksum-verification	Job	{"t":"HRMS.BackgroundJobs.Jobs.AuditLogChecksumVerificationJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
75	recurring-job:audit-log-checksum-verification	CreatedAt	1763553688075	\N	0
76	recurring-job:audit-log-checksum-verification	NextExecution	1763856000000	\N	0
77	recurring-job:audit-log-checksum-verification	V	2	\N	0
78	recurring-job:subscription-notifications	Queue	default	\N	0
79	recurring-job:subscription-notifications	Cron	0 6 * * *	\N	0
80	recurring-job:subscription-notifications	TimeZoneId	Mauritius Standard Time	\N	0
81	recurring-job:subscription-notifications	Job	{"t":"HRMS.BackgroundJobs.Jobs.SubscriptionNotificationJob, HRMS.BackgroundJobs","m":"Execute"}	\N	0
82	recurring-job:subscription-notifications	CreatedAt	1763553688097	\N	0
83	recurring-job:subscription-notifications	NextExecution	1763604000000	\N	0
84	recurring-job:subscription-notifications	V	2	\N	0
85	recurring-job:abandoned-tenant-cleanup	Queue	default	\N	0
86	recurring-job:abandoned-tenant-cleanup	Cron	0 5 * * *	\N	0
87	recurring-job:abandoned-tenant-cleanup	TimeZoneId	Mauritius Standard Time	\N	0
88	recurring-job:abandoned-tenant-cleanup	Job	{"t":"HRMS.BackgroundJobs.Jobs.AbandonedTenantCleanupJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
89	recurring-job:abandoned-tenant-cleanup	CreatedAt	1763553688109	\N	0
90	recurring-job:abandoned-tenant-cleanup	NextExecution	1763600400000	\N	0
91	recurring-job:abandoned-tenant-cleanup	V	2	\N	0
92	recurring-job:activation-reminders	Queue	default	\N	0
93	recurring-job:activation-reminders	Cron	0 8 * * *	\N	0
94	recurring-job:activation-reminders	TimeZoneId	Mauritius Standard Time	\N	0
95	recurring-job:activation-reminders	Job	{"t":"HRMS.BackgroundJobs.Jobs.ActivationReminderJob, HRMS.BackgroundJobs","m":"ExecuteAsync"}	\N	0
96	recurring-job:activation-reminders	CreatedAt	1763553688139	\N	0
97	recurring-job:activation-reminders	NextExecution	1763611200000	\N	0
98	recurring-job:activation-reminders	V	2	\N	0
99	recurring-job:monitoring-performance-snapshot	Queue	default	\N	0
100	recurring-job:monitoring-performance-snapshot	Cron	*/5 * * * *	\N	0
101	recurring-job:monitoring-performance-snapshot	TimeZoneId	UTC	\N	0
102	recurring-job:monitoring-performance-snapshot	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure","m":"CapturePerformanceSnapshotAsync"}	\N	0
103	recurring-job:monitoring-performance-snapshot	CreatedAt	1763553688149	\N	0
105	recurring-job:monitoring-performance-snapshot	V	2	\N	0
106	recurring-job:monitoring-dashboard-refresh	Queue	default	\N	0
107	recurring-job:monitoring-dashboard-refresh	Cron	*/5 * * * *	\N	0
108	recurring-job:monitoring-dashboard-refresh	TimeZoneId	UTC	\N	0
109	recurring-job:monitoring-dashboard-refresh	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure","m":"RefreshDashboardSummaryAsync"}	\N	0
110	recurring-job:monitoring-dashboard-refresh	CreatedAt	1763553688160	\N	0
112	recurring-job:monitoring-dashboard-refresh	V	2	\N	0
113	recurring-job:monitoring-alert-checks	Queue	default	\N	0
114	recurring-job:monitoring-alert-checks	Cron	*/5 * * * *	\N	0
115	recurring-job:monitoring-alert-checks	TimeZoneId	UTC	\N	0
116	recurring-job:monitoring-alert-checks	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure","m":"CheckAlertThresholdsAsync"}	\N	0
117	recurring-job:monitoring-alert-checks	CreatedAt	1763553688174	\N	0
119	recurring-job:monitoring-alert-checks	V	2	\N	0
120	recurring-job:monitoring-data-cleanup	Queue	default	\N	0
121	recurring-job:monitoring-data-cleanup	Cron	0 2 * * *	\N	0
122	recurring-job:monitoring-data-cleanup	TimeZoneId	Mauritius Standard Time	\N	0
123	recurring-job:monitoring-data-cleanup	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure","m":"CleanupOldMonitoringDataAsync"}	\N	0
124	recurring-job:monitoring-data-cleanup	CreatedAt	1763553688195	\N	0
125	recurring-job:monitoring-data-cleanup	NextExecution	1763589600000	\N	0
126	recurring-job:monitoring-data-cleanup	V	2	\N	0
127	recurring-job:monitoring-slow-query-analysis	Queue	default	\N	0
128	recurring-job:monitoring-slow-query-analysis	Cron	0 3 * * *	\N	0
129	recurring-job:monitoring-slow-query-analysis	TimeZoneId	Mauritius Standard Time	\N	0
130	recurring-job:monitoring-slow-query-analysis	Job	{"t":"HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure","m":"AnalyzeSlowQueriesAsync"}	\N	0
131	recurring-job:monitoring-slow-query-analysis	CreatedAt	1763553688206	\N	0
132	recurring-job:monitoring-slow-query-analysis	NextExecution	1763593200000	\N	0
133	recurring-job:monitoring-slow-query-analysis	V	2	\N	0
136	recurring-job:monitoring-dashboard-refresh	LastExecution	1763554214951	\N	0
138	recurring-job:monitoring-alert-checks	LastExecution	1763554214951	\N	0
134	recurring-job:monitoring-performance-snapshot	LastExecution	1763554214951	\N	0
104	recurring-job:monitoring-performance-snapshot	NextExecution	1763554500000	\N	0
135	recurring-job:monitoring-performance-snapshot	LastJobId	4	\N	0
111	recurring-job:monitoring-dashboard-refresh	NextExecution	1763554500000	\N	0
137	recurring-job:monitoring-dashboard-refresh	LastJobId	5	\N	0
118	recurring-job:monitoring-alert-checks	NextExecution	1763554500000	\N	0
139	recurring-job:monitoring-alert-checks	LastJobId	6	\N	0
\.


--
-- Data for Name: job; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.job (id, stateid, statename, invocationdata, arguments, createdat, expireat, updatecount) FROM stdin;
5	58	Scheduled	{"Type": "HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure", "Method": "RefreshDashboardSummaryAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-19 12:10:14.985972+00	\N	0
4	60	Scheduled	{"Type": "HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure", "Method": "CapturePerformanceSnapshotAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-19 12:10:14.962375+00	\N	0
2	33	Failed	{"Type": "HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure", "Method": "RefreshDashboardSummaryAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-19 12:05:14.827945+00	\N	0
3	11	Succeeded	{"Type": "HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure", "Method": "CheckAlertThresholdsAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-19 12:05:14.842932+00	2025-11-20 12:05:22.599409+00	0
1	32	Failed	{"Type": "HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure", "Method": "CapturePerformanceSnapshotAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-19 12:05:14.795829+00	\N	0
6	44	Succeeded	{"Type": "HRMS.Infrastructure.BackgroundJobs.MonitoringJobs, HRMS.Infrastructure", "Method": "CheckAlertThresholdsAsync", "Arguments": "[]", "ParameterTypes": "[]"}	[]	2025-11-19 12:10:15.02617+00	2025-11-20 12:10:24.841566+00	0
\.


--
-- Data for Name: jobparameter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.jobparameter (id, jobid, name, value, updatecount) FROM stdin;
1	1	RecurringJobId	"monitoring-performance-snapshot"	0
2	1	Time	1763553914	0
3	1	CurrentCulture	""	0
4	2	RecurringJobId	"monitoring-dashboard-refresh"	0
5	2	Time	1763553914	0
6	2	CurrentCulture	""	0
7	3	RecurringJobId	"monitoring-alert-checks"	0
8	3	Time	1763553914	0
9	3	CurrentCulture	""	0
11	2	RetryCount	3	0
10	1	RetryCount	3	0
12	4	RecurringJobId	"monitoring-performance-snapshot"	0
13	4	Time	1763554214	0
14	4	CurrentCulture	""	0
15	5	RecurringJobId	"monitoring-dashboard-refresh"	0
16	5	Time	1763554214	0
17	5	CurrentCulture	""	0
18	6	RecurringJobId	"monitoring-alert-checks"	0
19	6	Time	1763554214	0
20	6	CurrentCulture	""	0
22	5	RetryCount	3	0
21	4	RetryCount	3	0
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
hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6	{"Queues": ["default"], "StartedAt": "2025-11-19T12:01:29.5336459Z", "WorkerCount": 5}	2025-11-19 12:12:59.903732+00	0
\.


--
-- Data for Name: set; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.set (id, key, score, value, expireat, updatecount) FROM stdin;
1	recurring-jobs	1763607600	daily-mv-refresh	\N	0
2	recurring-jobs	1763611200	daily-token-cleanup	\N	0
3	recurring-jobs	1763870400	weekly-vacuum-maintenance	\N	0
4	recurring-jobs	1764554400	monthly-partition-maintenance	\N	0
5	recurring-jobs	1763618400	daily-health-check	\N	0
6	recurring-jobs	1763614800	document-expiry-alerts	\N	0
7	recurring-jobs	1763578800	absent-marking	\N	0
8	recurring-jobs	1764536400	leave-accrual	\N	0
9	recurring-jobs	1763589600	delete-expired-drafts	\N	0
10	recurring-jobs	1764543600	audit-log-archival	\N	0
11	recurring-jobs	1763856000	audit-log-checksum-verification	\N	0
12	recurring-jobs	1763604000	subscription-notifications	\N	0
13	recurring-jobs	1763600400	abandoned-tenant-cleanup	\N	0
14	recurring-jobs	1763611200	activation-reminders	\N	0
18	recurring-jobs	1763589600	monitoring-data-cleanup	\N	0
19	recurring-jobs	1763593200	monitoring-slow-query-analysis	\N	0
15	recurring-jobs	1763554500	monitoring-performance-snapshot	\N	0
16	recurring-jobs	1763554500	monitoring-dashboard-refresh	\N	0
17	recurring-jobs	1763554500	monitoring-alert-checks	\N	0
46	retries	0	5	\N	0
47	schedule	1763554462	5	\N	0
48	retries	0	4	\N	0
49	schedule	1763554465	4	\N	0
\.


--
-- Data for Name: state; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

COPY hangfire.state (id, jobid, name, reason, createdat, data, updatecount) FROM stdin;
1	1	Enqueued	Triggered by recurring job scheduler	2025-11-19 12:05:14.813995+00	{"Queue": "default", "EnqueuedAt": "1763553914808"}	0
2	2	Enqueued	Triggered by recurring job scheduler	2025-11-19 12:05:14.830991+00	{"Queue": "default", "EnqueuedAt": "1763553914830"}	0
3	3	Enqueued	Triggered by recurring job scheduler	2025-11-19 12:05:14.845117+00	{"Queue": "default", "EnqueuedAt": "1763553914845"}	0
4	1	Processing	\N	2025-11-19 12:05:14.846822+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "7d20c94a-3ece-40bc-b397-008052c7101f", "StartedAt": "1763553914828"}	0
5	2	Processing	\N	2025-11-19 12:05:14.84726+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "fa8aff46-ed50-4416-807a-0eebd8827322", "StartedAt": "1763553914837"}	0
6	3	Processing	\N	2025-11-19 12:05:14.862656+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "6ea81699-df6d-4b52-a1d2-4f658295be12", "StartedAt": "1763553914856"}	0
7	1	Failed	An exception occurred during performance of the job.	2025-11-19 12:05:21.27984+00	{"FailedAt": "1763553921249", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlConnection.OpenAsync(CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenDbConnectionAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
8	1	Scheduled	Retry attempt 1 of 3: Cannot access a disposed object.\nObject name: 'Np…	2025-11-19 12:05:21.289457+00	{"EnqueueAt": "1763553951271", "ScheduledAt": "1763553921272"}	0
9	2	Failed	An exception occurred during performance of the job.	2025-11-19 12:05:22.115978+00	{"FailedAt": "1763553922104", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
10	2	Scheduled	Retry attempt 1 of 3: 3F000: schema "monitoring" does not exist\n\nPOSITI…	2025-11-19 12:05:22.118845+00	{"EnqueueAt": "1763553952110", "ScheduledAt": "1763553922110"}	0
11	3	Succeeded	\N	2025-11-19 12:05:22.600861+00	{"Latency": "26", "SucceededAt": "1763553922591", "PerformanceDuration": "7721"}	0
12	1	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:05:59.782711+00	{"Queue": "default", "EnqueuedAt": "1763553959777"}	0
13	2	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:05:59.790429+00	{"Queue": "default", "EnqueuedAt": "1763553959786"}	0
14	1	Processing	\N	2025-11-19 12:05:59.791386+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "aa1e1184-8c89-4796-8c8e-a31a7bac9efc", "StartedAt": "1763553959787"}	0
15	2	Processing	\N	2025-11-19 12:05:59.800073+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "adce7aa9-2e95-4bbe-8300-e9cf06be8589", "StartedAt": "1763553959795"}	0
16	2	Failed	An exception occurred during performance of the job.	2025-11-19 12:06:06.439041+00	{"FailedAt": "1763553966420", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
17	2	Scheduled	Retry attempt 2 of 3: 3F000: schema "monitoring" does not exist\n\nPOSITI…	2025-11-19 12:06:06.441202+00	{"EnqueueAt": "1763554026432", "ScheduledAt": "1763553966432"}	0
45	4	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:11:00.093272+00	{"Queue": "default", "EnqueuedAt": "1763554260088"}	0
46	5	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:11:00.104741+00	{"Queue": "default", "EnqueuedAt": "1763554260099"}	0
18	1	Failed	An exception occurred during performance of the job.	2025-11-19 12:06:08.937205+00	{"FailedAt": "1763553968901", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlConnection.OpenAsync(CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenDbConnectionAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
19	1	Scheduled	Retry attempt 2 of 3: Cannot access a disposed object.\nObject name: 'Np…	2025-11-19 12:06:08.947249+00	{"EnqueueAt": "1763554028931", "ScheduledAt": "1763553968931"}	0
20	2	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:07:14.863507+00	{"Queue": "default", "EnqueuedAt": "1763554034860"}	0
21	2	Processing	\N	2025-11-19 12:07:14.873318+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "7d20c94a-3ece-40bc-b397-008052c7101f", "StartedAt": "1763554034869"}	0
22	1	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:07:14.877062+00	{"Queue": "default", "EnqueuedAt": "1763554034868"}	0
23	1	Processing	\N	2025-11-19 12:07:14.903084+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "adce7aa9-2e95-4bbe-8300-e9cf06be8589", "StartedAt": "1763554034889"}	0
24	2	Failed	An exception occurred during performance of the job.	2025-11-19 12:07:20.808133+00	{"FailedAt": "1763554040792", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
25	2	Scheduled	Retry attempt 3 of 3: 3F000: schema "monitoring" does not exist\n\nPOSITI…	2025-11-19 12:07:20.811606+00	{"EnqueueAt": "1763554160802", "ScheduledAt": "1763554040802"}	0
26	1	Failed	An exception occurred during performance of the job.	2025-11-19 12:07:22.610348+00	{"FailedAt": "1763554042593", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlConnection.OpenAsync(CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenDbConnectionAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
27	1	Scheduled	Retry attempt 3 of 3: Cannot access a disposed object.\nObject name: 'Np…	2025-11-19 12:07:22.616214+00	{"EnqueueAt": "1763554162605", "ScheduledAt": "1763554042605"}	0
28	2	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:09:29.930052+00	{"Queue": "default", "EnqueuedAt": "1763554169927"}	0
29	1	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:09:29.940247+00	{"Queue": "default", "EnqueuedAt": "1763554169936"}	0
30	1	Processing	\N	2025-11-19 12:09:29.94709+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "fa8aff46-ed50-4416-807a-0eebd8827322", "StartedAt": "1763554169944"}	0
31	2	Processing	\N	2025-11-19 12:09:30.939174+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "7d20c94a-3ece-40bc-b397-008052c7101f", "StartedAt": "1763554169934"}	0
32	1	Failed	An exception occurred during performance of the job.	2025-11-19 12:09:40.554218+00	{"FailedAt": "1763554180543", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlConnection.OpenAsync(CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenDbConnectionAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
35	4	Processing	\N	2025-11-19 12:10:14.992415+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "6ea81699-df6d-4b52-a1d2-4f658295be12", "StartedAt": "1763554214983"}	0
37	5	Processing	\N	2025-11-19 12:10:15.026663+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "adce7aa9-2e95-4bbe-8300-e9cf06be8589", "StartedAt": "1763554215016"}	0
44	6	Succeeded	\N	2025-11-19 12:10:24.842092+00	{"Latency": "51", "SucceededAt": "1763554224832", "PerformanceDuration": "9754"}	0
33	2	Failed	An exception occurred during performance of the job.	2025-11-19 12:09:41.016211+00	{"FailedAt": "1763554180985", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
34	4	Enqueued	Triggered by recurring job scheduler	2025-11-19 12:10:14.970433+00	{"Queue": "default", "EnqueuedAt": "1763554214970"}	0
36	5	Enqueued	Triggered by recurring job scheduler	2025-11-19 12:10:14.992754+00	{"Queue": "default", "EnqueuedAt": "1763554214992"}	0
38	6	Enqueued	Triggered by recurring job scheduler	2025-11-19 12:10:15.031996+00	{"Queue": "default", "EnqueuedAt": "1763554215030"}	0
39	6	Processing	\N	2025-11-19 12:10:15.06874+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "aa1e1184-8c89-4796-8c8e-a31a7bac9efc", "StartedAt": "1763554215055"}	0
42	5	Failed	An exception occurred during performance of the job.	2025-11-19 12:10:22.811956+00	{"FailedAt": "1763554222790", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
43	5	Scheduled	Retry attempt 1 of 3: 3F000: schema "monitoring" does not exist\n\nPOSITI…	2025-11-19 12:10:22.815202+00	{"EnqueueAt": "1763554252801", "ScheduledAt": "1763554222801"}	0
47	4	Processing	\N	2025-11-19 12:11:00.108569+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "6ea81699-df6d-4b52-a1d2-4f658295be12", "StartedAt": "1763554260103"}	0
51	5	Failed	An exception occurred during performance of the job.	2025-11-19 12:11:09.940881+00	{"FailedAt": "1763554269897", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
52	5	Scheduled	Retry attempt 2 of 3: 3F000: schema "monitoring" does not exist\n\nPOSITI…	2025-11-19 12:11:09.949344+00	{"EnqueueAt": "1763554329921", "ScheduledAt": "1763554269921"}	0
40	4	Failed	An exception occurred during performance of the job.	2025-11-19 12:10:21.922397+00	{"FailedAt": "1763554221891", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlConnection.OpenAsync(CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenDbConnectionAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
41	4	Scheduled	Retry attempt 1 of 3: Cannot access a disposed object.\nObject name: 'Np…	2025-11-19 12:10:21.925241+00	{"EnqueueAt": "1763554251914", "ScheduledAt": "1763554221914"}	0
48	5	Processing	\N	2025-11-19 12:11:01.125471+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "adce7aa9-2e95-4bbe-8300-e9cf06be8589", "StartedAt": "1763554260109"}	0
49	4	Failed	An exception occurred during performance of the job.	2025-11-19 12:11:09.535756+00	{"FailedAt": "1763554269523", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
50	4	Scheduled	Retry attempt 2 of 3: Cannot access a disposed object.\nObject name: 'Np…	2025-11-19 12:11:09.539175+00	{"EnqueueAt": "1763554329531", "ScheduledAt": "1763554269531"}	0
53	4	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:12:15.1658+00	{"Queue": "default", "EnqueuedAt": "1763554335160"}	0
54	5	Enqueued	Triggered by DelayedJobScheduler	2025-11-19 12:12:15.177384+00	{"Queue": "default", "EnqueuedAt": "1763554335170"}	0
55	4	Processing	\N	2025-11-19 12:12:15.248354+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "6ea81699-df6d-4b52-a1d2-4f658295be12", "StartedAt": "1763554335242"}	0
56	5	Processing	\N	2025-11-19 12:12:15.250166+00	{"ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "WorkerId": "aa1e1184-8c89-4796-8c8e-a31a7bac9efc", "StartedAt": "1763554335245"}	0
57	5	Failed	An exception occurred during performance of the job.	2025-11-19 12:12:22.405656+00	{"FailedAt": "1763554342388", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "Npgsql.PostgresException", "ExceptionDetails": "Npgsql.PostgresException: 3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15\\n   at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)\\n   at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.InitializeReaderAsync(AsyncEnumerator enumerator, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Query.Internal.FromSqlQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.RefreshDashboardMetricsAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 172\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<RefreshDashboardSummaryAsync>b__5_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 114\\n   at Polly.ResiliencePipeline.<>c.<<ExecuteAsync>b__3_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync(Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.RefreshDashboardSummaryAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 112\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "3F000: schema \\"monitoring\\" does not exist\\n\\nPOSITION: 15"}	0
58	5	Scheduled	Retry attempt 3 of 3: 3F000: schema "monitoring" does not exist\n\nPOSITI…	2025-11-19 12:12:22.410529+00	{"EnqueueAt": "1763554462394", "ScheduledAt": "1763554342394"}	0
59	4	Failed	An exception occurred during performance of the job.	2025-11-19 12:12:25.699774+00	{"FailedAt": "1763554345684", "ServerId": "hrms-codespaces-eaf641:73552:4894e272-5fff-4ba3-abe3-f36c1a739fd6", "ExceptionType": "System.ObjectDisposedException", "ExceptionDetails": "System.ObjectDisposedException: Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'.\\n   at Npgsql.ThrowHelper.ThrowObjectDisposedException(String objectName)\\n   at Npgsql.NpgsqlConnection.Open(Boolean async, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenInternalAsync(Boolean errorsExpected, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.OpenAsync(CancellationToken cancellationToken, Boolean errorsExpected)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)\\n   at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1237\\n   at HRMS.Infrastructure.Services.MonitoringService.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs:line 1253\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.<CapturePerformanceSnapshotAsync>b__4_0(CancellationToken token) in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 64\\n   at Polly.ResiliencePipeline.<>c__10`1.<<ExecuteAsync>b__10_0>d.MoveNext()\\n--- End of stack trace from previous location ---\\n   at Polly.Outcome`1.GetResultOrRethrow()\\n   at Polly.ResiliencePipeline.ExecuteAsync[TResult](Func`2 callback, CancellationToken cancellationToken)\\n   at HRMS.Infrastructure.BackgroundJobs.MonitoringJobs.CapturePerformanceSnapshotAsync() in /workspaces/HRAPP/src/HRMS.Infrastructure/BackgroundJobs/MonitoringJobs.cs:line 62\\n   at InvokeStub_TaskAwaiter.GetResult(Object, Object, IntPtr*)\\n   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)\\n", "ExceptionMessage": "Cannot access a disposed object.\\nObject name: 'NpgsqlConnection'."}	0
60	4	Scheduled	Retry attempt 3 of 3: Cannot access a disposed object.\nObject name: 'Np…	2025-11-19 12:12:25.70225+00	{"EnqueueAt": "1763554465696", "ScheduledAt": "1763554345696"}	0
\.


--
-- Data for Name: ActivationResendLogs; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."ActivationResendLogs" ("Id", "TenantId", "RequestedAt", "RequestedFromIp", "RequestedByEmail", "TokenGenerated", "TokenExpiry", "Success", "FailureReason", "UserAgent", "DeviceInfo", "Geolocation", "EmailDelivered", "EmailSendError", "ResendCountLastHour", "WasRateLimited", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: AdminUsers; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."AdminUsers" ("Id", "UserName", "Email", "PasswordHash", "FirstName", "LastName", "IsActive", "LastLoginDate", "PhoneNumber", "IsTwoFactorEnabled", "TwoFactorSecret", "BackupCodes", "LockoutEnabled", "LockoutEnd", "AccessFailedCount", "LastPasswordChangeDate", "PasswordExpiresAt", "MustChangePassword", "AllowedIPAddresses", "SessionTimeoutMinutes", "Permissions", "LastLoginIPAddress", "LastFailedLoginAttempt", "IsInitialSetupAccount", "PasswordHistory", "CreatedBySuperAdminId", "LastModifiedBySuperAdminId", "AllowedLoginHours", "StatusNotes", "PasswordResetToken", "PasswordResetTokenExpiry", "ActivationTokenExpiry", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
0ca74424-a59b-4b81-862b-00c2543b925a	Super Admin	admin@hrms.com	s4DY7lZgqHbGQuv57WTSroR85DUVzun8woo1IKI6GT1zoRcq4PyTMn5t1TZJgHr5	Super	Admin	t	2025-11-19 12:09:21.313486+00	\N	f	\N	\N	t	\N	0	\N	\N	f	\N	15	["*"]	127.0.0.1	\N	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-11-19 12:01:26.469165+00	System	2025-11-19 12:01:26.469207+00	System	f	\N	\N
\.


--
-- Data for Name: AuditLogs; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."AuditLogs" ("Id", "TenantId", "TenantName", "UserId", "UserEmail", "UserFullName", "UserRole", "SessionId", "ActionType", "Category", "Severity", "EntityType", "EntityId", "Success", "ErrorMessage", "OldValues", "NewValues", "ChangedFields", "Reason", "ApprovalReference", "IpAddress", "Geolocation", "UserAgent", "DeviceInfo", "NetworkInfo", "PerformedAt", "DurationMs", "BusinessDate", "PolicyReference", "DocumentationLink", "HttpMethod", "RequestPath", "QueryString", "ResponseCode", "CorrelationId", "ParentActionId", "AdditionalMetadata", "Checksum", "IsArchived", "ArchivedAt", "IsUnderLegalHold", "LegalHoldId") FROM stdin;
906f6b31-57b1-4dbf-9c40-c3476a5130cd	\N	\N	\N	\N	\N	\N	\N	111	3	1	AdminUser	0ca74424-a59b-4b81-862b-00c2543b925a	t	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-11-19 12:01:26.707172+00	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	7yQB1V+2gXtFBCB9KulM9bZjZ8VoZW+VYOSgcrWwfq0=	f	\N	f	\N
4afb40da-a17c-4d22-a870-b2fe836a496b	\N	\N	\N	\N	\N	\N	0HNH7CE8L5GH3:00000001	114	7	1	\N	\N	t	\N	\N	\N	\N	\N	\N	127.0.0.1	\N	curl/8.5.0	\N	\N	2025-11-19 12:02:53.243855+00	64	\N	\N	\N	GET	/health		200	e2b270233c824cdd8530b96a20ecd715	\N	{"Host": "localhost:5090", "Scheme": "http", "Protocol": "HTTP/1.1", "ContentType": null, "ContentLength": null}	9c38058ab93b4e5d5c9addf3dfc4f42454051b170531f0c40bd67da6e279e9e1	f	\N	f	\N
807b5c1a-953d-4372-a2ce-bfb51b6dafd9	\N	\N	\N	\N	\N	\N	\N	112	3	1	AdminUser	0ca74424-a59b-4b81-862b-00c2543b925a	t	\N	\N	\N	LastLoginDate, LastLoginIPAddress	\N	\N	127.0.0.1	\N	curl/8.5.0	\N	\N	2025-11-19 12:09:21.411497+00	\N	\N	\N	\N	POST	/api/auth/login	\N	\N	\N	\N	\N	RuE2rx/quTMCNJqVM0dj+RlBSYPLEpyJNUoF7GOlT0s=	f	\N	f	\N
10690fe1-18f5-41fe-9ca4-44c534c0901a	\N	\N	0ca74424-a59b-4b81-862b-00c2543b925a	admin@hrms.com	\N	\N	0HNH7CE8L5GH4:00000001	1	1	1	\N	\N	t	\N	\N	\N	\N	\N	\N	127.0.0.1	\N	curl/8.5.0	\N	\N	2025-11-19 12:09:21.477833+00	\N	\N	\N	\N	POST	/api/auth/login		\N	0HNH7CE8L5GH4:00000001	\N	\N	0254badd072de808218151306fb28d4a6eae08dc2bd3dcc4f0e3868573d42e47	f	\N	f	\N
e3991d8c-0986-4dda-9e33-1833f16204ba	\N	\N	\N	\N	\N	\N	0HNH7CE8L5GH4:00000001	1	1	1	\N	\N	t	\N	\N	\N	\N	\N	\N	127.0.0.1	\N	curl/8.5.0	\N	\N	2025-11-19 12:09:21.527398+00	1348	\N	\N	\N	POST	/api/auth/login		200	b9a18beb6a334acfbfe13cfdcdc44853	\N	{"Host": "localhost:5090", "Scheme": "http", "Protocol": "HTTP/1.1", "ContentType": "application/json", "ContentLength": 49}	923159d15e1c765d3fd219c3473f883302ae389d74dd24ae9b8b46cc20d712c8	f	\N	f	\N
\.


--
-- Data for Name: DetectedAnomalies; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."DetectedAnomalies" ("Id", "TenantId", "AnomalyType", "RiskLevel", "Status", "RiskScore", "UserId", "UserEmail", "IpAddress", "Location", "DetectedAt", "Description", "Evidence", "RelatedAuditLogIds", "DetectionRule", "ModelVersion", "InvestigatedBy", "InvestigatedAt", "InvestigationNotes", "Resolution", "ResolvedAt", "NotificationSent", "NotificationSentAt", "NotificationRecipients", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: Districts; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."Districts" ("Id", "DistrictCode", "DistrictName", "DistrictNameFrench", "Region", "AreaSqKm", "Population", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
\.


--
-- Data for Name: FeatureFlags; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."FeatureFlags" ("Id", "TenantId", "Module", "IsEnabled", "RolloutPercentage", "Description", "Tags", "MinimumTier", "IsEmergencyDisabled", "EmergencyDisabledReason", "EmergencyDisabledAt", "EmergencyDisabledBy", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: IndustrySectors; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."IndustrySectors" ("Id", "SectorCode", "SectorName", "SectorNameFrench", "Description", "ParentSectorId", "RemunerationOrderReference", "RemunerationOrderYear", "IsActive", "RequiresSpecialPermits", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
1	CAT	Catering & Tourism Industries	Industries de l'Hôtellerie et du Tourisme	\N	\N	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903215+00	\N	f
10	CONST	Construction & Quarrying Industries	Industries de la Construction et des Carrières	\N	\N	GN No. 162 of 2022	2022	t	f	2025-11-19 12:01:26.903444+00	\N	f
11	BPO	Business Process Outsourcing (BPO)	Externalisation des Processus d'Affaires	\N	\N	GN No. 201 of 2023	2023	t	f	2025-11-19 12:01:26.903445+00	\N	f
12	SECURITY	Security Services	Services de Sécurité	\N	\N	GN No. 178 of 2023	2023	t	t	2025-11-19 12:01:26.903445+00	\N	f
13	CLEANING	Cleaning Services	Services de Nettoyage	\N	\N	GN No. 165 of 2022	2022	t	f	2025-11-19 12:01:26.903446+00	\N	f
14	FINANCE	Financial Services	Services Financiers	\N	\N	GN No. 189 of 2023	2023	t	t	2025-11-19 12:01:26.903446+00	\N	f
19	ICT	Information & Communication Technology	Technologies de l'Information et de la Communication	\N	\N	GN No. 195 of 2023	2023	t	f	2025-11-19 12:01:26.903448+00	\N	f
20	MANUFACTURING	Manufacturing Industries	Industries Manufacturières	\N	\N	GN No. 172 of 2022	2022	t	f	2025-11-19 12:01:26.903449+00	\N	f
25	RETAIL	Retail & Distributive Trade	Commerce de Détail et Distribution	\N	\N	GN No. 183 of 2023	2023	t	f	2025-11-19 12:01:26.90345+00	\N	f
29	HEALTHCARE	Healthcare & Medical Services	Services de Santé et Médicaux	\N	\N	GN No. 191 of 2023	2023	t	t	2025-11-19 12:01:26.903455+00	\N	f
30	EDUCATION	Education & Training Services	Services d'Éducation et de Formation	\N	\N	GN No. 187 of 2023	2023	t	f	2025-11-19 12:01:26.903455+00	\N	f
31	TRANSPORT	Transport & Logistics	Transport et Logistique	\N	\N	GN No. 174 of 2022	2022	t	t	2025-11-19 12:01:26.903456+00	\N	f
35	BAKERY	Bakeries & Confectionery	Boulangeries et Confiseries	\N	\N	GN No. 168 of 2022	2022	t	f	2025-11-19 12:01:26.903457+00	\N	f
36	ENTERTAINMENT	Cinema & Entertainment	Cinéma et Divertissement	\N	\N	GN No. 179 of 2023	2023	t	f	2025-11-19 12:01:26.903457+00	\N	f
37	LEGAL	Legal Services (Attorneys & Notaries)	Services Juridiques (Avocats et Notaires)	\N	\N	GN No. 193 of 2023	2023	t	t	2025-11-19 12:01:26.903458+00	\N	f
38	REALESTATE	Real Estate & Property Management	Immobilier et Gestion de Propriétés	\N	\N	GN No. 186 of 2023	2023	t	f	2025-11-19 12:01:26.903458+00	\N	f
39	AGRICULTURE	Agriculture & Fishing	Agriculture et Pêche	\N	\N	GN No. 161 of 2022	2022	t	f	2025-11-19 12:01:26.903459+00	\N	f
40	PRINTING	Printing & Publishing	Imprimerie et Édition	\N	\N	GN No. 177 of 2023	2023	t	f	2025-11-19 12:01:26.903459+00	\N	f
41	TELECOM	Telecommunications	Télécommunications	\N	\N	GN No. 197 of 2023	2023	t	t	2025-11-19 12:01:26.903459+00	\N	f
42	IMPORTEXPORT	Import/Export Trade	Commerce d'Importation/Exportation	\N	\N	GN No. 182 of 2023	2023	t	f	2025-11-19 12:01:26.903459+00	\N	f
43	WAREHOUSE	Warehouse & Storage Services	Services d'Entreposage et de Stockage	\N	\N	GN No. 175 of 2022	2022	t	f	2025-11-19 12:01:26.90346+00	\N	f
44	PROFESSIONAL	Professional Services (Accounting, Consulting)	Services Professionnels (Comptabilité, Conseil)	\N	\N	GN No. 192 of 2023	2023	t	f	2025-11-19 12:01:26.90346+00	\N	f
45	HOSPITALITY	Hospitality Services (excluding Catering)	Services d'Accueil (hors Restauration)	\N	\N	GN No. 188 of 2023	2023	t	f	2025-11-19 12:01:26.90346+00	\N	f
46	BEAUTY	Beauty & Wellness Services	Services de Beauté et de Bien-être	\N	\N	GN No. 169 of 2022	2022	t	f	2025-11-19 12:01:26.90346+00	\N	f
47	EVENTS	Event Management & Wedding Services	Gestion d'Événements et Services de Mariage	\N	\N	GN No. 184 of 2023	2023	t	f	2025-11-19 12:01:26.903461+00	\N	f
48	DOMESTIC	Domestic Services	Services Domestiques	\N	\N	GN No. 166 of 2022	2022	t	f	2025-11-19 12:01:26.903461+00	\N	f
49	GENERAL	General Office & Administrative Services	Services Généraux de Bureau et Administratifs	\N	\N	GN No. 199 of 2023	2023	t	f	2025-11-19 12:01:26.903461+00	\N	f
50	PHARMACY	Pharmacy & Pharmaceutical Services	Pharmacie et Services Pharmaceutiques	\N	\N	GN No. 190 of 2023	2023	t	t	2025-11-19 12:01:26.903462+00	\N	f
51	AUTOMOTIVE	Automotive Services & Repairs	Services Automobiles et Réparations	\N	\N	GN No. 170 of 2022	2022	t	f	2025-11-19 12:01:26.903462+00	\N	f
52	RECRUITMENT	Recruitment & Employment Agencies	Agences de Recrutement et d'Emploi	\N	\N	GN No. 194 of 2023	2023	t	t	2025-11-19 12:01:26.903462+00	\N	f
2	CAT-HOTEL-LARGE	Hotels & Accommodation (40+ covers)	Hôtels et Hébergement (40+ couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903283+00	\N	f
3	CAT-HOTEL-SMALL	Hotels & Accommodation (Under 40 covers)	Hôtels et Hébergement (Moins de 40 couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903284+00	\N	f
4	CAT-RESTAURANT-LARGE	Restaurants & Cafés (40+ covers)	Restaurants et Cafés (40+ couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903284+00	\N	f
5	CAT-RESTAURANT-SMALL	Restaurants & Cafés (Under 40 covers)	Restaurants et Cafés (Moins de 40 couverts)	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903284+00	\N	f
6	CAT-FASTFOOD	Fast Food Outlets	Restauration Rapide	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903284+00	\N	f
7	CAT-BARS	Bars & Night Clubs	Bars et Boîtes de Nuit	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903285+00	\N	f
8	CAT-BOARDINGHOUSE	Boarding Houses & Guest Houses	Pensions et Maisons d'Hôtes	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903285+00	\N	f
9	CAT-ATTRACTIONS	Tourist Attractions & Leisure Parks	Attractions Touristiques et Parcs de Loisirs	\N	1	GN No. 185 of 2023	2023	t	f	2025-11-19 12:01:26.903285+00	\N	f
15	FINANCE-BANKING	Banking	Services Bancaires	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-19 12:01:26.903446+00	\N	f
16	FINANCE-INSURANCE	Insurance	Assurance	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-19 12:01:26.903447+00	\N	f
17	FINANCE-FUND	Fund Administration	Administration de Fonds	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-19 12:01:26.903447+00	\N	f
18	FINANCE-WEALTH	Wealth Management	Gestion de Patrimoine	\N	14	GN No. 189 of 2023	2023	t	t	2025-11-19 12:01:26.903447+00	\N	f
21	MFG-TEXTILE	Textiles & Apparel (Export Enterprises)	Textile et Habillement (Entreprises d'Exportation)	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-19 12:01:26.903449+00	\N	f
22	MFG-JEWELRY	Jewelry & Optical Goods	Bijouterie et Optique	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-19 12:01:26.903449+00	\N	f
23	MFG-FURNITURE	Furniture Manufacturing	Fabrication de Meubles	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-19 12:01:26.90345+00	\N	f
24	MFG-FOOD	Food Processing & Beverages	Transformation Alimentaire et Boissons	\N	20	GN No. 172 of 2022	2022	t	f	2025-11-19 12:01:26.90345+00	\N	f
26	RETAIL-SUPERMARKET	Supermarkets & Hypermarkets	Supermarchés et Hypermarchés	\N	25	GN No. 183 of 2023	2023	t	f	2025-11-19 12:01:26.903451+00	\N	f
27	RETAIL-SHOPS	Shops & Stores	Boutiques et Magasins	\N	25	GN No. 183 of 2023	2023	t	f	2025-11-19 12:01:26.903451+00	\N	f
28	RETAIL-WHOLESALE	Wholesale Trade	Commerce de Gros	\N	25	GN No. 183 of 2023	2023	t	f	2025-11-19 12:01:26.903455+00	\N	f
32	TRANSPORT-FREIGHT	Freight & Cargo Services	Services de Fret et Cargaison	\N	31	GN No. 174 of 2022	2022	t	t	2025-11-19 12:01:26.903456+00	\N	f
33	TRANSPORT-TAXI	Taxi Services	Services de Taxi	\N	31	GN No. 174 of 2022	2022	t	t	2025-11-19 12:01:26.903456+00	\N	f
34	TRANSPORT-BUS	Bus Services	Services de Bus	\N	31	GN No. 174 of 2022	2022	t	t	2025-11-19 12:01:26.903457+00	\N	f
\.


--
-- Data for Name: LegalHolds; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."LegalHolds" ("Id", "TenantId", "CaseNumber", "Description", "Reason", "StartDate", "EndDate", "Status", "UserIds", "EntityTypes", "SearchKeywords", "RequestedBy", "LegalRepresentative", "LegalRepresentativeEmail", "LawFirm", "CourtOrder", "ReleasedBy", "ReleasedAt", "ReleaseNotes", "AffectedAuditLogCount", "AffectedEntityCount", "NotifiedUsers", "NotificationSentAt", "ComplianceFrameworks", "RetentionPeriodDays", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "AdditionalMetadata") FROM stdin;
\.


--
-- Data for Name: PostalCodes; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."PostalCodes" ("Id", "Code", "VillageName", "DistrictName", "Region", "VillageId", "DistrictId", "LocalityType", "IsPrimary", "Notes", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
\.


--
-- Data for Name: RefreshTokens; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."RefreshTokens" ("Id", "AdminUserId", "TenantId", "EmployeeId", "Token", "ExpiresAt", "CreatedAt", "CreatedByIp", "RevokedAt", "RevokedByIp", "ReplacedByToken", "ReasonRevoked", "SessionTimeoutMinutes", "LastActivityAt") FROM stdin;
1625aeac-0779-4292-91df-92b777b99a80	0ca74424-a59b-4b81-862b-00c2543b925a	\N	\N	FoN11As4gqY5euqIob/oOSJAYG48p8kSJPtuXItccYAMftDCJZj0ugKEpKCHi2WMcr0LrIonTeE3PlFbNk7WLQ==	2025-11-26 12:09:21.348928+00	2025-11-19 12:09:21.348928+00	127.0.0.1	\N	\N	\N	\N	30	2025-11-19 12:09:21.348928+00
\.


--
-- Data for Name: SectorComplianceRules; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."SectorComplianceRules" ("Id", "SectorId", "RuleCategory", "RuleName", "RuleConfig", "EffectiveFrom", "EffectiveTo", "LegalReference", "Notes", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
1	2	OVERTIME	Catering & Tourism - Overtime Rates	{"sunday_rate": 2.0, "night_shift_rate": 1.25, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "time_off_in_lieu_allowed": true, "meal_allowance_amount_mur": 85, "max_overtime_hours_per_day": 4, "meal_allowance_after_hours": 2, "max_overtime_hours_per_week": 20, "cyclone_warning_class_3_rate": 3.0, "public_holiday_after_hours_rate": 3.0, "public_holiday_normal_hours_rate": 2.0}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023 - Catering & Tourism Remuneration Order	\N	2025-11-19 12:01:27.171155+00	\N	f
2	2	MINIMUM_WAGE	Catering & Tourism - Minimum Wage 2025	{"notes": "Minimum wage increased Jan 2025 from MUR 16,500 to MUR 17,110. Salary compensation increased from MUR 500 to MUR 610.", "currency": "MUR", "effective_date": "2025-01-01", "salary_compensation_mur": 610, "monthly_minimum_wage_mur": 17110, "applies_to_basic_salary_up_to_mur": 50000}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023 + National Minimum Wage Regulations 2025	\N	2025-11-19 12:01:27.17116+00	\N	f
3	2	WORKING_HOURS	Catering & Tourism - Working Hours	{"daily_max_hours": 12, "rotational_shifts": true, "unsocial_hours_end": "06:00", "standard_daily_hours": 9, "unsocial_hours_start": "22:00", "standard_weekly_hours": 45, "shift_patterns_allowed": true, "working_pattern_options": ["9 hours x 5 days per week", "8 hours x 5 days + 5 hours x 1 day"], "mandatory_lunch_break_minutes": 60}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023 - Catering & Tourism Remuneration Order	\N	2025-11-19 12:01:27.17116+00	\N	f
4	2	ALLOWANCES	Catering & Tourism - Allowances	{"tips_pooling_allowed": true, "service_charge_distribution": "As per hotel policy", "housing_allowance_applicable": false, "meal_allowance_per_shift_mur": 85, "uniform_allowance_per_year_mur": 500, "transport_allowance_per_month_mur": 0}	2025-01-01 00:00:00+00	\N	GN No. 185 of 2023	\N	2025-11-19 12:01:27.171161+00	\N	f
5	2	LEAVE	Catering & Tourism - Leave Entitlements	{"sick_leave_days": 15, "annual_leave_days": 22, "casual_leave_days": 5, "paternity_leave_days": 5, "maternity_leave_weeks": 14, "leave_calculation_basis": "working_days", "encashment_allowed_on_resignation": true, "annual_leave_carry_forward_max_days": 5}	2025-01-01 00:00:00+00	\N	Workers' Rights Act 2019 + GN No. 185 of 2023	\N	2025-11-19 12:01:27.171161+00	\N	f
6	2	GRATUITY	Catering & Tourism - Gratuity Calculation	{"notes": "Gratuity = (Basic Salary / 22) * 15 * Years of Service", "formula": "15 days per year of service", "calculation_basis": "basic_salary", "minimum_service_months": 12, "max_gratuity_amount_mur": null, "pro_rated_for_partial_year": true}	2025-01-01 00:00:00+00	\N	Workers' Rights Act 2019 Section 111	\N	2025-11-19 12:01:27.171162+00	\N	f
7	11	OVERTIME	BPO - Overtime Rates	{"sunday_rate": 2.0, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "time_off_in_lieu_allowed": true, "max_overtime_hours_per_day": 4, "max_overtime_hours_per_week": 20, "public_holiday_after_hours_rate": 3.0, "night_shift_allowance_percentage": 10, "public_holiday_normal_hours_rate": 2.0}	2025-01-01 00:00:00+00	\N	GN No. 201 of 2023 - BPO Remuneration Order	\N	2025-11-19 12:01:27.171162+00	\N	f
8	11	MINIMUM_WAGE	BPO - Minimum Wage 2025	{"currency": "MUR", "effective_date": "2025-01-01", "salary_compensation_mur": 610, "monthly_minimum_wage_mur": 17110, "applies_to_basic_salary_up_to_mur": 50000}	2025-01-01 00:00:00+00	\N	GN No. 201 of 2023 + National Minimum Wage Regulations 2025	\N	2025-11-19 12:01:27.171162+00	\N	f
9	11	WORKING_HOURS	BPO - Working Hours	{"daily_max_hours": 12, "rotational_shifts": true, "night_shifts_allowed": true, "standard_daily_hours": 9, "standard_weekly_hours": 45, "shift_patterns_allowed": true, "mandatory_break_minutes": 60, "working_pattern_options": ["9 hours x 5 days per week", "8 hours x 5 days + 5 hours x 1 day"]}	2025-01-01 00:00:00+00	\N	GN No. 201 of 2023	\N	2025-11-19 12:01:27.171162+00	\N	f
10	12	OVERTIME	Security Services - Overtime Rates	{"sunday_rate": 2.0, "night_shift_rate": 1.1, "weekday_overtime_rate": 1.25, "weekend_overtime_rate": 1.5, "time_off_in_lieu_allowed": false, "max_overtime_hours_per_day": 6, "max_overtime_hours_per_week": 24, "public_holiday_after_hours_rate": 2.5, "public_holiday_normal_hours_rate": 2.0}	2025-01-01 00:00:00+00	\N	GN No. 178 of 2023 - Security Services Remuneration Order	\N	2025-11-19 12:01:27.171163+00	\N	f
11	12	WORKING_HOURS	Security Services - Working Hours	{"daily_max_hours": 14, "rotational_shifts": true, "standard_daily_hours": 8, "standard_weekly_hours": 48, "shift_patterns_allowed": true, "mandatory_break_minutes": 60, "working_pattern_options": ["8 hours x 6 days per week", "12-hour shifts (rotating)"], "continuous_duty_max_hours": 12}	2025-01-01 00:00:00+00	\N	GN No. 178 of 2023	\N	2025-11-19 12:01:27.171163+00	\N	f
12	15	WORKING_HOURS	Banking - Working Hours	{"daily_max_hours": 10, "standard_daily_hours": 8, "standard_weekly_hours": 40, "shift_patterns_allowed": false, "mandatory_break_minutes": 60, "working_pattern_options": ["8 hours x 5 days per week (Monday-Friday)"], "weekend_work_exceptional_only": true}	2025-01-01 00:00:00+00	\N	GN No. 189 of 2023 - Financial Services Remuneration Order	\N	2025-11-19 12:01:27.171163+00	\N	f
13	15	OVERTIME	Banking - Overtime Rates	{"notes": "Banking sector typically minimizes overtime due to strict working hours", "sunday_rate": 2.5, "public_holiday_rate": 3.0, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.5, "time_off_in_lieu_preferred": true, "max_overtime_hours_per_week": 10}	2025-01-01 00:00:00+00	\N	GN No. 189 of 2023	\N	2025-11-19 12:01:27.171163+00	\N	f
14	10	OVERTIME	Construction - Overtime Rates	{"sunday_rate": 2.0, "night_shift_rate": 1.25, "public_holiday_rate": 2.5, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "max_overtime_hours_per_day": 4, "max_overtime_hours_per_week": 20}	2025-01-01 00:00:00+00	\N	GN No. 162 of 2022 - Construction Remuneration Order	\N	2025-11-19 12:01:27.171164+00	\N	f
15	10	ALLOWANCES	Construction - Allowances	{"meal_allowance_per_day_mur": 85, "hazard_allowance_applicable": true, "tool_allowance_per_month_mur": 200, "transport_allowance_per_day_mur": 50, "height_work_allowance_per_day_mur": 100, "safety_equipment_provided_by_employer": true}	2025-01-01 00:00:00+00	\N	GN No. 162 of 2022	\N	2025-11-19 12:01:27.171164+00	\N	f
16	26	WORKING_HOURS	Retail - Working Hours	{"daily_max_hours": 12, "standard_daily_hours": 9, "standard_weekly_hours": 45, "shift_patterns_allowed": true, "mandatory_break_minutes": 60, "working_pattern_options": ["9 hours x 5 days per week", "8 hours x 5 days + 5 hours x 1 day"], "sunday_trading_hours_restricted": true, "public_holiday_trading_requires_approval": true}	2025-01-01 00:00:00+00	\N	GN No. 183 of 2023 - Retail & Distributive Trade Remuneration Order	\N	2025-11-19 12:01:27.171166+00	\N	f
17	26	OVERTIME	Retail - Overtime Rates	{"sunday_rate": 2.5, "public_holiday_rate": 3.0, "weekday_overtime_rate": 1.5, "weekend_overtime_rate": 2.0, "time_off_in_lieu_allowed": true, "max_overtime_hours_per_day": 4, "max_overtime_hours_per_week": 20}	2025-01-01 00:00:00+00	\N	GN No. 183 of 2023	\N	2025-11-19 12:01:27.171166+00	\N	f
\.


--
-- Data for Name: SecurityAlerts; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."SecurityAlerts" ("Id", "AlertType", "Severity", "Category", "Status", "Title", "Description", "RecommendedActions", "RiskScore", "AuditLogId", "AuditActionType", "TenantId", "TenantName", "UserId", "UserEmail", "UserFullName", "UserRole", "IpAddress", "Geolocation", "UserAgent", "DeviceInfo", "CreatedAt", "DetectedAt", "AcknowledgedAt", "ResolvedAt", "AcknowledgedBy", "AcknowledgedByEmail", "ResolvedBy", "ResolvedByEmail", "ResolutionNotes", "AssignedTo", "AssignedToEmail", "EmailSent", "EmailSentAt", "EmailRecipients", "SmsSent", "SmsSentAt", "SmsRecipients", "SlackSent", "SlackSentAt", "SlackChannels", "SiemSent", "SiemSentAt", "SiemSystem", "DetectionRule", "BaselineMetrics", "CurrentMetrics", "DeviationPercentage", "CorrelationId", "AdditionalMetadata", "Tags", "ComplianceFrameworks", "RequiresEscalation", "EscalatedTo", "EscalatedAt", "IsDeleted", "DeletedAt") FROM stdin;
\.


--
-- Data for Name: SubscriptionNotificationLogs; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."SubscriptionNotificationLogs" ("Id", "TenantId", "NotificationType", "SentDate", "RecipientEmail", "EmailSubject", "DeliverySuccess", "DeliveryError", "SubscriptionEndDateAtNotification", "DaysUntilExpiryAtNotification", "SubscriptionPaymentId", "RequiresFollowUp", "FollowUpCompletedDate", "FollowUpNotes", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: SubscriptionPayments; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."SubscriptionPayments" ("Id", "TenantId", "PeriodStartDate", "PeriodEndDate", "AmountMUR", "SubtotalMUR", "TaxRate", "TaxAmountMUR", "TotalMUR", "IsTaxExempt", "Status", "PaidDate", "ProcessedBy", "PaymentReference", "PaymentMethod", "DueDate", "Notes", "EmployeeTier", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Tenants; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."Tenants" ("Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone", "Status", "EmployeeTier", "YearlyPriceMUR", "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth", "SuspensionReason", "SuspensionDate", "SoftDeleteDate", "DeletionReason", "GracePeriodDays", "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate", "LastNotificationSent", "LastNotificationType", "GracePeriodStartDate", "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName", "ActivationToken", "ActivationTokenExpiry", "ActivatedAt", "ActivatedBy", "IsGovernmentEntity", "CurrentUserCount", "CurrentStorageGB", "SectorId", "SectorSelectedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: Villages; Type: TABLE DATA; Schema: master; Owner: postgres
--

COPY master."Villages" ("Id", "VillageCode", "VillageName", "VillageNameFrench", "PostalCode", "DistrictId", "LocalityType", "Latitude", "Longitude", "DisplayOrder", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted") FROM stdin;
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20251031135011_InitialMasterSchema	9.0.0
20251104020635_AddApiCallsPerMonthToTenant	9.0.0
20251107043300_AddMauritiusAddressHierarchyWithSeedData	9.0.0
20251108031642_AddRefreshTokens	9.0.0
20251108042110_AddMfaBackupCodes	9.0.0
20251108054617_AddTenantActivationFields	9.0.0
20251108120244_EnhancedAuditLog	9.0.0
20251110032635_AddSecurityAlertTable	9.0.0
20251110062536_AuditLogImmutabilityAndSecurityFixes	9.0.0
20251110074843_AddSuperAdminSecurityFields	9.0.0
20251110093755_AddFortune500ComplianceFeatures	9.0.0
20251110125444_AddSubscriptionManagementSystem	9.0.0
20251111125329_InitialMasterDb	9.0.0
20251113040317_AddSecurityEnhancements	9.0.0
20251119100114_AddPasswordExpiresAtToEmployeeAndAdminUser	9.0.0
\.


--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.aggregatedcounter_id_seq', 8, true);


--
-- Name: counter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.counter_id_seq', 10, true);


--
-- Name: hash_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.hash_id_seq', 139, true);


--
-- Name: job_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.job_id_seq', 6, true);


--
-- Name: jobparameter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.jobparameter_id_seq', 22, true);


--
-- Name: jobqueue_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.jobqueue_id_seq', 16, true);


--
-- Name: list_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.list_id_seq', 1, false);


--
-- Name: set_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.set_id_seq', 49, true);


--
-- Name: state_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.state_id_seq', 60, true);


--
-- Name: Districts_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."Districts_Id_seq"', 1, false);


--
-- Name: IndustrySectors_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."IndustrySectors_Id_seq"', 1, false);


--
-- Name: PostalCodes_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."PostalCodes_Id_seq"', 1, false);


--
-- Name: SectorComplianceRules_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."SectorComplianceRules_Id_seq"', 1, false);


--
-- Name: Villages_Id_seq; Type: SEQUENCE SET; Schema: master; Owner: postgres
--

SELECT pg_catalog.setval('master."Villages_Id_seq"', 1, false);


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
-- Name: ActivationResendLogs PK_ActivationResendLogs; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."ActivationResendLogs"
    ADD CONSTRAINT "PK_ActivationResendLogs" PRIMARY KEY ("Id");


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
-- Name: DetectedAnomalies PK_DetectedAnomalies; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."DetectedAnomalies"
    ADD CONSTRAINT "PK_DetectedAnomalies" PRIMARY KEY ("Id");


--
-- Name: Districts PK_Districts; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Districts"
    ADD CONSTRAINT "PK_Districts" PRIMARY KEY ("Id");


--
-- Name: FeatureFlags PK_FeatureFlags; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."FeatureFlags"
    ADD CONSTRAINT "PK_FeatureFlags" PRIMARY KEY ("Id");


--
-- Name: IndustrySectors PK_IndustrySectors; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."IndustrySectors"
    ADD CONSTRAINT "PK_IndustrySectors" PRIMARY KEY ("Id");


--
-- Name: LegalHolds PK_LegalHolds; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."LegalHolds"
    ADD CONSTRAINT "PK_LegalHolds" PRIMARY KEY ("Id");


--
-- Name: PostalCodes PK_PostalCodes; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."PostalCodes"
    ADD CONSTRAINT "PK_PostalCodes" PRIMARY KEY ("Id");


--
-- Name: RefreshTokens PK_RefreshTokens; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."RefreshTokens"
    ADD CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id");


--
-- Name: SectorComplianceRules PK_SectorComplianceRules; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SectorComplianceRules"
    ADD CONSTRAINT "PK_SectorComplianceRules" PRIMARY KEY ("Id");


--
-- Name: SecurityAlerts PK_SecurityAlerts; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SecurityAlerts"
    ADD CONSTRAINT "PK_SecurityAlerts" PRIMARY KEY ("Id");


--
-- Name: SubscriptionNotificationLogs PK_SubscriptionNotificationLogs; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SubscriptionNotificationLogs"
    ADD CONSTRAINT "PK_SubscriptionNotificationLogs" PRIMARY KEY ("Id");


--
-- Name: SubscriptionPayments PK_SubscriptionPayments; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SubscriptionPayments"
    ADD CONSTRAINT "PK_SubscriptionPayments" PRIMARY KEY ("Id");


--
-- Name: Tenants PK_Tenants; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Tenants"
    ADD CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id");


--
-- Name: Villages PK_Villages; Type: CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Villages"
    ADD CONSTRAINT "PK_Villages" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
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
-- Name: IX_ActivationResendLogs_IP_RequestedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_ActivationResendLogs_IP_RequestedAt" ON master."ActivationResendLogs" USING btree ("RequestedFromIp", "RequestedAt");


--
-- Name: IX_ActivationResendLogs_RequestedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_ActivationResendLogs_RequestedAt" ON master."ActivationResendLogs" USING btree ("RequestedAt");


--
-- Name: IX_ActivationResendLogs_RequestedFromIp; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_ActivationResendLogs_RequestedFromIp" ON master."ActivationResendLogs" USING btree ("RequestedFromIp");


--
-- Name: IX_ActivationResendLogs_Success; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_ActivationResendLogs_Success" ON master."ActivationResendLogs" USING btree ("Success");


--
-- Name: IX_ActivationResendLogs_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_ActivationResendLogs_TenantId" ON master."ActivationResendLogs" USING btree ("TenantId");


--
-- Name: IX_ActivationResendLogs_TenantId_RequestedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_ActivationResendLogs_TenantId_RequestedAt" ON master."ActivationResendLogs" USING btree ("TenantId", "RequestedAt");


--
-- Name: IX_AdminUsers_Email; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_AdminUsers_Email" ON master."AdminUsers" USING btree ("Email");


--
-- Name: IX_AdminUsers_UserName; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_AdminUsers_UserName" ON master."AdminUsers" USING btree ("UserName");


--
-- Name: IX_AuditLogs_ActionType; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_ActionType" ON master."AuditLogs" USING btree ("ActionType");


--
-- Name: IX_AuditLogs_Category; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_Category" ON master."AuditLogs" USING btree ("Category");


--
-- Name: IX_AuditLogs_Category_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_Category_PerformedAt" ON master."AuditLogs" USING btree ("Category", "PerformedAt");


--
-- Name: IX_AuditLogs_CorrelationId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_CorrelationId" ON master."AuditLogs" USING btree ("CorrelationId");


--
-- Name: IX_AuditLogs_EntityType_EntityId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_EntityType_EntityId" ON master."AuditLogs" USING btree ("EntityType", "EntityId");


--
-- Name: IX_AuditLogs_IsArchived_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_IsArchived_PerformedAt" ON master."AuditLogs" USING btree ("IsArchived", "PerformedAt");


--
-- Name: IX_AuditLogs_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_PerformedAt" ON master."AuditLogs" USING btree ("PerformedAt");


--
-- Name: IX_AuditLogs_SessionId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_SessionId" ON master."AuditLogs" USING btree ("SessionId");


--
-- Name: IX_AuditLogs_Severity; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_Severity" ON master."AuditLogs" USING btree ("Severity");


--
-- Name: IX_AuditLogs_Success_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_Success_PerformedAt" ON master."AuditLogs" USING btree ("Success", "PerformedAt");


--
-- Name: IX_AuditLogs_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_TenantId" ON master."AuditLogs" USING btree ("TenantId");


--
-- Name: IX_AuditLogs_TenantId_Category_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_TenantId_Category_PerformedAt" ON master."AuditLogs" USING btree ("TenantId", "Category", "PerformedAt");


--
-- Name: IX_AuditLogs_TenantId_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_TenantId_PerformedAt" ON master."AuditLogs" USING btree ("TenantId", "PerformedAt");


--
-- Name: IX_AuditLogs_UserId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_UserId" ON master."AuditLogs" USING btree ("UserId");


--
-- Name: IX_AuditLogs_UserId_PerformedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_AuditLogs_UserId_PerformedAt" ON master."AuditLogs" USING btree ("UserId", "PerformedAt");


--
-- Name: IX_DetectedAnomalies_AnomalyType; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_AnomalyType" ON master."DetectedAnomalies" USING btree ("AnomalyType");


--
-- Name: IX_DetectedAnomalies_DetectedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_DetectedAt" ON master."DetectedAnomalies" USING btree ("DetectedAt");


--
-- Name: IX_DetectedAnomalies_RiskLevel; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_RiskLevel" ON master."DetectedAnomalies" USING btree ("RiskLevel");


--
-- Name: IX_DetectedAnomalies_Status; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_Status" ON master."DetectedAnomalies" USING btree ("Status");


--
-- Name: IX_DetectedAnomalies_Status_DetectedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_Status_DetectedAt" ON master."DetectedAnomalies" USING btree ("Status", "DetectedAt");


--
-- Name: IX_DetectedAnomalies_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_TenantId" ON master."DetectedAnomalies" USING btree ("TenantId");


--
-- Name: IX_DetectedAnomalies_TenantId_DetectedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_DetectedAnomalies_TenantId_DetectedAt" ON master."DetectedAnomalies" USING btree ("TenantId", "DetectedAt");


--
-- Name: IX_Districts_DisplayOrder; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Districts_DisplayOrder" ON master."Districts" USING btree ("DisplayOrder");


--
-- Name: IX_Districts_DistrictCode; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Districts_DistrictCode" ON master."Districts" USING btree ("DistrictCode");


--
-- Name: IX_Districts_Region; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Districts_Region" ON master."Districts" USING btree ("Region");


--
-- Name: IX_FeatureFlags_IsEmergencyDisabled; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_FeatureFlags_IsEmergencyDisabled" ON master."FeatureFlags" USING btree ("IsEmergencyDisabled");


--
-- Name: IX_FeatureFlags_IsEnabled; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_FeatureFlags_IsEnabled" ON master."FeatureFlags" USING btree ("IsEnabled");


--
-- Name: IX_FeatureFlags_Module; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_FeatureFlags_Module" ON master."FeatureFlags" USING btree ("Module");


--
-- Name: IX_FeatureFlags_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_FeatureFlags_TenantId" ON master."FeatureFlags" USING btree ("TenantId");


--
-- Name: IX_FeatureFlags_TenantId_Module; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_FeatureFlags_TenantId_Module" ON master."FeatureFlags" USING btree ("TenantId", "Module");


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
-- Name: IX_LegalHolds_CaseNumber; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_LegalHolds_CaseNumber" ON master."LegalHolds" USING btree ("CaseNumber");


--
-- Name: IX_LegalHolds_CreatedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_LegalHolds_CreatedAt" ON master."LegalHolds" USING btree ("CreatedAt");


--
-- Name: IX_LegalHolds_Status; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_LegalHolds_Status" ON master."LegalHolds" USING btree ("Status");


--
-- Name: IX_LegalHolds_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_LegalHolds_TenantId" ON master."LegalHolds" USING btree ("TenantId");


--
-- Name: IX_PostalCodes_Code; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_PostalCodes_Code" ON master."PostalCodes" USING btree ("Code");


--
-- Name: IX_PostalCodes_DistrictId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_PostalCodes_DistrictId" ON master."PostalCodes" USING btree ("DistrictId");


--
-- Name: IX_PostalCodes_VillageId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_PostalCodes_VillageId" ON master."PostalCodes" USING btree ("VillageId");


--
-- Name: IX_PostalCodes_VillageName_DistrictName; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_PostalCodes_VillageName_DistrictName" ON master."PostalCodes" USING btree ("VillageName", "DistrictName");


--
-- Name: IX_RefreshTokens_AdminUserId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_AdminUserId" ON master."RefreshTokens" USING btree ("AdminUserId");


--
-- Name: IX_RefreshTokens_AdminUserId_ExpiresAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_AdminUserId_ExpiresAt" ON master."RefreshTokens" USING btree ("AdminUserId", "ExpiresAt");


--
-- Name: IX_RefreshTokens_ExpiresAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON master."RefreshTokens" USING btree ("ExpiresAt");


--
-- Name: IX_RefreshTokens_Token; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON master."RefreshTokens" USING btree ("Token");


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
-- Name: IX_SecurityAlerts_AlertType; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_AlertType" ON master."SecurityAlerts" USING btree ("AlertType");


--
-- Name: IX_SecurityAlerts_AuditLogId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_AuditLogId" ON master."SecurityAlerts" USING btree ("AuditLogId");


--
-- Name: IX_SecurityAlerts_CreatedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_CreatedAt" ON master."SecurityAlerts" USING btree ("CreatedAt");


--
-- Name: IX_SecurityAlerts_Severity; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_Severity" ON master."SecurityAlerts" USING btree ("Severity");


--
-- Name: IX_SecurityAlerts_Severity_Status_CreatedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_Severity_Status_CreatedAt" ON master."SecurityAlerts" USING btree ("Severity", "Status", "CreatedAt");


--
-- Name: IX_SecurityAlerts_Status; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_Status" ON master."SecurityAlerts" USING btree ("Status");


--
-- Name: IX_SecurityAlerts_Status_CreatedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_Status_CreatedAt" ON master."SecurityAlerts" USING btree ("Status", "CreatedAt");


--
-- Name: IX_SecurityAlerts_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_TenantId" ON master."SecurityAlerts" USING btree ("TenantId");


--
-- Name: IX_SecurityAlerts_TenantId_CreatedAt; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_TenantId_CreatedAt" ON master."SecurityAlerts" USING btree ("TenantId", "CreatedAt");


--
-- Name: IX_SecurityAlerts_UserId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SecurityAlerts_UserId" ON master."SecurityAlerts" USING btree ("UserId");


--
-- Name: IX_SubscriptionNotificationLogs_NotificationType; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_NotificationType" ON master."SubscriptionNotificationLogs" USING btree ("NotificationType");


--
-- Name: IX_SubscriptionNotificationLogs_RequiresFollowUp; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_RequiresFollowUp" ON master."SubscriptionNotificationLogs" USING btree ("RequiresFollowUp", "FollowUpCompletedDate");


--
-- Name: IX_SubscriptionNotificationLogs_SentDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_SentDate" ON master."SubscriptionNotificationLogs" USING btree ("SentDate");


--
-- Name: IX_SubscriptionNotificationLogs_SubscriptionPaymentId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_SubscriptionPaymentId" ON master."SubscriptionNotificationLogs" USING btree ("SubscriptionPaymentId");


--
-- Name: IX_SubscriptionNotificationLogs_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_TenantId" ON master."SubscriptionNotificationLogs" USING btree ("TenantId");


--
-- Name: IX_SubscriptionNotificationLogs_TenantId_SentDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_TenantId_SentDate" ON master."SubscriptionNotificationLogs" USING btree ("TenantId", "SentDate");


--
-- Name: IX_SubscriptionNotificationLogs_TenantId_Type_SentDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionNotificationLogs_TenantId_Type_SentDate" ON master."SubscriptionNotificationLogs" USING btree ("TenantId", "NotificationType", "SentDate");


--
-- Name: IX_SubscriptionPayments_DueDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionPayments_DueDate" ON master."SubscriptionPayments" USING btree ("DueDate");


--
-- Name: IX_SubscriptionPayments_PaidDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionPayments_PaidDate" ON master."SubscriptionPayments" USING btree ("PaidDate");


--
-- Name: IX_SubscriptionPayments_Status; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionPayments_Status" ON master."SubscriptionPayments" USING btree ("Status");


--
-- Name: IX_SubscriptionPayments_TenantId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionPayments_TenantId" ON master."SubscriptionPayments" USING btree ("TenantId");


--
-- Name: IX_SubscriptionPayments_TenantId_PeriodStartDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionPayments_TenantId_PeriodStartDate" ON master."SubscriptionPayments" USING btree ("TenantId", "PeriodStartDate");


--
-- Name: IX_SubscriptionPayments_TenantId_Status; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_SubscriptionPayments_TenantId_Status" ON master."SubscriptionPayments" USING btree ("TenantId", "Status");


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
-- Name: IX_Tenants_Status_SubscriptionEndDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Tenants_Status_SubscriptionEndDate" ON master."Tenants" USING btree ("Status", "SubscriptionEndDate");


--
-- Name: IX_Tenants_Subdomain; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Tenants_Subdomain" ON master."Tenants" USING btree ("Subdomain");


--
-- Name: IX_Tenants_SubscriptionEndDate; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Tenants_SubscriptionEndDate" ON master."Tenants" USING btree ("SubscriptionEndDate");


--
-- Name: IX_Villages_DisplayOrder; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Villages_DisplayOrder" ON master."Villages" USING btree ("DisplayOrder");


--
-- Name: IX_Villages_DistrictId; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Villages_DistrictId" ON master."Villages" USING btree ("DistrictId");


--
-- Name: IX_Villages_PostalCode; Type: INDEX; Schema: master; Owner: postgres
--

CREATE INDEX "IX_Villages_PostalCode" ON master."Villages" USING btree ("PostalCode");


--
-- Name: IX_Villages_VillageCode; Type: INDEX; Schema: master; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Villages_VillageCode" ON master."Villages" USING btree ("VillageCode");


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
-- Name: ActivationResendLogs FK_ActivationResendLogs_Tenants_TenantId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."ActivationResendLogs"
    ADD CONSTRAINT "FK_ActivationResendLogs_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES master."Tenants"("Id") ON DELETE CASCADE;


--
-- Name: FeatureFlags FK_FeatureFlags_Tenants_TenantId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."FeatureFlags"
    ADD CONSTRAINT "FK_FeatureFlags_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES master."Tenants"("Id") ON DELETE CASCADE;


--
-- Name: IndustrySectors FK_IndustrySectors_IndustrySectors_ParentSectorId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."IndustrySectors"
    ADD CONSTRAINT "FK_IndustrySectors_IndustrySectors_ParentSectorId" FOREIGN KEY ("ParentSectorId") REFERENCES master."IndustrySectors"("Id") ON DELETE RESTRICT;


--
-- Name: PostalCodes FK_PostalCodes_Districts_DistrictId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."PostalCodes"
    ADD CONSTRAINT "FK_PostalCodes_Districts_DistrictId" FOREIGN KEY ("DistrictId") REFERENCES master."Districts"("Id") ON DELETE RESTRICT;


--
-- Name: PostalCodes FK_PostalCodes_Villages_VillageId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."PostalCodes"
    ADD CONSTRAINT "FK_PostalCodes_Villages_VillageId" FOREIGN KEY ("VillageId") REFERENCES master."Villages"("Id") ON DELETE RESTRICT;


--
-- Name: RefreshTokens FK_RefreshTokens_AdminUsers_AdminUserId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_AdminUsers_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES master."AdminUsers"("Id") ON DELETE CASCADE;


--
-- Name: SectorComplianceRules FK_SectorComplianceRules_IndustrySectors_SectorId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SectorComplianceRules"
    ADD CONSTRAINT "FK_SectorComplianceRules_IndustrySectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES master."IndustrySectors"("Id") ON DELETE CASCADE;


--
-- Name: SubscriptionNotificationLogs FK_SubscriptionNotificationLogs_SubscriptionPayments_Subscript~; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SubscriptionNotificationLogs"
    ADD CONSTRAINT "FK_SubscriptionNotificationLogs_SubscriptionPayments_Subscript~" FOREIGN KEY ("SubscriptionPaymentId") REFERENCES master."SubscriptionPayments"("Id") ON DELETE RESTRICT;


--
-- Name: SubscriptionNotificationLogs FK_SubscriptionNotificationLogs_Tenants_TenantId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SubscriptionNotificationLogs"
    ADD CONSTRAINT "FK_SubscriptionNotificationLogs_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES master."Tenants"("Id") ON DELETE RESTRICT;


--
-- Name: SubscriptionPayments FK_SubscriptionPayments_Tenants_TenantId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."SubscriptionPayments"
    ADD CONSTRAINT "FK_SubscriptionPayments_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES master."Tenants"("Id") ON DELETE RESTRICT;


--
-- Name: Tenants FK_Tenants_IndustrySectors_SectorId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Tenants"
    ADD CONSTRAINT "FK_Tenants_IndustrySectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES master."IndustrySectors"("Id") ON DELETE RESTRICT;


--
-- Name: Villages FK_Villages_Districts_DistrictId; Type: FK CONSTRAINT; Schema: master; Owner: postgres
--

ALTER TABLE ONLY master."Villages"
    ADD CONSTRAINT "FK_Villages_Districts_DistrictId" FOREIGN KEY ("DistrictId") REFERENCES master."Districts"("Id") ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--

\unrestrict h4rsubtdJPb8FHPBWH7Ty7ni7n5gy8rLlsFwwHklg4NZEVdLaJE1Ogvx1BH9oJx

