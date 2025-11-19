# Automated Testing Strategy - Fortune 500 Grade
## HRMS Application - Comprehensive Testing Framework

**Version:** 1.0.0
**Last Updated:** 2025-11-17
**Owner:** QA Engineering Team

---

## Overview

This document defines a comprehensive automated testing strategy for the HRMS application, covering all layers of the testing pyramid: unit tests, integration tests, E2E tests, performance tests, security tests, and accessibility tests.

---

## Testing Pyramid

```
                    ▲
                   /|\
                  / | \
                 /  |  \
                /   |   \
               /  E2E    \          10% - E2E Tests
              /   Tests   \
             /             \
            /---------------\
           /                 \
          /    Integration    \     20% - Integration Tests
         /       Tests         \
        /                       \
       /-------------------------\
      /                           \
     /        Unit Tests           \   70% - Unit Tests
    /                               \
   /_________________________________\
```

**Distribution:**
- **Unit Tests:** 70% - Fast, isolated, focused
- **Integration Tests:** 20% - Component integration
- **E2E Tests:** 10% - Critical user journeys

---

## 1. Unit Testing

### Frontend Unit Testing (Angular)

**Framework:** Jasmine + Karma

**What to Test:**
- Component logic
- Service methods
- Pipe transformations
- Directive behavior
- Guard logic
- Utility functions

**Test Structure:**
```typescript
// Example: auth.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('login', () => {
    it('should return auth response with token on successful login', (done) => {
      const mockCredentials = { email: 'test@test.com', password: 'password' };
      const mockResponse = { token: 'jwt-token', user: { id: 1, email: 'test@test.com' } };

      service.login(mockCredentials).subscribe(response => {
        expect(response.token).toBe('jwt-token');
        expect(response.user.email).toBe('test@test.com');
        done();
      });

      const req = httpMock.expectOne('/api/auth/login');
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });

    it('should throw error on invalid credentials', (done) => {
      const mockCredentials = { email: 'test@test.com', password: 'wrong' };

      service.login(mockCredentials).subscribe(
        () => fail('should have failed'),
        (error) => {
          expect(error.status).toBe(401);
          done();
        }
      );

      const req = httpMock.expectOne('/api/auth/login');
      req.flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle account lockout', (done) => {
      const mockCredentials = { email: 'locked@test.com', password: 'password' };

      service.login(mockCredentials).subscribe(
        () => fail('should have failed'),
        (error) => {
          expect(error.status).toBe(423);
          expect(error.error.message).toContain('Account is locked');
          done();
        }
      );

      const req = httpMock.expectOne('/api/auth/login');
      req.flush(
        { message: 'Account is locked until 2025-11-18T10:00:00Z' },
        { status: 423, statusText: 'Locked' }
      );
    });
  });

  describe('logout', () => {
    it('should clear token from storage', () => {
      spyOn(localStorage, 'removeItem');
      service.logout();
      expect(localStorage.removeItem).toHaveBeenCalledWith('auth_token');
    });

    it('should clear user from state', () => {
      service.currentUser$.subscribe(user => {
        expect(user).toBeNull();
      });
      service.logout();
    });
  });
});
```

**Component Testing:**
```typescript
// Example: employee-list.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmployeeListComponent } from './employee-list.component';
import { EmployeeService } from '../../services/employee.service';
import { of, throwError } from 'rxjs';

describe('EmployeeListComponent', () => {
  let component: EmployeeListComponent;
  let fixture: ComponentFixture<EmployeeListComponent>;
  let employeeService: jasmine.SpyObj<EmployeeService>;

  beforeEach(async () => {
    const employeeServiceSpy = jasmine.createSpyObj('EmployeeService', ['getEmployees', 'deleteEmployee']);

    await TestBed.configureTestingModule({
      declarations: [EmployeeListComponent],
      providers: [
        { provide: EmployeeService, useValue: employeeServiceSpy }
      ]
    }).compileComponents();

    employeeService = TestBed.inject(EmployeeService) as jasmine.SpyObj<EmployeeService>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(EmployeeListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load employees on init', () => {
    const mockEmployees = [
      { id: 1, firstName: 'John', lastName: 'Doe', email: 'john@test.com' },
      { id: 2, firstName: 'Jane', lastName: 'Smith', email: 'jane@test.com' }
    ];
    employeeService.getEmployees.and.returnValue(of(mockEmployees));

    fixture.detectChanges(); // ngOnInit

    expect(component.employees.length).toBe(2);
    expect(employeeService.getEmployees).toHaveBeenCalled();
  });

  it('should display error message when loading fails', () => {
    employeeService.getEmployees.and.returnValue(throwError(() => new Error('Network error')));

    fixture.detectChanges();

    expect(component.error).toBe('Failed to load employees');
    expect(component.employees.length).toBe(0);
  });

  it('should filter employees by search term', () => {
    component.employees = [
      { id: 1, firstName: 'John', lastName: 'Doe', email: 'john@test.com' },
      { id: 2, firstName: 'Jane', lastName: 'Smith', email: 'jane@test.com' }
    ];

    component.searchTerm = 'john';
    component.filterEmployees();

    expect(component.filteredEmployees.length).toBe(1);
    expect(component.filteredEmployees[0].firstName).toBe('John');
  });

  it('should call delete service when delete is clicked', () => {
    const employeeId = 1;
    employeeService.deleteEmployee.and.returnValue(of(null));

    component.deleteEmployee(employeeId);

    expect(employeeService.deleteEmployee).toHaveBeenCalledWith(employeeId);
  });
});
```

**Running Frontend Unit Tests:**
```bash
# Run all tests
npm run test

# Run tests in headless mode (CI)
npm run test:headless

# Run tests with coverage
npm run test:coverage

# Run tests in watch mode
npm run test -- --watch

# Run specific test file
npm run test -- --include='**/auth.service.spec.ts'
```

### Backend Unit Testing (.NET)

**Framework:** xUnit + Moq

**What to Test:**
- Service business logic
- Controller actions
- Repository methods
- Validators
- Helpers and utilities
- Extension methods

**Test Structure:**
```csharp
// Example: AuthServiceTests.cs
using Xunit;
using Moq;
using HRMS.Application.Services;
using HRMS.Core.Entities;
using HRMS.Core.Interfaces;

namespace HRMS.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IAdminUserRepository> _mockUserRepo;
        private readonly Mock<IPasswordHasher> _mockHasher;
        private readonly Mock<IJwtTokenGenerator> _mockTokenGenerator;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUserRepo = new Mock<IAdminUserRepository>();
            _mockHasher = new Mock<IPasswordHasher>();
            _mockTokenGenerator = new Mock<IJwtTokenGenerator>();
            _authService = new AuthService(
                _mockUserRepo.Object,
                _mockHasher.Object,
                _mockTokenGenerator.Object
            );
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var email = "admin@test.com";
            var password = "Password123!";
            var hashedPassword = "hashed_password";

            var user = new AdminUser
            {
                Id = 1,
                Email = email,
                PasswordHash = hashedPassword,
                LockoutEnabled = true,
                AccessFailedCount = 0
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);
            _mockHasher.Setup(h => h.VerifyPassword(password, hashedPassword))
                .Returns(true);
            _mockTokenGenerator.Setup(t => t.GenerateToken(user))
                .Returns("jwt_token");

            // Act
            var result = await _authService.LoginAsync(email, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("jwt_token", result.Token);
            Assert.Equal(email, result.User.Email);
            _mockUserRepo.Verify(r => r.ResetAccessFailedCountAsync(user), Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_IncrementsFailedCount()
        {
            // Arrange
            var email = "admin@test.com";
            var password = "WrongPassword";
            var hashedPassword = "hashed_password";

            var user = new AdminUser
            {
                Id = 1,
                Email = email,
                PasswordHash = hashedPassword,
                LockoutEnabled = true,
                AccessFailedCount = 0
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);
            _mockHasher.Setup(h => h.VerifyPassword(password, hashedPassword))
                .Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => _authService.LoginAsync(email, password)
            );
            _mockUserRepo.Verify(r => r.IncrementAccessFailedCountAsync(user), Times.Once);
        }

        [Fact]
        public async Task Login_After5FailedAttempts_LocksAccount()
        {
            // Arrange
            var email = "admin@test.com";
            var password = "WrongPassword";

            var user = new AdminUser
            {
                Id = 1,
                Email = email,
                PasswordHash = "hashed_password",
                LockoutEnabled = true,
                AccessFailedCount = 4 // Next attempt will be 5th
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);
            _mockHasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => _authService.LoginAsync(email, password)
            );
            _mockUserRepo.Verify(r => r.LockAccountAsync(user, It.IsAny<DateTimeOffset>()), Times.Once);
        }

        [Fact]
        public async Task Login_WithLockedAccount_ThrowsAccountLockedException()
        {
            // Arrange
            var email = "admin@test.com";
            var password = "Password123!";

            var user = new AdminUser
            {
                Id = 1,
                Email = email,
                LockoutEnabled = true,
                LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            _mockUserRepo.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AccountLockedException>(
                () => _authService.LoginAsync(email, password)
            );
            Assert.Contains("locked until", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task Login_WithInvalidEmail_ThrowsArgumentException(string email)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _authService.LoginAsync(email, "password")
            );
        }
    }
}
```

**Running Backend Unit Tests:**
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=lcov

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Run tests by category
dotnet test --filter "Category=Unit"

# Run tests in parallel
dotnet test --parallel
```

**Coverage Goals:**
- Business Logic (Services): 90%+
- Controllers: 75%+
- Repositories: 80%+
- Utilities: 85%+
- Overall: 80%+

---

## 2. Integration Testing

### API Integration Tests

**Purpose:** Test API endpoints with real HTTP requests and database interactions

**Framework:** .NET Integration Tests with WebApplicationFactory

```csharp
// Example: EmployeeControllerIntegrationTests.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http.Json;

namespace HRMS.Tests.Integration
{
    public class EmployeeControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public EmployeeControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetEmployees_ReturnsOk_WithEmployeeList()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");

            // Act
            var response = await _client.GetAsync("/api/employees");

            // Assert
            response.EnsureSuccessStatusCode();
            var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
            Assert.NotNull(employees);
            Assert.True(employees.Count > 0);
        }

        [Fact]
        public async Task CreateEmployee_WithValidData_ReturnsCreated()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");

            var newEmployee = new CreateEmployeeRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                DepartmentId = 1,
                PositionId = 1
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/employees", newEmployee);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<EmployeeDto>();
            Assert.Equal("John", created.FirstName);
            Assert.Equal("Doe", created.LastName);
        }

        [Fact]
        public async Task GetEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _client.GetAsync("/api/employees/999999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateEmployee_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var newEmployee = new CreateEmployeeRequest
            {
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/employees", newEmployee);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var loginRequest = new { email = "admin@test.com", password = "Admin@123" };
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return result.Token;
        }
    }
}
```

**Database Integration Tests:**
```csharp
// Example: EmployeeRepositoryIntegrationTests.cs
public class EmployeeRepositoryIntegrationTests : IDisposable
{
    private readonly TenantDbContext _context;
    private readonly EmployeeRepository _repository;

    public EmployeeRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TenantDbContext(options);
        _repository = new EmployeeRepository(_context);

        SeedDatabase();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEmployee()
    {
        // Act
        var employee = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(employee);
        Assert.Equal("John", employee.FirstName);
    }

    [Fact]
    public async Task CreateAsync_WithValidEmployee_SavesToDatabase()
    {
        // Arrange
        var newEmployee = new Employee
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@test.com",
            DepartmentId = 1
        };

        // Act
        await _repository.CreateAsync(newEmployee);
        var saved = await _repository.GetByEmailAsync("jane@test.com");

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("Jane", saved.FirstName);
    }

    private void SeedDatabase()
    {
        _context.Employees.Add(new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            DepartmentId = 1
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

**Running Integration Tests:**
```bash
# Run integration tests only
dotnet test --filter "Category=Integration"

# Run with test database
DATABASE_URL=postgres://localhost:5432/hrms_test dotnet test

# Run integration tests in CI
dotnet test --filter "Category=Integration" --logger "trx;LogFileName=integration-results.trx"
```

---

## 3. End-to-End (E2E) Testing

### Playwright E2E Tests

**Purpose:** Test critical user journeys from end to end

**Framework:** Playwright

**Setup:**
```bash
# Install Playwright
npm install -D @playwright/test

# Install browsers
npx playwright install

# Create playwright.config.ts
npx playwright init
```

**Configuration:**
```typescript
// playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['junit', { outputFile: 'results.xml' }],
    ['json', { outputFile: 'results.json' }]
  ],
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    }
  ],
  webServer: {
    command: 'npm run start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env.CI,
  },
});
```

**E2E Test Examples:**
```typescript
// e2e/auth/login.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test('should login successfully with valid credentials @smoke', async ({ page }) => {
    await page.goto('/login');

    await page.fill('input[name="email"]', 'admin@hrms.com');
    await page.fill('input[name="password"]', 'Admin@123');
    await page.click('button[type="submit"]');

    await expect(page).toHaveURL('/dashboard');
    await expect(page.locator('h1')).toContainText('Dashboard');
  });

  test('should show error with invalid credentials', async ({ page }) => {
    await page.goto('/login');

    await page.fill('input[name="email"]', 'admin@hrms.com');
    await page.fill('input[name="password"]', 'wrong-password');
    await page.click('button[type="submit"]');

    await expect(page.locator('.error-message')).toContainText('Invalid credentials');
    await expect(page).toHaveURL('/login');
  });

  test('should lock account after 5 failed attempts', async ({ page }) => {
    await page.goto('/login');

    for (let i = 0; i < 5; i++) {
      await page.fill('input[name="email"]', 'admin@hrms.com');
      await page.fill('input[name="password"]', 'wrong-password');
      await page.click('button[type="submit"]');
      await page.waitForTimeout(500);
    }

    await expect(page.locator('.error-message')).toContainText('Account is locked');
  });
});

// e2e/employee/employee-management.spec.ts
test.describe('Employee Management', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/login');
    await page.fill('input[name="email"]', 'hr@hrms.com');
    await page.fill('input[name="password"]', 'Hr@123');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/dashboard');
  });

  test('should create new employee @smoke', async ({ page }) => {
    await page.goto('/employees');
    await page.click('button:has-text("Add Employee")');

    await page.fill('input[name="firstName"]', 'John');
    await page.fill('input[name="lastName"]', 'Doe');
    await page.fill('input[name="email"]', 'john.doe@company.com');
    await page.selectOption('select[name="department"]', '1');
    await page.selectOption('select[name="position"]', '1');

    await page.click('button[type="submit"]');

    await expect(page.locator('.success-message')).toContainText('Employee created successfully');
    await expect(page.locator('table tbody tr')).toContainText('John Doe');
  });

  test('should edit existing employee', async ({ page }) => {
    await page.goto('/employees');
    await page.click('table tbody tr:first-child button:has-text("Edit")');

    await page.fill('input[name="firstName"]', 'Jane');
    await page.click('button[type="submit"]');

    await expect(page.locator('.success-message')).toContainText('Employee updated successfully');
    await expect(page.locator('table tbody tr:first-child')).toContainText('Jane');
  });

  test('should delete employee', async ({ page }) => {
    await page.goto('/employees');

    page.on('dialog', dialog => dialog.accept());
    await page.click('table tbody tr:first-child button:has-text("Delete")');

    await expect(page.locator('.success-message')).toContainText('Employee deleted successfully');
  });
});

// e2e/leave/leave-application.spec.ts
test.describe('Leave Management', () => {
  test('should apply for leave @smoke', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'employee@hrms.com');
    await page.fill('input[name="password"]', 'Employee@123');
    await page.click('button[type="submit"]');

    await page.goto('/leave/apply');
    await page.selectOption('select[name="leaveType"]', 'Annual');
    await page.fill('input[name="startDate"]', '2025-12-01');
    await page.fill('input[name="endDate"]', '2025-12-05');
    await page.fill('textarea[name="reason"]', 'Family vacation');
    await page.click('button[type="submit"]');

    await expect(page.locator('.success-message')).toContainText('Leave application submitted');
  });

  test('should approve leave as manager', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'manager@hrms.com');
    await page.fill('input[name="password"]', 'Manager@123');
    await page.click('button[type="submit"]');

    await page.goto('/leave/approvals');
    await page.click('table tbody tr:first-child button:has-text("Approve")');

    await expect(page.locator('.success-message')).toContainText('Leave approved');
    await expect(page.locator('table tbody tr:first-child .status')).toContainText('Approved');
  });
});
```

**Running E2E Tests:**
```bash
# Run all E2E tests
npx playwright test

# Run smoke tests only
npx playwright test --grep @smoke

# Run in headed mode (see browser)
npx playwright test --headed

# Run specific browser
npx playwright test --project=chromium

# Run in debug mode
npx playwright test --debug

# Generate report
npx playwright show-report
```

**Critical User Journeys to Test:**
1. Authentication (login, logout, password reset)
2. Employee Management (CRUD operations)
3. Leave Management (apply, approve, reject)
4. Timesheet (submit, approve)
5. Payroll (view payslips)
6. Reports (generate, download)

---

## 4. Performance Testing

### Load Testing with k6

**Purpose:** Ensure application can handle expected load

**Framework:** k6

**Setup:**
```bash
# Install k6
brew install k6  # macOS
# or
sudo apt-get install k6  # Ubuntu

# Create test script
```

**Load Test Example:**
```javascript
// load-tests/api-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp up to 100 users
    { duration: '5m', target: 100 },  // Stay at 100 users
    { duration: '2m', target: 200 },  // Ramp up to 200 users
    { duration: '5m', target: 200 },  // Stay at 200 users
    { duration: '2m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    errors: ['rate<0.01'],            // Error rate must be below 1%
  },
};

const BASE_URL = 'https://hrms.example.com/api';
let authToken;

export function setup() {
  // Login to get auth token
  const loginRes = http.post(`${BASE_URL}/auth/login`, JSON.stringify({
    email: 'loadtest@hrms.com',
    password: 'LoadTest@123'
  }), {
    headers: { 'Content-Type': 'application/json' },
  });

  return { token: loginRes.json('token') };
}

export default function(data) {
  const params = {
    headers: {
      'Authorization': `Bearer ${data.token}`,
      'X-Tenant-Id': '1',
      'Content-Type': 'application/json',
    },
  };

  // Test: Get employees list
  let res = http.get(`${BASE_URL}/employees`, params);
  check(res, {
    'get employees status 200': (r) => r.status === 200,
    'get employees duration < 500ms': (r) => r.timings.duration < 500,
  }) || errorRate.add(1);

  sleep(1);

  // Test: Get specific employee
  res = http.get(`${BASE_URL}/employees/1`, params);
  check(res, {
    'get employee status 200': (r) => r.status === 200,
    'get employee duration < 200ms': (r) => r.timings.duration < 200,
  }) || errorRate.add(1);

  sleep(1);

  // Test: Create employee
  res = http.post(`${BASE_URL}/employees`, JSON.stringify({
    firstName: `User${__VU}`,
    lastName: `Test${__ITER}`,
    email: `user${__VU}-${__ITER}@test.com`,
    departmentId: 1,
    positionId: 1
  }), params);
  check(res, {
    'create employee status 201': (r) => r.status === 201,
  }) || errorRate.add(1);

  sleep(2);
}

export function teardown(data) {
  // Cleanup if needed
}
```

**Running Load Tests:**
```bash
# Run load test
k6 run load-tests/api-load-test.js

# Run with custom duration
k6 run --duration 10m load-tests/api-load-test.js

# Run with custom VUs
k6 run --vus 50 --duration 5m load-tests/api-load-test.js

# Output to JSON
k6 run --out json=results.json load-tests/api-load-test.js

# Cloud execution
k6 cloud load-tests/api-load-test.js
```

**Performance Benchmarks:**
- API P95: < 500ms
- API P99: < 1000ms
- Error Rate: < 0.1%
- Throughput: > 1000 req/sec
- Concurrent Users: 500+

---

## 5. Security Testing

### SAST (Static Application Security Testing)

**Tool:** SonarQube

```bash
# Run SonarQube scan
dotnet sonarscanner begin \
  /k:"hrms-app" \
  /d:sonar.host.url="http://sonarqube:9000" \
  /d:sonar.login="token"

dotnet build

dotnet sonarscanner end /d:sonar.login="token"
```

### DAST (Dynamic Application Security Testing)

**Tool:** OWASP ZAP

```bash
# Run ZAP scan
docker run -t owasp/zap2docker-stable zap-baseline.py \
  -t https://hrms.example.com \
  -r zap-report.html
```

### Dependency Scanning

```bash
# Frontend
npm audit --production

# Backend
dotnet list package --vulnerable --include-transitive

# Using Snyk
snyk test
snyk monitor
```

### Penetration Testing Checklist

- [ ] SQL Injection testing
- [ ] XSS (Cross-Site Scripting) testing
- [ ] CSRF (Cross-Site Request Forgery) testing
- [ ] Authentication bypass attempts
- [ ] Authorization bypass attempts
- [ ] Session management testing
- [ ] Input validation testing
- [ ] API security testing
- [ ] Rate limiting testing
- [ ] SSL/TLS configuration testing

---

## 6. Accessibility Testing

### Automated Accessibility Testing

**Tool:** axe-core with Playwright

```typescript
// e2e/accessibility.spec.ts
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Accessibility', () => {
  test('should not have any automatically detectable accessibility issues', async ({ page }) => {
    await page.goto('/');

    const accessibilityScanResults = await new AxeBuilder({ page }).analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('login page should be accessible', async ({ page }) => {
    await page.goto('/login');

    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });
});
```

### Manual Accessibility Testing

- [ ] Keyboard navigation
- [ ] Screen reader compatibility
- [ ] Color contrast (WCAG AA)
- [ ] Focus indicators
- [ ] ARIA labels
- [ ] Alt text for images
- [ ] Form label associations
- [ ] Skip navigation links

**Tools:**
- Lighthouse (automated)
- axe DevTools (browser extension)
- NVDA/JAWS (screen readers)
- Keyboard only navigation

---

## 7. Visual Regression Testing

### Percy Integration

```typescript
// Example with Percy
import percySnapshot from '@percy/playwright';

test('visual regression test', async ({ page }) => {
  await page.goto('/dashboard');
  await percySnapshot(page, 'Dashboard');

  await page.goto('/employees');
  await percySnapshot(page, 'Employee List');
});
```

---

## Test Execution Strategy

### Local Development
```bash
# Quick feedback loop
npm run test:watch          # Unit tests in watch mode
npm run test:changed        # Only changed files
```

### Pre-Commit
```bash
npm run test:headless       # All unit tests
npm run lint                # Linting
```

### CI/CD Pipeline
```bash
# Pull Request
- Unit tests (all)
- Integration tests (all)
- E2E tests (smoke suite only)
- Security scan
- Coverage check

# Main Branch
- Unit tests (all)
- Integration tests (all)
- E2E tests (full suite)
- Performance tests (light)
- Security scan (full)

# Release
- All tests
- Load testing (full)
- Security penetration testing
- Accessibility audit
```

---

## Test Data Management

### Test Data Strategy
- Use factories for test data generation
- Seed database with realistic data
- Use faker.js for fake data
- Maintain test data snapshots

### Test Database
- Separate test database
- Reset between test runs
- Use transactions for isolation
- In-memory database for unit tests

---

## Estimated Implementation Time

| Task | Time |
|------|------|
| Unit test framework setup | 4h |
| Write unit tests (initial) | 40h |
| Integration test setup | 8h |
| Write integration tests | 24h |
| E2E test setup (Playwright) | 8h |
| Write E2E tests | 32h |
| Performance test setup | 8h |
| Security testing setup | 8h |
| CI/CD integration | 16h |
| Documentation | 4h |
| **Total** | **152h (4 weeks)** |

---

## Success Metrics

- Test Coverage: >= 80%
- Test Execution Time: < 15 minutes
- E2E Test Success Rate: >= 95%
- Flaky Test Rate: < 2%
- Bugs Found in Production: < 5 per release

---

**Document Owner:** QA Team
**Last Review Date:** 2025-11-17
**Next Review Date:** 2026-02-17
