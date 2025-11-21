import { Injectable } from '@angular/core';
import { ActivityLog } from '../models/dashboard.model';

/**
 * Service to handle exporting activity logs to various formats
 * Supports CSV and PDF export
 */
@Injectable({
  providedIn: 'root'
})
export class ActivityExportService {

  /**
   * Export activities to CSV format
   */
  exportToCSV(activities: ActivityLog[], filename: string = 'activity-log'): void {
    const csvContent = this.generateCSV(activities);
    this.downloadFile(csvContent, `${filename}.csv`, 'text/csv');
  }

  /**
   * Export activities to PDF format
   * Note: This is a simplified HTML-to-PDF approach
   * For production, consider using libraries like jsPDF or pdfmake
   */
  exportToPDF(activities: ActivityLog[], filename: string = 'activity-log'): void {
    const htmlContent = this.generateHTMLReport(activities);

    // Create a printable window
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      printWindow.document.write(htmlContent);
      printWindow.document.close();

      // Wait for content to load then print
      printWindow.onload = () => {
        printWindow.print();
      };
    }
  }

  /**
   * Generate CSV content from activities
   */
  private generateCSV(activities: ActivityLog[]): string {
    const headers = [
      'ID',
      'Timestamp',
      'Type',
      'Severity',
      'Title',
      'Description',
      'Tenant Name',
      'User Name',
      'Metadata'
    ];

    const rows = activities.map(activity => [
      this.escapeCsvValue(activity.id),
      this.escapeCsvValue(activity.timestamp.toISOString()),
      this.escapeCsvValue(activity.type),
      this.escapeCsvValue(activity.severity),
      this.escapeCsvValue(activity.title),
      this.escapeCsvValue(activity.description),
      this.escapeCsvValue(activity.tenantName || ''),
      this.escapeCsvValue(activity.userName || ''),
      this.escapeCsvValue(JSON.stringify(activity.metadata || {}))
    ]);

    const csvRows = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ];

    return csvRows.join('\n');
  }

  /**
   * Escape CSV value to handle commas, quotes, and newlines
   */
  private escapeCsvValue(value: string): string {
    if (value.includes(',') || value.includes('"') || value.includes('\n')) {
      return `"${value.replace(/"/g, '""')}"`;
    }
    return value;
  }

  /**
   * Generate HTML report for PDF printing
   */
  private generateHTMLReport(activities: ActivityLog[]): string {
    const timestamp = new Date().toLocaleString();

    return `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <title>Activity Log Report</title>
  <style>
    * {
      margin: 0;
      padding: 0;
      box-sizing: border-box;
    }

    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      padding: 40px;
      color: #1a1a1a;
      background: white;
    }

    .report-header {
      margin-bottom: 40px;
      border-bottom: 3px solid #3b82f6;
      padding-bottom: 20px;
    }

    .report-title {
      font-size: 28px;
      font-weight: 700;
      color: #1a1a1a;
      margin-bottom: 8px;
    }

    .report-meta {
      font-size: 14px;
      color: #666;
    }

    .activities-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 20px;
    }

    .activities-table th {
      background: #f3f4f6;
      padding: 12px;
      text-align: left;
      font-weight: 600;
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #4b5563;
      border-bottom: 2px solid #e5e7eb;
    }

    .activities-table td {
      padding: 12px;
      border-bottom: 1px solid #e5e7eb;
      font-size: 13px;
      vertical-align: top;
    }

    .activities-table tr:hover {
      background: #f9fafb;
    }

    .severity-badge {
      display: inline-block;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .severity-success {
      background: rgba(34, 197, 94, 0.1);
      color: rgb(34, 197, 94);
    }

    .severity-info {
      background: rgba(59, 130, 246, 0.1);
      color: rgb(59, 130, 246);
    }

    .severity-warning {
      background: rgba(234, 179, 8, 0.1);
      color: rgb(234, 179, 8);
    }

    .severity-error {
      background: rgba(239, 68, 68, 0.1);
      color: rgb(239, 68, 68);
    }

    .title {
      font-weight: 600;
      color: #1a1a1a;
      margin-bottom: 4px;
    }

    .description {
      color: #6b7280;
      font-size: 12px;
      line-height: 1.5;
    }

    .timestamp {
      color: #9ca3af;
      font-size: 12px;
      white-space: nowrap;
    }

    .metadata {
      font-family: 'Monaco', 'Menlo', monospace;
      font-size: 11px;
      color: #6b7280;
      background: #f9fafb;
      padding: 4px 8px;
      border-radius: 4px;
      max-width: 200px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .report-footer {
      margin-top: 40px;
      padding-top: 20px;
      border-top: 1px solid #e5e7eb;
      text-align: center;
      color: #9ca3af;
      font-size: 12px;
    }

    @media print {
      body {
        padding: 20px;
      }

      .activities-table {
        page-break-inside: auto;
      }

      .activities-table tr {
        page-break-inside: avoid;
        page-break-after: auto;
      }
    }
  </style>
</head>
<body>
  <div class="report-header">
    <h1 class="report-title">Activity Log Report</h1>
    <p class="report-meta">Generated on ${timestamp} | Total Activities: ${activities.length}</p>
  </div>

  <table class="activities-table">
    <thead>
      <tr>
        <th style="width: 200px;">Timestamp</th>
        <th style="width: 120px;">Severity</th>
        <th>Activity</th>
        <th style="width: 150px;">Tenant/User</th>
      </tr>
    </thead>
    <tbody>
      ${activities.map(activity => `
        <tr>
          <td class="timestamp">${activity.timestamp.toLocaleString()}</td>
          <td>
            <span class="severity-badge severity-${activity.severity}">
              ${activity.severity}
            </span>
          </td>
          <td>
            <div class="title">${this.escapeHtml(activity.title)}</div>
            <div class="description">${this.escapeHtml(activity.description)}</div>
            ${activity.metadata ? `<div class="metadata">${this.escapeHtml(JSON.stringify(activity.metadata))}</div>` : ''}
          </td>
          <td>
            ${activity.tenantName ? `<div><strong>Tenant:</strong> ${this.escapeHtml(activity.tenantName)}</div>` : ''}
            ${activity.userName ? `<div><strong>User:</strong> ${this.escapeHtml(activity.userName)}</div>` : ''}
          </td>
        </tr>
      `).join('')}
    </tbody>
  </table>

  <div class="report-footer">
    <p>HRMS Activity Log Report | Confidential</p>
  </div>
</body>
</html>
    `;
  }

  /**
   * Escape HTML to prevent XSS
   */
  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Download file to user's computer
   */
  private downloadFile(content: string, filename: string, mimeType: string): void {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';

    document.body.appendChild(link);
    link.click();

    // Cleanup
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  /**
   * Export filtered subset of activities
   */
  exportFiltered(
    activities: ActivityLog[],
    format: 'csv' | 'pdf',
    filterDescription?: string
  ): void {
    const timestamp = new Date().toISOString().split('T')[0];
    const filename = filterDescription
      ? `activity-log-${filterDescription}-${timestamp}`
      : `activity-log-${timestamp}`;

    if (format === 'csv') {
      this.exportToCSV(activities, filename);
    } else {
      this.exportToPDF(activities, filename);
    }
  }
}
