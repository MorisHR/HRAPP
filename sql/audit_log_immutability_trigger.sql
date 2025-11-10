-- ============================================
-- AUDIT LOG IMMUTABILITY TRIGGER
-- Purpose: Enforce immutability at database level for compliance
-- Prevents any UPDATE or DELETE operations on audit logs
-- ============================================

-- Function to prevent audit log modifications
CREATE OR REPLACE FUNCTION master.prevent_audit_log_modification()
RETURNS TRIGGER AS $$
BEGIN
    -- Block UPDATE operations
    IF TG_OP = 'UPDATE' THEN
        RAISE EXCEPTION 'AUDIT_LOG_IMMUTABLE: Audit logs are immutable and cannot be modified. Log ID: %', OLD."Id"
            USING ERRCODE = '23502',
                  HINT = 'Audit logs must remain unchanged for compliance. Create a new audit log entry instead.';
    END IF;

    -- Block DELETE operations
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'AUDIT_LOG_IMMUTABLE: Audit logs are immutable and cannot be deleted. Log ID: %', OLD."Id"
            USING ERRCODE = '23502',
                  HINT = 'Audit logs must be retained for 10+ years for compliance. Use archival instead.';
    END IF;

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create trigger on AuditLogs table
DROP TRIGGER IF EXISTS audit_log_immutability_trigger ON master."AuditLogs";

CREATE TRIGGER audit_log_immutability_trigger
BEFORE UPDATE OR DELETE ON master."AuditLogs"
FOR EACH ROW
EXECUTE FUNCTION master.prevent_audit_log_modification();

-- Add comment explaining the trigger
COMMENT ON TRIGGER audit_log_immutability_trigger ON master."AuditLogs"
IS 'Enforces immutability of audit logs for compliance. Blocks all UPDATE and DELETE operations.';

-- Create audit log for this security enhancement
DO $$
BEGIN
    INSERT INTO master."AuditLogs" (
        "Id", "ActionType", "Category", "Severity", "EntityType",
        "Success", "PerformedAt", "UserEmail", "Reason",
        "AdditionalMetadata"
    ) VALUES (
        gen_random_uuid(),
        20, -- SYSTEM_CONFIGURATION_CHANGED
        6,  -- SYSTEM_ADMINISTRATION
        1,  -- INFO
        'AuditLog',
        true,
        NOW(),
        'system@hrms.com',
        'Audit log immutability trigger installed for compliance',
        '{"trigger": "prevent_audit_log_modification", "operations": ["UPDATE", "DELETE"], "compliance": "Fortune 500"}'
    );
END $$;

COMMENT ON FUNCTION master.prevent_audit_log_modification()
IS 'Prevents modification or deletion of audit logs to maintain compliance with Fortune 500 audit requirements';

-- Verify trigger is installed
SELECT
    trigger_name,
    event_manipulation,
    event_object_table,
    action_timing,
    action_statement
FROM information_schema.triggers
WHERE trigger_name = 'audit_log_immutability_trigger';
