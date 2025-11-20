namespace HRMS.Core.Enums;

/// <summary>
/// Employee termination type classification
/// Used for turnover analysis and compliance reporting
/// Fortune 500 standard categories
/// </summary>
public enum TerminationType
{
    /// <summary>
    /// Employee resigned voluntarily
    /// </summary>
    Voluntary = 1,

    /// <summary>
    /// Employee was terminated by company
    /// </summary>
    Involuntary = 2,

    /// <summary>
    /// Employee retired
    /// </summary>
    Retirement = 3,

    /// <summary>
    /// Contract expired (not renewed)
    /// </summary>
    ContractExpired = 4,

    /// <summary>
    /// Mutual agreement termination
    /// </summary>
    MutualAgreement = 5,

    /// <summary>
    /// Death of employee
    /// </summary>
    Deceased = 6,

    /// <summary>
    /// Termination during probation period
    /// </summary>
    ProbationTermination = 7,

    /// <summary>
    /// Job abandonment / No-show
    /// </summary>
    Abandonment = 8,

    /// <summary>
    /// Layoff due to restructuring
    /// </summary>
    Layoff = 9,

    /// <summary>
    /// Other reason
    /// </summary>
    Other = 10
}
