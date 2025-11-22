// ════════════════════════════════════════════════════════════════════════════
// End-to-End Load Test
// Comprehensive user journeys testing all major workflows
// ════════════════════════════════════════════════════════════════════════════

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';
import { config } from './config.js';
import {
  authenticateAdmin,
  authenticateTenantAdmin,
  authenticateEmployee,
  authenticatedGet,
  authenticatedPost,
  authenticatedPut,
  authenticatedDelete,
  generateTenantData,
  generateEmployeeData,
  generateAttendanceData,
  generateLeaveRequest,
  thinkTime,
  randomSleep,
  validatePagination,
} from './utils.js';

// ────────────────────────────────────────────────────────────────────────────
// Custom Metrics
// ────────────────────────────────────────────────────────────────────────────

const tenantCreations = new Counter('tenant_creations');
const employeeCreations = new Counter('employee_creations');
const attendanceRecords = new Counter('attendance_records');
const leaveRequests = new Counter('leave_requests');
const tenantCreationTime = new Trend('tenant_creation_time');
const employeeCreationTime = new Trend('employee_creation_time');
const attendanceRecordTime = new Trend('attendance_record_time');
const leaveRequestTime = new Trend('leave_request_time');
const dashboardLoadTime = new Trend('dashboard_load_time');
const reportGenerationTime = new Trend('report_generation_time');

// ────────────────────────────────────────────────────────────────────────────
// Test Configuration
// ────────────────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    // SuperAdmin workflow
    superadmin_workflow: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 5 },     // Ramp up
        { duration: '5m', target: 5 },     // Sustain
        { duration: '30s', target: 0 },    // Ramp down
      ],
      exec: 'superAdminWorkflow',
    },

    // Tenant admin workflow
    tenant_admin_workflow: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },    // Ramp up
        { duration: '5m', target: 20 },    // Sustain
        { duration: '1m', target: 40 },    // Ramp up more
        { duration: '5m', target: 40 },    // Sustain
        { duration: '1m', target: 0 },     // Ramp down
      ],
      startTime: '30s',
      exec: 'tenantAdminWorkflow',
    },

    // Employee workflow
    employee_workflow: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 50 },    // Ramp up
        { duration: '5m', target: 50 },    // Sustain
        { duration: '2m', target: 100 },   // Ramp up more
        { duration: '5m', target: 100 },   // Sustain
        { duration: '2m', target: 0 },     // Ramp down
      ],
      startTime: '1m',
      exec: 'employeeWorkflow',
    },
  },

  thresholds: {
    'tenant_creation_time': ['p(95)<3000'],                  // Tenant creation < 3s
    'employee_creation_time': ['p(95)<1000'],                // Employee creation < 1s
    'attendance_record_time': ['p(95)<500'],                 // Attendance < 500ms
    'leave_request_time': ['p(95)<800'],                     // Leave request < 800ms
    'dashboard_load_time': ['p(95)<2000'],                   // Dashboard < 2s
    'report_generation_time': ['p(95)<5000'],                // Reports < 5s
    'http_req_duration': ['p(95)<1000', 'p(99)<2000'],
    'http_req_failed': ['rate<0.01'],                        // Error rate < 1%
  },
};

// ────────────────────────────────────────────────────────────────────────────
// SuperAdmin Workflow
// ────────────────────────────────────────────────────────────────────────────

export function superAdminWorkflow() {
  group('SuperAdmin Complete Workflow', () => {
    // Step 1: Login
    const token = authenticateAdmin();
    if (!token) return;

    thinkTime();

    // Step 2: View dashboard
    group('Dashboard', () => {
      const startTime = Date.now();

      const dashboardResponse = authenticatedGet(
        `${config.baseUrl}/api/admin/dashboard/statistics`,
        token,
        'AdminDashboard'
      );

      check(dashboardResponse, {
        'dashboard loaded': (r) => r.status === 200,
        'has statistics': (r) => {
          const json = r.json();
          return json.totalTenants !== undefined && json.activeTenants !== undefined;
        },
      });

      dashboardLoadTime.add(Date.now() - startTime);
    });

    thinkTime();

    // Step 3: Create new tenant
    group('Create Tenant', () => {
      const startTime = Date.now();
      const tenantData = generateTenantData(__VU);

      const createResponse = authenticatedPost(
        `${config.baseUrl}/api/tenants`,
        tenantData,
        token,
        'CreateTenant'
      );

      const success = check(createResponse, {
        'tenant created': (r) => r.status === 201 || r.status === 200,
        'tenant has id': (r) => r.json('id') !== undefined,
      });

      if (success) {
        tenantCreations.add(1);
        tenantCreationTime.add(Date.now() - startTime);

        const tenantId = createResponse.json('id');

        // Step 4: View tenant details
        thinkTime();
        const detailsResponse = authenticatedGet(
          `${config.baseUrl}/api/tenants/${tenantId}`,
          token,
          'TenantDetails'
        );

        check(detailsResponse, {
          'tenant details loaded': (r) => r.status === 200,
        });

        // Step 5: Update tenant
        thinkTime();
        const updateData = {
          ...tenantData,
          maxEmployees: 2000,
        };

        const updateResponse = authenticatedPut(
          `${config.baseUrl}/api/tenants/${tenantId}`,
          updateData,
          token,
          'UpdateTenant'
        );

        check(updateResponse, {
          'tenant updated': (r) => r.status === 200,
        });
      }
    });

    thinkTime();

    // Step 6: View tenant list
    group('Tenant List', () => {
      const listResponse = authenticatedGet(
        `${config.baseUrl}/api/tenants?pageNumber=1&pageSize=20`,
        token,
        'TenantList'
      );

      check(listResponse, {
        'tenant list loaded': (r) => r.status === 200,
        'has pagination': (r) => validatePagination(r),
      });
    });

    thinkTime();

    // Step 7: View activity logs
    group('Activity Logs', () => {
      const logsResponse = authenticatedGet(
        `${config.baseUrl}/api/admin/monitoring/tenants/activity?pageNumber=1&pageSize=50`,
        token,
        'ActivityLogs'
      );

      check(logsResponse, {
        'activity logs loaded': (r) => r.status === 200,
      });
    });

    thinkTime();

    // Step 8: Generate system report
    group('System Report', () => {
      const startTime = Date.now();

      const reportResponse = authenticatedGet(
        `${config.baseUrl}/api/admin/reports/system-health`,
        token,
        'SystemReport'
      );

      check(reportResponse, {
        'report generated': (r) => r.status === 200,
      });

      reportGenerationTime.add(Date.now() - startTime);
    });

    randomSleep(2, 5);
  });
}

// ────────────────────────────────────────────────────────────────────────────
// Tenant Admin Workflow
// ────────────────────────────────────────────────────────────────────────────

export function tenantAdminWorkflow() {
  group('Tenant Admin Complete Workflow', () => {
    // Step 1: Login
    const token = authenticateTenantAdmin();
    if (!token) return;

    thinkTime();

    // Step 2: View dashboard
    group('Dashboard', () => {
      const startTime = Date.now();

      const dashboardResponse = authenticatedGet(
        `${config.baseUrl}/api/tenant/dashboard`,
        token,
        'TenantDashboard'
      );

      check(dashboardResponse, {
        'dashboard loaded': (r) => r.status === 200,
      });

      dashboardLoadTime.add(Date.now() - startTime);
    });

    thinkTime();

    // Step 3: Create employee
    group('Create Employee', () => {
      const startTime = Date.now();
      const employeeData = generateEmployeeData(__VU + __ITER);

      const createResponse = authenticatedPost(
        `${config.baseUrl}/api/tenant/employees`,
        employeeData,
        token,
        'CreateEmployee'
      );

      const success = check(createResponse, {
        'employee created': (r) => r.status === 201 || r.status === 200,
        'employee has id': (r) => r.json('id') !== undefined,
      });

      if (success) {
        employeeCreations.add(1);
        employeeCreationTime.add(Date.now() - startTime);

        const employeeId = createResponse.json('id');

        // Step 4: View employee details
        thinkTime();
        const detailsResponse = authenticatedGet(
          `${config.baseUrl}/api/tenant/employees/${employeeId}`,
          token,
          'EmployeeDetails'
        );

        check(detailsResponse, {
          'employee details loaded': (r) => r.status === 200,
        });

        // Step 5: Update employee
        thinkTime();
        const updateData = {
          ...employeeData,
          salary: employeeData.salary + 5000,
        };

        const updateResponse = authenticatedPut(
          `${config.baseUrl}/api/tenant/employees/${employeeId}`,
          updateData,
          token,
          'UpdateEmployee'
        );

        check(updateResponse, {
          'employee updated': (r) => r.status === 200,
        });

        // Step 6: Record attendance for employee
        thinkTime();
        group('Record Attendance', () => {
          const attendanceStart = Date.now();
          const attendanceData = generateAttendanceData(employeeId);

          const attendanceResponse = authenticatedPost(
            `${config.baseUrl}/api/tenant/attendance`,
            attendanceData,
            token,
            'RecordAttendance'
          );

          const attendanceSuccess = check(attendanceResponse, {
            'attendance recorded': (r) => r.status === 201 || r.status === 200,
          });

          if (attendanceSuccess) {
            attendanceRecords.add(1);
            attendanceRecordTime.add(Date.now() - attendanceStart);
          }
        });
      }
    });

    thinkTime();

    // Step 7: View employee list
    group('Employee List', () => {
      const listResponse = authenticatedGet(
        `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=20`,
        token,
        'EmployeeList'
      );

      check(listResponse, {
        'employee list loaded': (r) => r.status === 200,
        'has pagination': (r) => validatePagination(r),
      });
    });

    thinkTime();

    // Step 8: View attendance report
    group('Attendance Report', () => {
      const startTime = Date.now();
      const today = new Date().toISOString().split('T')[0];

      const reportResponse = authenticatedGet(
        `${config.baseUrl}/api/tenant/reports/attendance?startDate=${today}&endDate=${today}`,
        token,
        'AttendanceReport'
      );

      check(reportResponse, {
        'attendance report generated': (r) => r.status === 200,
      });

      reportGenerationTime.add(Date.now() - startTime);
    });

    thinkTime();

    // Step 9: Process pending leave requests
    group('Leave Management', () => {
      const pendingResponse = authenticatedGet(
        `${config.baseUrl}/api/tenant/leave/pending?pageNumber=1&pageSize=10`,
        token,
        'PendingLeaveRequests'
      );

      check(pendingResponse, {
        'pending leave requests loaded': (r) => r.status === 200,
      });

      // Approve a leave request if any exist
      if (pendingResponse.json('items') && pendingResponse.json('items').length > 0) {
        const leaveId = pendingResponse.json('items')[0].id;

        thinkTime();
        const approveResponse = authenticatedPut(
          `${config.baseUrl}/api/tenant/leave/${leaveId}/approve`,
          { comments: 'Approved during load test' },
          token,
          'ApproveLeave'
        );

        check(approveResponse, {
          'leave request approved': (r) => r.status === 200,
        });
      }
    });

    randomSleep(2, 5);
  });
}

// ────────────────────────────────────────────────────────────────────────────
// Employee Workflow
// ────────────────────────────────────────────────────────────────────────────

export function employeeWorkflow() {
  group('Employee Complete Workflow', () => {
    // Step 1: Login
    const token = authenticateEmployee();
    if (!token) return;

    thinkTime();

    // Step 2: View dashboard
    group('Dashboard', () => {
      const startTime = Date.now();

      const dashboardResponse = authenticatedGet(
        `${config.baseUrl}/api/employee/dashboard`,
        token,
        'EmployeeDashboard'
      );

      check(dashboardResponse, {
        'dashboard loaded': (r) => r.status === 200,
      });

      dashboardLoadTime.add(Date.now() - startTime);
    });

    thinkTime();

    // Step 3: Clock in/out (attendance)
    group('Attendance', () => {
      const clockInStart = Date.now();

      const clockInResponse = authenticatedPost(
        `${config.baseUrl}/api/employee/attendance/clock-in`,
        {},
        token,
        'ClockIn'
      );

      const success = check(clockInResponse, {
        'clocked in successfully': (r) => r.status === 200 || r.status === 201,
      });

      if (success) {
        attendanceRecords.add(1);
        attendanceRecordTime.add(Date.now() - clockInStart);

        // Simulate work time
        sleep(5);

        // Clock out
        const clockOutResponse = authenticatedPost(
          `${config.baseUrl}/api/employee/attendance/clock-out`,
          {},
          token,
          'ClockOut'
        );

        check(clockOutResponse, {
          'clocked out successfully': (r) => r.status === 200,
        });
      }
    });

    thinkTime();

    // Step 4: View attendance history
    group('Attendance History', () => {
      const historyResponse = authenticatedGet(
        `${config.baseUrl}/api/employee/attendance?pageNumber=1&pageSize=20`,
        token,
        'AttendanceHistory'
      );

      check(historyResponse, {
        'attendance history loaded': (r) => r.status === 200,
        'has pagination': (r) => validatePagination(r),
      });
    });

    thinkTime();

    // Step 5: Submit leave request
    group('Leave Request', () => {
      const leaveStart = Date.now();
      const leaveData = generateLeaveRequest('employee-id');

      const leaveResponse = authenticatedPost(
        `${config.baseUrl}/api/employee/leave/request`,
        leaveData,
        token,
        'SubmitLeaveRequest'
      );

      const success = check(leaveResponse, {
        'leave request submitted': (r) => r.status === 201 || r.status === 200,
      });

      if (success) {
        leaveRequests.add(1);
        leaveRequestTime.add(Date.now() - leaveStart);
      }
    });

    thinkTime();

    // Step 6: View leave balance
    group('Leave Balance', () => {
      const balanceResponse = authenticatedGet(
        `${config.baseUrl}/api/employee/leave/balance`,
        token,
        'LeaveBalance'
      );

      check(balanceResponse, {
        'leave balance loaded': (r) => r.status === 200,
      });
    });

    thinkTime();

    // Step 7: View payslips
    group('Payslips', () => {
      const payslipsResponse = authenticatedGet(
        `${config.baseUrl}/api/employee/payroll/payslips?pageNumber=1&pageSize=12`,
        token,
        'Payslips'
      );

      check(payslipsResponse, {
        'payslips loaded': (r) => r.status === 200,
      });
    });

    thinkTime();

    // Step 8: Update profile
    group('Profile Update', () => {
      const profileData = {
        phoneNumber: `+1${Math.floor(Math.random() * 9000000000 + 1000000000)}`,
      };

      const updateResponse = authenticatedPut(
        `${config.baseUrl}/api/employee/profile`,
        profileData,
        token,
        'UpdateProfile'
      );

      check(updateResponse, {
        'profile updated': (r) => r.status === 200,
      });
    });

    randomSleep(1, 4);
  });
}

// ────────────────────────────────────────────────────────────────────────────
// Setup & Teardown
// ────────────────────────────────────────────────────────────────────────────

export function setup() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  End-to-End Load Test');
  console.log('  Base URL:', config.baseUrl);
  console.log('  Testing complete user workflows');
  console.log('═══════════════════════════════════════════════════════════');
}

export function teardown(data) {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  End-to-End Load Test Complete');
  console.log('═══════════════════════════════════════════════════════════');
}

// ────────────────────────────────────────────────────────────────────────────
// Default Export
// ────────────────────────────────────────────────────────────────────────────

export default function () {
  // Scenarios are defined above
}
