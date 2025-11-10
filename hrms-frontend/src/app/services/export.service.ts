import { Injectable } from '@angular/core';
import { SecurityAlert } from '../models/security-alert.model';

@Injectable({
  providedIn: 'root'
})
export class ExportService {

  /**
   * Export security alerts to CSV
   */
  exportToCSV(alerts: SecurityAlert[], filename = 'security-alerts.csv'): void {
    if (!alerts || alerts.length === 0) {
      console.warn('No data to export');
      return;
    }

    // Define CSV headers
    const headers = [
      'Alert ID',
      'Severity',
      'Status',
      'Alert Type',
      'Title',
      'Description',
      'Risk Score',
      'User Email',
      'User Name',
      'IP Address',
      'Location',
      'Detected At',
      'Acknowledged At',
      'Acknowledged By',
      'Resolved At',
      'Resolved By',
      'Resolution Notes',
      'Assigned To',
      'Tenant',
      'Requires Escalation',
      'Email Sent',
      'SMS Sent',
      'Slack Sent',
      'SIEM Sent'
    ];

    // Convert data to CSV rows
    const csvRows = [
      headers.join(','), // Header row
      ...alerts.map(alert => this.alertToCSVRow(alert))
    ];

    // Create CSV content
    const csvContent = csvRows.join('\n');

    // Create and download file
    this.downloadFile(csvContent, filename, 'text/csv;charset=utf-8;');
  }

  /**
   * Export security alerts to PDF
   */
  exportToPDF(alerts: SecurityAlert[], filename = 'security-alerts.pdf'): void {
    if (!alerts || alerts.length === 0) {
      console.warn('No data to export');
      return;
    }

    // Create PDF content using basic HTML to PDF conversion
    const htmlContent = this.generatePDFHTML(alerts);

    // Open print dialog for PDF generation
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      printWindow.document.write(htmlContent);
      printWindow.document.close();
      printWindow.focus();

      // Wait for content to load then print
      setTimeout(() => {
        printWindow.print();
      }, 250);
    }
  }

  /**
   * Convert alert to CSV row
   */
  private alertToCSVRow(alert: SecurityAlert): string {
    const fields = [
      alert.id,
      alert.severityName,
      alert.statusName,
      alert.alertTypeName,
      this.escapeCSV(alert.title),
      this.escapeCSV(alert.description),
      alert.riskScore,
      alert.userEmail || '',
      alert.userFullName || '',
      alert.ipAddress || '',
      alert.geolocation || '',
      this.formatDate(alert.detectedAt),
      alert.acknowledgedAt ? this.formatDate(alert.acknowledgedAt) : '',
      alert.acknowledgedByEmail || '',
      alert.resolvedAt ? this.formatDate(alert.resolvedAt) : '',
      alert.resolvedByEmail || '',
      this.escapeCSV(alert.resolutionNotes || ''),
      alert.assignedToEmail || '',
      alert.tenantName || '',
      alert.requiresEscalation ? 'Yes' : 'No',
      alert.emailSent ? 'Yes' : 'No',
      alert.smsSent ? 'Yes' : 'No',
      alert.slackSent ? 'Yes' : 'No',
      alert.siemSent ? 'Yes' : 'No'
    ];

    return fields.map(field => `"${field}"`).join(',');
  }

  /**
   * Escape CSV special characters
   */
  private escapeCSV(value: string): string {
    if (!value) return '';
    return value.replace(/"/g, '""').replace(/\n/g, ' ').replace(/\r/g, '');
  }

  /**
   * Format date for export
   */
  private formatDate(date: Date | string): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleString();
  }

  /**
   * Generate HTML for PDF
   */
  private generatePDFHTML(alerts: SecurityAlert[]): string {
    const currentDate = new Date().toLocaleString();
    const title = 'Security Alerts Report';

    const tableRows = alerts.map(alert => `
      <tr>
        <td>${alert.id.substring(0, 8)}</td>
        <td><span class="badge badge-${this.getSeverityClass(alert.severityName)}">${alert.severityName}</span></td>
        <td>${alert.alertTypeName}</td>
        <td>${this.escapeHTML(alert.title)}</td>
        <td>${alert.riskScore}/100</td>
        <td>${alert.userEmail || 'N/A'}</td>
        <td>${this.formatDate(alert.detectedAt)}</td>
        <td><span class="badge badge-${this.getStatusClass(alert.statusName)}">${alert.statusName}</span></td>
      </tr>
    `).join('');

    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="UTF-8">
        <title>${title}</title>
        <style>
          @page {
            size: A4 landscape;
            margin: 1cm;
          }
          body {
            font-family: Arial, sans-serif;
            font-size: 10pt;
            margin: 0;
            padding: 20px;
          }
          .header {
            text-align: center;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 2px solid #333;
          }
          .header h1 {
            margin: 0 0 5px 0;
            color: #2c3e50;
          }
          .header p {
            margin: 0;
            color: #7f8c8d;
            font-size: 9pt;
          }
          .summary {
            margin-bottom: 20px;
            background: #f8f9fa;
            padding: 10px;
            border-radius: 5px;
          }
          .summary strong {
            color: #2c3e50;
          }
          table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
          }
          th {
            background-color: #34495e;
            color: white;
            padding: 8px;
            text-align: left;
            font-size: 9pt;
            border: 1px solid #2c3e50;
          }
          td {
            padding: 6px 8px;
            border: 1px solid #ddd;
            font-size: 9pt;
          }
          tr:nth-child(even) {
            background-color: #f8f9fa;
          }
          .badge {
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 8pt;
            font-weight: bold;
            white-space: nowrap;
          }
          .badge-red { background-color: #dc3545; color: white; }
          .badge-orange { background-color: #fd7e14; color: white; }
          .badge-yellow { background-color: #ffc107; color: #000; }
          .badge-blue { background-color: #0dcaf0; color: white; }
          .badge-green { background-color: #28a745; color: white; }
          .badge-gray { background-color: #6c757d; color: white; }
          @media print {
            .no-print { display: none; }
          }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>${title}</h1>
          <p>Generated on ${currentDate}</p>
        </div>
        <div class="summary">
          <strong>Total Alerts:</strong> ${alerts.length} |
          <strong>Severity Breakdown:</strong>
          ${this.getSeveritySummary(alerts)}
        </div>
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Severity</th>
              <th>Type</th>
              <th>Title</th>
              <th>Risk</th>
              <th>User</th>
              <th>Detected</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            ${tableRows}
          </tbody>
        </table>
      </body>
      </html>
    `;
  }

  /**
   * Get severity summary for PDF
   */
  private getSeveritySummary(alerts: SecurityAlert[]): string {
    const counts = {
      EMERGENCY: 0,
      CRITICAL: 0,
      WARNING: 0,
      INFO: 0
    };

    alerts.forEach(alert => {
      const severity = alert.severityName.toUpperCase();
      if (severity in counts) {
        counts[severity as keyof typeof counts]++;
      }
    });

    return Object.entries(counts)
      .map(([key, value]) => `${key}: ${value}`)
      .join(' | ');
  }

  /**
   * Get CSS class for severity badge
   */
  private getSeverityClass(severity: string): string {
    const severityMap: Record<string, string> = {
      'EMERGENCY': 'red',
      'CRITICAL': 'orange',
      'WARNING': 'yellow',
      'INFO': 'blue'
    };
    return severityMap[severity.toUpperCase()] || 'gray';
  }

  /**
   * Get CSS class for status badge
   */
  private getStatusClass(status: string): string {
    const statusMap: Record<string, string> = {
      'NEW': 'red',
      'ACKNOWLEDGED': 'yellow',
      'IN_PROGRESS': 'blue',
      'IN PROGRESS': 'blue',
      'RESOLVED': 'green',
      'CLOSED': 'gray'
    };
    return statusMap[status.toUpperCase()] || 'gray';
  }

  /**
   * Escape HTML special characters
   */
  private escapeHTML(text: string): string {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Download file helper
   */
  private downloadFile(content: string, filename: string, mimeType: string): void {
    const blob = new Blob([content], { type: mimeType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
}
