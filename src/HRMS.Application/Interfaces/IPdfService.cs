namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for generating PDF documents
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates payslip PDF
    /// </summary>
    Task<byte[]> GeneratePayslipPdfAsync(Guid payslipId);

    /// <summary>
    /// Generates employment certificate PDF
    /// </summary>
    Task<byte[]> GenerateEmploymentCertificatePdfAsync(Guid employeeId);

    /// <summary>
    /// Generates monthly attendance report PDF
    /// </summary>
    Task<byte[]> GenerateAttendanceReportPdfAsync(Guid employeeId, int month, int year);

    /// <summary>
    /// Generates annual leave report PDF
    /// </summary>
    Task<byte[]> GenerateLeaveReportPdfAsync(Guid employeeId, int year);

    /// <summary>
    /// Generates tax certificate (Form C for MRA) PDF
    /// </summary>
    Task<byte[]> GenerateTaxCertificatePdfAsync(Guid employeeId, int year);
}
