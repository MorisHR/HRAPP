# SQL Injection Prevention Checklist

**Purpose:** Ensure all database code follows Fortune 500-grade security practices
**Created:** November 19, 2025
**Status:** MANDATORY for all code reviews

---

## Pre-Merge Checklist

Before merging any code that interacts with the database, verify:

### Required Security Checks

- [ ] **No raw SQL string concatenation or interpolation**
- [ ] **All raw SQL uses parameterized queries**
- [ ] **Prefer LINQ over raw SQL whenever possible**
- [ ] **Input validation for all user-provided parameters**
- [ ] **Unit tests with SQL injection payloads**
- [ ] **Static analysis passes (CA2100)**
- [ ] **Code review by security-aware developer**
- [ ] **No hardcoded credentials in queries**

---

## Banned SQL Patterns

### ❌ NEVER USE

```csharp
// 1. String interpolation in SQL
$"SELECT * FROM Table WHERE Id = '{userId}'"

// 2. String concatenation in SQL
"SELECT * FROM Table WHERE Id = '" + userId + "'"

// 3. Direct ExecuteSqlRaw with string interpolation
context.Database.ExecuteSqlRaw($"DELETE FROM Table WHERE Id = '{id}'")

// 4. SqlQueryRaw with string interpolation
context.Database.SqlQueryRaw<string>($"SELECT Name WHERE Id = '{id}'")

// 5. Unvalidated user input in queries
var query = $"SELECT * FROM Users WHERE Username = '{userInput}'";
```

---

## Approved SQL Patterns

### ✅ ALWAYS USE

```csharp
// 1. LINQ (PREFERRED - No SQL injection possible)
var result = await context.Users
    .Where(u => u.Id == userId)
    .FirstOrDefaultAsync();

// 2. FromSqlInterpolated (Compile-time safety)
var users = await context.Users
    .FromSqlInterpolated($"SELECT * FROM Users WHERE Id = {userId}")
    .ToListAsync();

// 3. SqlQueryRaw with positional parameters
var name = await context.Database.SqlQueryRaw<string>(
    "SELECT Name FROM Users WHERE Id = {0}",
    userId)
    .FirstOrDefaultAsync();

// 4. ExecuteSqlRawAsync with NpgsqlParameter
await context.Database.ExecuteSqlRawAsync(
    "UPDATE Users SET Name = @name WHERE Id = @id",
    new NpgsqlParameter("@name", newName),
    new NpgsqlParameter("@id", userId)
);

// 5. FromSqlRaw with positional parameters (when SELECT FOR UPDATE needed)
var token = await context.RefreshTokens
    .FromSqlRaw(@"
        SELECT * FROM ""RefreshTokens""
        WHERE ""Token"" = {0}
        FOR UPDATE
    ", refreshToken)
    .FirstOrDefaultAsync();
```

---

## Input Validation

### Always validate user inputs BEFORE using in queries

```csharp
using HRMS.Core.Validators;

// Validate GUID format
SecurityValidators.ValidateTenantId(tenantId);

// Validate refresh token
SecurityValidators.ValidateRefreshToken(refreshToken);

// Validate API key
SecurityValidators.ValidateApiKey(apiKey);
```

### Validation Benefits

1. **Defense-in-depth** - Multiple security layers
2. **Fail-fast** - Reject invalid input immediately
3. **Clear errors** - Better user experience
4. **Audit trail** - Log suspicious inputs

---

## Unit Testing

### Required Tests

Every service with database queries must have tests for:

```csharp
[Theory]
[InlineData("1' OR '1'='1' --")]
[InlineData("1'; DROP TABLE Users; --")]
[InlineData("1' UNION SELECT * --")]
public void ShouldRejectSqlInjectionPayloads(string maliciousInput)
{
    // Verify that SQL injection attempts are rejected
    Assert.Throws<ArgumentException>(
        () => service.QueryMethod(maliciousInput)
    );
}
```

---

## Static Analysis

### Required Tools

Install and configure:

```xml
<!-- In Directory.Build.props -->
<ItemGroup>
    <!-- CA2100: Review SQL queries for security vulnerabilities -->
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" />

    <!-- Advanced security scanning -->
    <PackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7" />
</ItemGroup>
```

### Required Rules

```ini
# In .editorconfig
[*.cs]
dotnet_diagnostic.CA2100.severity = error
dotnet_diagnostic.EF1001.severity = warning
```

---

## Code Review Guidelines

### Security Reviewer Checklist

When reviewing database code:

1. **Identify all SQL queries** - Use search: `SqlQueryRaw`, `ExecuteSqlRaw`, `FromSqlRaw`
2. **Check for string interpolation** - Look for `$"` in SQL
3. **Verify parameterization** - Ensure `{0}` or `{parameter}` used
4. **Validate input handling** - Check for `SecurityValidators` usage
5. **Review tests** - Confirm SQL injection tests exist
6. **Check LINQ alternative** - Can this be LINQ instead?

---

## Real-World Attack Examples

### Example 1: Classic OR Bypass

**Attack Input:**
```
userId = "1' OR '1'='1' --"
```

**Vulnerable Query:**
```sql
SELECT * FROM Users WHERE Id = '1' OR '1'='1' --'
```

**Result:** Returns ALL users (authentication bypass)

**Protection:** Parameterized query treats entire string as literal value

---

### Example 2: Union-Based Data Exfiltration

**Attack Input:**
```
userId = "1' UNION SELECT password FROM AdminUsers --"
```

**Vulnerable Query:**
```sql
SELECT Name FROM Users WHERE Id = '1' UNION SELECT password FROM AdminUsers --'
```

**Result:** Exposes admin passwords

**Protection:** Input validation rejects non-GUID format

---

### Example 3: Multi-Tenant Breach

**Attack Input:**
```
tenantId = "1' OR '1'='1' --"
```

**Vulnerable Query:**
```sql
SELECT SchemaName FROM Tenants WHERE Id = '1' OR '1'='1' --'
```

**Result:** Returns ALL tenant schemas (complete breach)

**Protection:** GUID validation + parameterized queries

---

## Emergency Response

### If SQL Injection Discovered

1. **Immediate:**
   - Create P0 ticket
   - Assess impact (data breach?)
   - Notify security team

2. **Short-term (24 hours):**
   - Apply parameterized query fix
   - Add input validation
   - Write unit test
   - Deploy hotfix

3. **Medium-term (1 week):**
   - Audit entire codebase for similar issues
   - Add static analysis rules
   - Security training for team

4. **Long-term (1 month):**
   - Implement pre-commit hooks
   - Enhanced monitoring
   - Penetration testing

---

## Compliance Requirements

### Standards Met by Following This Checklist

- ✅ **GDPR Article 32** - Technical measures to ensure security
- ✅ **SOC 2 Type II** - CC5/CC6 control activities
- ✅ **ISO 27001** - A.8 technological controls
- ✅ **PCI DSS** - Requirement 6.5.1 (SQL injection prevention)
- ✅ **OWASP Top 10** - A03:2021 Injection prevention

---

## Resources

### Training Materials

- OWASP SQL Injection Prevention Cheat Sheet
- EF Core Documentation: Raw SQL Queries
- PostgreSQL Security Best Practices

### Internal Documentation

- `/docs/security/SECURITY_GAPS_VERIFICATION_REPORT.md`
- `/SQL_INJECTION_FIX_IMPLEMENTATION_PLAN.md`
- `/src/HRMS.Core/Validators/SecurityValidators.cs`

### Testing

- `/tests/HRMS.Tests/Security/SqlInjectionPreventionTests.cs`

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| Nov 19, 2025 | 1.0 | Initial creation - SQL injection prevention initiative |

---

**Remember:** SQL injection is the #1 web application security risk. Prevention is mandatory, not optional!
