# SQL Injection Fix - Fortune 500 Implementation Plan

**Date:** November 19, 2025
**Priority:** P0 - CRITICAL SECURITY FIX
**Target:** Production-grade security hardening

---

## Executive Summary

This plan addresses the critical SQL injection vulnerabilities discovered in the HRMS application with a comprehensive, multi-layered Fortune 500-grade approach.

**Vulnerabilities to Fix:**
1. **CRITICAL:** DeviceWebhookService.cs:374 - Direct string interpolation
2. **HIGH:** TenantAuthService.cs:445 - FromSqlRaw usage

**Implementation Approach:**
- Defense-in-depth security strategy
- Parameterized queries (primary fix)
- Input validation (secondary defense)
- Automated testing (verification)
- Static analysis (prevention)
- Documentation (knowledge transfer)

---

## Phase 1: Immediate Fixes (P0 - CRITICAL)

### Fix 1.1: DeviceWebhookService.cs - Critical SQL Injection

**Current Vulnerable Code (Line 373-374):**
```csharp
var schema = await masterDbContext.Database.SqlQueryRaw<string>(
    $"SELECT \"SchemaName\" FROM master.\"Tenants\" WHERE \"Id\" = '{tenantId}' AND \"IsDeleted\" = false")
    .FirstOrDefaultAsync();
```

**Security Fix - Parameterized Query:**
```csharp
var schema = await masterDbContext.Database.SqlQueryRaw<string>(
    @"SELECT ""SchemaName"" FROM master.""Tenants""
      WHERE ""Id"" = {0} AND ""IsDeleted"" = false",
    tenantId)
    .FirstOrDefaultAsync();
```

**Why This Works:**
- EF Core automatically creates NpgsqlParameter for {0}
- Parameter binding prevents SQL injection
- Database treats tenantId as data, not SQL code
- Attack payload `"1' OR '1'='1' --"` becomes literal string value

**Alternative: Use LINQ (Safer):**
```csharp
var schema = await masterDbContext.Tenants
    .Where(t => t.Id == tenantId && !t.IsDeleted)
    .Select(t => t.SchemaName)
    .FirstOrDefaultAsync();
```

**Recommendation:** Use LINQ approach - completely eliminates SQL injection risk

---

### Fix 1.2: TenantAuthService.cs - FromSqlRaw Improvement

**Current Code (Line 445-451):**
```csharp
var token = await _masterContext.RefreshTokens
    .FromSqlRaw(@"
        SELECT * FROM ""RefreshTokens""
        WHERE ""Token"" = {0}
        AND ""TenantId"" IS NOT NULL
        AND ""EmployeeId"" IS NOT NULL
        FOR UPDATE
    ", refreshToken)
    .FirstOrDefaultAsync();
```

**Security Improvement - FromSqlInterpolated:**
```csharp
var token = await _masterContext.RefreshTokens
    .FromSqlInterpolated($@"
        SELECT * FROM ""RefreshTokens""
        WHERE ""Token"" = {refreshToken}
        AND ""TenantId"" IS NOT NULL
        AND ""EmployeeId"" IS NOT NULL
        FOR UPDATE
    ")
    .FirstOrDefaultAsync();
```

**Why This Is Better:**
- Compile-time safety (C# interpolation)
- Explicit parameter binding
- More readable and maintainable
- Same security as {0} but clearer intent

---

## Phase 2: Input Validation (Defense-in-Depth)

### Add Input Validation Layer

**Implementation: TenantId Validator**
```csharp
// File: src/HRMS.Core/Validators/TenantIdValidator.cs
public static class TenantIdValidator
{
    // Only allow valid GUID format
    public static bool IsValidTenantId(string tenantId)
    {
        return Guid.TryParse(tenantId, out _);
    }

    // Throw if invalid (fail-fast)
    public static void ValidateOrThrow(string tenantId, string paramName)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(paramName);

        if (!Guid.TryParse(tenantId, out _))
            throw new ArgumentException($"Invalid tenant ID format: {paramName}", paramName);
    }
}
```

**Usage in DeviceWebhookService:**
```csharp
// Validate before any database query
TenantIdValidator.ValidateOrThrow(tenantId, nameof(tenantId));

// Now safe to use in query
var schema = await masterDbContext.Tenants
    .Where(t => t.Id == Guid.Parse(tenantId) && !t.IsDeleted)
    .Select(t => t.SchemaName)
    .FirstOrDefaultAsync();
```

---

## Phase 3: Automated Testing (Verification)

### Unit Tests for SQL Injection Prevention

**File: tests/HRMS.Tests/Security/SqlInjectionTests.cs**

```csharp
public class SqlInjectionTests
{
    [Theory]
    [InlineData("1' OR '1'='1' --")]
    [InlineData("1'; DROP TABLE Tenants; --")]
    [InlineData("1' UNION SELECT * FROM Users --")]
    [InlineData("'; DELETE FROM RefreshTokens; --")]
    public async Task DeviceWebhookService_ShouldRejectSqlInjectionPayloads(string maliciousInput)
    {
        // Arrange
        var service = CreateDeviceWebhookService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.GetTenantSchema(maliciousInput)
        );
    }

    [Fact]
    public async Task DeviceWebhookService_ShouldOnlyAcceptValidGuids()
    {
        // Arrange
        var service = CreateDeviceWebhookService();
        var validTenantId = Guid.NewGuid().ToString();

        // Act - Should not throw
        var result = await service.GetTenantSchema(validTenantId);

        // Assert
        Assert.NotNull(result);
    }
}
```

---

## Phase 4: Static Analysis (Prevention)

### Add Roslyn Analyzer Rules

**File: .editorconfig**

```ini
# SQL Injection Prevention Rules
dotnet_diagnostic.CA2100.severity = error  # Review SQL queries for security vulnerabilities
dotnet_diagnostic.EF1001.severity = warning # Raw SQL usage warning

# Custom rule: Ban string concatenation in SQL
# (Requires custom analyzer)
```

**File: Directory.Build.props**

```xml
<ItemGroup>
    <!-- Security Analysis -->
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>

    <!-- SQL Injection Detection -->
    <PackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

---

## Phase 5: Code Review Checklist

### Pre-Deployment Security Checklist

**File: docs/security/SQL_INJECTION_PREVENTION_CHECKLIST.md**

```markdown
# SQL Injection Prevention Checklist

Before merging any code that touches database queries:

## Required Checks

- [ ] No raw SQL string concatenation or interpolation
- [ ] All raw SQL uses parameterized queries
- [ ] Prefer LINQ over raw SQL whenever possible
- [ ] Input validation for all user-provided parameters
- [ ] Unit tests with SQL injection payloads
- [ ] Static analysis passes (CA2100)
- [ ] Code review by security-aware developer

## Raw SQL Patterns (BANNED)

❌ NEVER USE:
```csharp
$"SELECT * FROM Table WHERE Id = '{userId}'"  // String interpolation
"SELECT * FROM Table WHERE Id = '" + userId + "'"  // Concatenation
```

✅ ALWAYS USE:
```csharp
.FromSqlInterpolated($"SELECT * FROM Table WHERE Id = {userId}")
.SqlQueryRaw("SELECT * FROM Table WHERE Id = {0}", userId)
.Where(x => x.Id == userId)  // LINQ (preferred)
```
```

---

## Phase 6: Audit Trail

### Log Security Fix

**Action: Create audit log entry for this security fix**

```csharp
await _auditLogService.LogAsync(new AuditLog
{
    ActionType = AuditActionType.SECURITY_VULNERABILITY_FIXED,
    Category = AuditCategory.SECURITY_EVENT,
    Severity = AuditSeverity.CRITICAL,
    EntityType = "DeviceWebhookService",
    NewValues = "Replaced direct SQL interpolation with parameterized query",
    Description = "Fixed critical SQL injection vulnerability (CVSS 9.8) in tenant schema lookup",
    PerformedBy = "Security Team",
    IpAddress = "Internal",
    Metadata = new Dictionary<string, object>
    {
        ["VulnerabilityType"] = "SQL Injection",
        ["CVSS"] = "9.8",
        ["File"] = "DeviceWebhookService.cs",
        ["Line"] = 374
    }
});
```

---

## Implementation Steps

### Step-by-Step Execution Plan

**Day 1 (Immediate):**
1. ✅ Create implementation plan (this document)
2. ⏳ Apply Fix 1.1 - DeviceWebhookService.cs
3. ⏳ Apply Fix 1.2 - TenantAuthService.cs
4. ⏳ Add TenantIdValidator
5. ⏳ Write unit tests with SQL injection payloads
6. ⏳ Run full test suite
7. ⏳ Security review
8. ⏳ Deploy to staging

**Day 2 (Validation):**
1. Run penetration testing on staging
2. Verify SQL injection payloads are rejected
3. Monitor audit logs for suspicious attempts
4. Performance testing (ensure no regression)

**Day 3 (Production):**
1. Deploy to production via hotfix process
2. Monitor for 24 hours
3. Create audit log entry
4. Update security documentation

**Week 1 (Prevention):**
1. Add static analysis rules
2. Add pre-commit hooks
3. Create security training materials
4. Update code review guidelines

---

## Success Criteria

### How We Know It's Fixed

**Primary Verification:**
- [ ] SQL injection payloads in unit tests are rejected
- [ ] No string concatenation/interpolation in SQL queries
- [ ] All parameters properly bound
- [ ] Static analysis passes (CA2100)

**Secondary Verification:**
- [ ] Penetration testing with OWASP ZAP passes
- [ ] Security audit confirms parameterization
- [ ] Code review by 2+ senior developers
- [ ] Audit logs show security fix applied

**Compliance Verification:**
- [ ] GDPR Article 32 compliance restored
- [ ] SOC 2 CC5/CC6 controls met
- [ ] ISO 27001 A.8 controls met
- [ ] PCI DSS Requirement 6.5.1 met

---

## Risk Mitigation

### Rollback Plan

If issues occur in production:

1. **Immediate:** Revert to previous version
2. **Short-term:** Fix in hotfix branch
3. **Long-term:** Enhanced testing before retry

### Testing Strategy

- Unit tests with malicious payloads
- Integration tests with actual database
- Penetration testing with OWASP tools
- Load testing (ensure no performance impact)

---

## Fortune 500 Best Practices Applied

✅ **Defense-in-Depth:** Multiple security layers (parameterization + validation)
✅ **Fail-Safe Defaults:** LINQ preferred over raw SQL
✅ **Least Privilege:** Input validation before processing
✅ **Complete Mediation:** Every input validated
✅ **Audit Trail:** Security fix logged in audit system
✅ **Automated Prevention:** Static analysis rules
✅ **Knowledge Transfer:** Documentation and training

---

## Estimated Timeline

- **Immediate Fixes:** 2-4 hours
- **Testing:** 4-6 hours
- **Deployment:** 2 hours
- **Monitoring:** 24 hours
- **Prevention Setup:** 1 week

**Total to Production:** 2-3 days
**Total Prevention Infrastructure:** 1 week

---

This plan ensures not only fixing the current vulnerabilities but preventing future SQL injection issues through automation, education, and architectural best practices.
