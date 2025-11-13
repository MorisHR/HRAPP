# Fortune 500 Grade Error Handling System

## 🎯 Overview

This document describes the comprehensive error handling system implemented across the HRMS application. The system follows Fortune 500 best practices for error handling, providing user-friendly messages, actionable guidance, and trackable error codes.

## 📋 Table of Contents

1. [Backend Error Handling](#backend-error-handling)
2. [Frontend Error Handling](#frontend-error-handling)
3. [Error Codes Reference](#error-codes-reference)
4. [Usage Examples](#usage-examples)
5. [Migration Guide](#migration-guide)

---

## Backend Error Handling

### Custom Exception Hierarchy

Located in `src/HRMS.Core/Exceptions/`:

```
HRMSException (Base)
├── ValidationException (HTTP 400)
├── NotFoundException (HTTP 404)
├── ConflictException (HTTP 409)
├── UnauthorizedException (HTTP 401)
├── ForbiddenException (HTTP 403)
└── BusinessRuleException (HTTP 422)
```

### Exception Properties

Each custom exception includes:
- **ErrorCode**: Trackable code (e.g., "AUTH_001", "EMP_404")
- **UserMessage**: Safe, user-friendly message
- **TechnicalDetails**: Internal details for logging (not shown to users)
- **SuggestedAction**: Actionable guidance for users

### Example Usage

```csharp
// ❌ OLD WAY - Technical, unfriendly
throw new Exception($"Employee not found: {employeeId}");

// ✅ NEW WAY - User-friendly, actionable
throw new NotFoundException(
    ErrorCodes.EMP_NOT_FOUND,
    "Employee information could not be found.",
    $"Employee ID {employeeId} not found in database",
    "Verify the employee selection and try again.");
```

### Global Exception Middleware

The `GlobalExceptionHandlingMiddleware` automatically:
- Catches all unhandled exceptions
- Maps them to appropriate HTTP status codes
- Transforms technical messages to user-friendly ones
- Adds correlation IDs for tracking
- Includes support contact information
- Hides sensitive details in production

---

## Frontend Error Handling

### ErrorHandlerService

Located in `src/app/core/services/error-handler.service.ts`

**Key Features:**
- Transforms backend API errors to user-friendly format
- Provides context-aware error messages
- Generates correlation IDs for tracking
- Maps HTTP status codes to helpful messages
- Determines when to show support contact

### Usage

```typescript
import { ErrorHandlerService, UserError } from './core/services/error-handler.service';

export class MyComponent {
  errorMessage: UserError | null = null;

  constructor(private errorHandler: ErrorHandlerService) {}

  async loadData() {
    try {
      // API call
    } catch (error) {
      this.errorMessage = this.errorHandler.handleError(error, 'employee data');
      this.errorHandler.logError(error, 'MyComponent.loadData');
    }
  }
}
```

### ErrorMessageComponent

Reusable component for displaying errors:

```html
<app-error-message 
  [error]="errorMessage" 
  [dismissible]="true">
</app-error-message>
```

---

## Error Codes Reference

### Authentication (AUTH_xxx)
| Code | Description | HTTP Status |
|------|-------------|-------------|
| AUTH_001 | Invalid credentials | 401 |
| AUTH_002 | Account locked | 401 |
| AUTH_003 | Account suspended | 401 |
| AUTH_004 | Session expired | 401 |
| AUTH_005 | Invalid token | 401 |
| AUTH_006 | MFA required | 401 |
| AUTH_007 | Invalid MFA code | 401 |
| AUTH_008 | Password expired | 401 |
| AUTH_009 | Insufficient permissions | 403 |
| AUTH_010 | Tenant not found | 404 |
| AUTH_011 | Tenant inactive | 403 |

### Employee Management (EMP_xxx)
| Code | Description | HTTP Status |
|------|-------------|-------------|
| EMP_001 | Employee not found | 404 |
| EMP_002 | Duplicate email | 409 |
| EMP_003 | Duplicate employee code | 409 |
| EMP_004 | Invalid employee data | 400 |
| EMP_005 | Cannot delete (has records) | 409 |
| EMP_006 | Draft not found | 404 |
| EMP_007 | Invalid draft data | 400 |

### Attendance (ATT_xxx)
| Code | Description | HTTP Status |
|------|-------------|-------------|
| ATT_001 | Attendance not found | 404 |
| ATT_002 | Duplicate attendance record | 409 |
| ATT_003 | Invalid time | 400 |
| ATT_004 | Unauthorized access | 403 |
| ATT_005 | Pending correction request | 409 |
| ATT_006 | Correction not found | 404 |
| ATT_007 | Already checked in | 409 |
| ATT_008 | Already checked out | 409 |

### Payroll (PAY_xxx)
| Code | Description | HTTP Status |
|------|-------------|-------------|
| PAY_001 | Payroll cycle not found | 404 |
| PAY_002 | Payslip not found | 404 |
| PAY_003 | Component not found | 404 |
| PAY_004 | Already processed | 409 |
| PAY_005 | Calculation error | 500 |

### System (SYS_xxx)
| Code | Description | HTTP Status |
|------|-------------|-------------|
| SYS_001 | Database error | 500 |
| SYS_002 | External service error | 502 |
| SYS_003 | Configuration error | 500 |
| SYS_999 | Unexpected error | 500 |

---

## Usage Examples

### Backend Service Example

```csharp
public async Task<Employee> GetEmployeeAsync(Guid id)
{
    var employee = await _context.Employees.FindAsync(id);
    
    if (employee == null)
    {
        throw new NotFoundException(
            ErrorCodes.EMP_NOT_FOUND,
            "The employee you're looking for could not be found.",
            $"Employee ID {id} not found",
            "Please verify the employee ID or contact support.");
    }
    
    return employee;
}

public async Task CreateAttendanceAsync(AttendanceDto dto)
{
    var existing = await _context.Attendances
        .Where(a => a.EmployeeId == dto.EmployeeId && a.Date == dto.Date)
        .FirstOrDefaultAsync();
    
    if (existing != null)
    {
        throw new ConflictException(
            ErrorCodes.ATT_DUPLICATE_RECORD,
            "Attendance has already been recorded for this date.",
            $"Duplicate record for employee {dto.EmployeeId} on {dto.Date:yyyy-MM-dd}",
            "To make changes, edit the existing record or contact HR.");
    }
    
    // Create attendance...
}
```

### Frontend Component Example

```typescript
export class EmployeeListComponent {
  employees: Employee[] = [];
  error: UserError | null = null;
  loading = false;

  constructor(
    private employeeService: EmployeeService,
    private errorHandler: ErrorHandlerService
  ) {}

  async loadEmployees() {
    this.loading = true;
    this.error = null;

    try {
      this.employees = await this.employeeService.getAll().toPromise();
    } catch (error) {
      this.error = this.errorHandler.handleError(error, 'employee list');
      this.errorHandler.logError(error, 'EmployeeListComponent.loadEmployees');
    } finally {
      this.loading = false;
    }
  }
}
```

```html
<app-error-message [error]="error" [dismissible]="true"></app-error-message>

<div *ngIf="loading">Loading...</div>
<div *ngIf="!loading && !error">
  <!-- Employee list -->
</div>
```

---

## Migration Guide

### For Backend Developers

**Step 1**: Replace generic exceptions with custom ones

```csharp
// Before
throw new Exception($"Employee {id} not found");

// After
throw new NotFoundException(
    ErrorCodes.EMP_NOT_FOUND,
    "Employee information could not be found.",
    $"Employee ID {id} not found in database",
    "Verify the employee selection and try again.");
```

**Step 2**: Use appropriate exception types

- `ValidationException` - Bad user input (HTTP 400)
- `NotFoundException` - Resource doesn't exist (HTTP 404)
- `ConflictException` - Duplicate/conflicting data (HTTP 409)
- `UnauthorizedException` - Not authenticated (HTTP 401)
- `ForbiddenException` - No permission (HTTP 403)
- `BusinessRuleException` - Business logic violation (HTTP 422)

**Step 3**: Add error codes from `ErrorCodes` class

### For Frontend Developers

**Step 1**: Inject ErrorHandlerService

```typescript
constructor(private errorHandler: ErrorHandlerService) {}
```

**Step 2**: Wrap API calls in try-catch

```typescript
try {
  const result = await this.apiService.someCall().toPromise();
} catch (error) {
  this.error = this.errorHandler.handleError(error, 'operation name');
  this.errorHandler.logError(error, 'Component.method');
}
```

**Step 3**: Display errors using ErrorMessageComponent

```html
<app-error-message [error]="error" [dismissible]="true"></app-error-message>
```

---

## Benefits

### ✅ User Experience
- Clear, non-technical error messages
- Actionable guidance for resolution
- Consistent error presentation
- Professional error handling

### ✅ Support & Debugging
- Trackable correlation IDs
- Proper error logging
- Technical details for debugging
- Error code categorization

### ✅ Security
- No exposure of internal IDs
- No stack traces in production
- No sensitive data in error messages
- Proper HTTP status codes

### ✅ Maintainability
- Centralized error handling
- Consistent error format
- Easy to extend
- Type-safe error codes

---

## Testing

### Backend Error Testing

```csharp
[Fact]
public async Task GetEmployee_NotFound_ThrowsNotFoundException()
{
    // Arrange
    var invalidId = Guid.NewGuid();
    
    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(
        () => _service.GetEmployeeAsync(invalidId));
    
    Assert.Equal(ErrorCodes.EMP_NOT_FOUND, exception.ErrorCode);
    Assert.Contains("could not be found", exception.UserMessage);
}
```

### Frontend Error Testing

```typescript
it('should display user-friendly error on API failure', () => {
  const error = new HttpErrorResponse({ status: 404 });
  component.handleError(error);
  
  expect(component.error).toBeTruthy();
  expect(component.error?.title).toBe('Not Found');
  expect(component.error?.correlationId).toBeTruthy();
});
```

---

## Future Enhancements

1. **Error Analytics Dashboard**
   - Track error frequency
   - Identify patterns
   - Monitor resolution rates

2. **Internationalization (i18n)**
   - Multi-language error messages
   - Localized guidance

3. **User Feedback Integration**
   - "Was this helpful?" button
   - Error report submission

4. **Automated Error Resolution**
   - Self-service recovery flows
   - Suggested fixes based on error type

---

## Support

For questions or issues with the error handling system:
- **Email**: support@morishr.com
- **Documentation**: This file
- **Code Examples**: See implementation in AuthService and AttendanceService

