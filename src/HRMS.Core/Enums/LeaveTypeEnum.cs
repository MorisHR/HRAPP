namespace HRMS.Core.Enums;

/// <summary>
/// Types of leave as per Mauritius Labour Law and company policy
/// </summary>
public enum LeaveTypeEnum
{
    AnnualLeave = 1,          // 22 days per year (Mauritius law)
    SickLeave = 2,            // 15 days paid per year (Mauritius law)
    CasualLeave = 3,          // As per company policy
    MaternityLeave = 4,       // 14 weeks (Mauritius law)
    PaternityLeave = 5,       // 5 days (Mauritius law)
    CompassionateLeave = 6,   // Death in family
    UnpaidLeave = 7,          // Unpaid time off
    StudyLeave = 8,           // Educational purposes
    MarriageLeave = 9,        // Wedding
    Other = 99                // Other types
}
