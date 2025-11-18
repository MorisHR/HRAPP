import { Component } from '@angular/core';

import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { Divider } from '../../shared/ui';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    Divider
],
  template: `
    <div class="landing-page">
      <!-- Header / Navigation -->
      <header class="header">
        <div class="container">
          <div class="header-content">
            <div class="logo">
              <h1>MorisHR</h1>
              <span class="tagline">Enterprise HRMS for Mauritius</span>
            </div>
            <nav class="nav">
              <a href="#features" class="nav-link">Features</a>
              <a href="#compliance" class="nav-link">Compliance</a>
              <a href="#pricing" class="nav-link">Pricing</a>
              <a href="#contact" class="nav-link">Contact</a>
              <button mat-raised-button color="primary" (click)="navigateToLogin()">
                Employee Login
              </button>
            </nav>
          </div>
        </div>
      </header>

      <!-- Hero Section -->
      <section class="hero">
        <div class="container">
          <div class="hero-content">
            <h1 class="hero-title">
              Modern HRMS Built for<br />
              <span class="highlight">Mauritius Businesses</span>
            </h1>
            <p class="hero-subtitle">
              Streamline payroll, manage compliance, and empower your workforce with MorisHR—the complete HR management solution designed for Mauritius regulations.
            </p>
            <div class="hero-actions">
              <button mat-raised-button color="primary" class="cta-primary" (click)="navigateToLogin()">
                <mat-icon>login</mat-icon>
                Employee Login
              </button>
              <button mat-stroked-button class="cta-secondary" (click)="contactSales()">
                <mat-icon>mail</mat-icon>
                Contact Sales
              </button>
            </div>
            <p class="hero-note">Trusted by businesses across Mauritius for compliance-first HR management</p>
          </div>
        </div>
      </section>

      <!-- Features Section -->
      <section id="features" class="features">
        <div class="container">
          <div class="section-header">
            <h2>Everything You Need to Manage Your Workforce</h2>
            <p>Powerful features designed for the unique needs of Mauritius businesses</p>
          </div>

          <div class="features-grid">
            <mat-card class="feature-card">
              <mat-icon class="feature-icon">account_balance</mat-icon>
              <h3>Automated Payroll</h3>
              <p>Process payroll with automatic CSG, NSF, and PAYE calculations. Generate payslips and tax reports in minutes, not hours.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">schedule</mat-icon>
              <h3>Time & Attendance</h3>
              <p>Track employee hours, manage leave requests, and maintain accurate timesheets. Integrate with biometric systems seamlessly.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">receipt_long</mat-icon>
              <h3>Leave Management</h3>
              <p>Handle annual leave, sick leave, and public holidays according to Mauritius labour laws. Automated accruals and approvals.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">people</mat-icon>
              <h3>Employee Self-Service</h3>
              <p>Empower employees to view payslips, request leave, update personal information, and access HR documents anytime, anywhere.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">description</mat-icon>
              <h3>Compliance Reports</h3>
              <p>Generate MRA-compliant reports, CSG summaries, and audit trails. Stay audit-ready with automated compliance tracking.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">analytics</mat-icon>
              <h3>HR Analytics</h3>
              <p>Make data-driven decisions with workforce analytics, turnover reports, and payroll insights. Real-time dashboards for HR leaders.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">cloud</mat-icon>
              <h3>Cloud-Based Platform</h3>
              <p>Access your HR system from anywhere with enterprise-grade security. Automatic backups and 99.9% uptime guarantee.</p>
            </mat-card>

            <mat-card class="feature-card">
              <mat-icon class="feature-icon">support_agent</mat-icon>
              <h3>Local Support</h3>
              <p>Dedicated support team based in Mauritius. Get help in your timezone with experts who understand local HR requirements.</p>
            </mat-card>
          </div>
        </div>
      </section>

      <!-- Compliance Section -->
      <section id="compliance" class="compliance">
        <div class="container">
          <div class="compliance-content">
            <div class="compliance-text">
              <h2>Built for Mauritius Compliance</h2>
              <p class="lead">
                MorisHR is purpose-built to handle the unique requirements of Mauritius employment law and taxation.
              </p>

              <div class="compliance-features">
                <div class="compliance-item">
                  <mat-icon>check_circle</mat-icon>
                  <div>
                    <h4>CSG & NSF Contributions</h4>
                    <p>Automatic calculation and tracking of Contribution Sociale Généralisée and National Savings Fund deductions.</p>
                  </div>
                </div>

                <div class="compliance-item">
                  <mat-icon>check_circle</mat-icon>
                  <div>
                    <h4>PAYE Tax Calculations</h4>
                    <p>Up-to-date PAYE tax bands and automatic withholding calculations aligned with MRA requirements.</p>
                  </div>
                </div>

                <div class="compliance-item">
                  <mat-icon>check_circle</mat-icon>
                  <div>
                    <h4>MRA-Ready Reports</h4>
                    <p>Generate statutory reports in the exact format required by the Mauritius Revenue Authority.</p>
                  </div>
                </div>

                <div class="compliance-item">
                  <mat-icon>check_circle</mat-icon>
                  <div>
                    <h4>Labour Law Compliance</h4>
                    <p>Stay compliant with Workers' Rights Act, Employment Rights Act, and all applicable labour regulations.</p>
                  </div>
                </div>
              </div>
            </div>

            <div class="compliance-image">
              <mat-card class="compliance-card">
                <mat-icon class="compliance-badge">verified_user</mat-icon>
                <h3>100% Mauritius Compliant</h3>
                <p>Updated quarterly to reflect the latest regulatory changes</p>
              </mat-card>
            </div>
          </div>
        </div>
      </section>

      <!-- Pricing Section -->
      <section id="pricing" class="pricing">
        <div class="container">
          <div class="section-header">
            <h2>Transparent Pricing for Every Business Size</h2>
            <p>Choose the plan that fits your needs. All plans include full compliance features.</p>
          </div>

          <div class="pricing-grid">
            <mat-card class="pricing-card">
              <div class="pricing-header">
                <h3>Starter</h3>
                <div class="price">
                  <span class="currency">Rs</span>
                  <span class="amount">Contact Us</span>
                </div>
                <p class="billing">Per month</p>
              </div>
              <app-divider />
              <ul class="features-list">
                <li><mat-icon>check</mat-icon>Up to 50 employees</li>
                <li><mat-icon>check</mat-icon>Payroll & CSG/NSF/PAYE</li>
                <li><mat-icon>check</mat-icon>Leave management</li>
                <li><mat-icon>check</mat-icon>Employee self-service</li>
                <li><mat-icon>check</mat-icon>Basic reports</li>
                <li><mat-icon>check</mat-icon>Email support</li>
              </ul>
              <button mat-stroked-button class="pricing-cta" (click)="contactSales()">
                Contact Sales
              </button>
            </mat-card>

            <mat-card class="pricing-card featured">
              <div class="badge">Most Popular</div>
              <div class="pricing-header">
                <h3>Professional</h3>
                <div class="price">
                  <span class="currency">Rs</span>
                  <span class="amount">Contact Us</span>
                </div>
                <p class="billing">Per month</p>
              </div>
              <app-divider />
              <ul class="features-list">
                <li><mat-icon>check</mat-icon>Up to 200 employees</li>
                <li><mat-icon>check</mat-icon>Everything in Starter</li>
                <li><mat-icon>check</mat-icon>Time & attendance</li>
                <li><mat-icon>check</mat-icon>Advanced analytics</li>
                <li><mat-icon>check</mat-icon>API access</li>
                <li><mat-icon>check</mat-icon>Priority support</li>
                <li><mat-icon>check</mat-icon>Custom workflows</li>
              </ul>
              <button mat-raised-button color="primary" class="pricing-cta" (click)="contactSales()">
                Contact Sales
              </button>
            </mat-card>

            <mat-card class="pricing-card">
              <div class="pricing-header">
                <h3>Enterprise</h3>
                <div class="price">
                  <span class="currency">Rs</span>
                  <span class="amount">Custom</span>
                </div>
                <p class="billing">Contact us for pricing</p>
              </div>
              <app-divider />
              <ul class="features-list">
                <li><mat-icon>check</mat-icon>Unlimited employees</li>
                <li><mat-icon>check</mat-icon>Everything in Professional</li>
                <li><mat-icon>check</mat-icon>Multi-company support</li>
                <li><mat-icon>check</mat-icon>Advanced security</li>
                <li><mat-icon>check</mat-icon>Dedicated account manager</li>
                <li><mat-icon>check</mat-icon>SLA guarantee</li>
                <li><mat-icon>check</mat-icon>Custom integrations</li>
              </ul>
              <button mat-stroked-button class="pricing-cta" (click)="contactSales()">
                Contact Sales
              </button>
            </mat-card>
          </div>
        </div>
      </section>

      <!-- CTA Section -->
      <section id="contact" class="cta">
        <div class="container">
          <div class="cta-content">
            <h2>Ready to Transform Your HR Operations?</h2>
            <p>Join leading businesses in Mauritius using MorisHR to streamline their workforce management.</p>
            <div class="cta-actions">
              <button mat-raised-button class="cta-button" (click)="navigateToLogin()">
                <mat-icon>login</mat-icon>
                Employee Login
              </button>
              <button mat-stroked-button class="cta-button secondary" (click)="contactSales()">
                <mat-icon>mail</mat-icon>
                Contact Sales Team
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- Footer -->
      <footer class="footer">
        <div class="container">
          <div class="footer-content">
            <div class="footer-section">
              <h3>MorisHR</h3>
              <p>Enterprise HRMS built specifically for Mauritius businesses. Compliant, reliable, and easy to use.</p>
            </div>

            <div class="footer-section">
              <h4>Product</h4>
              <a href="#features">Features</a>
              <a href="#compliance">Compliance</a>
              <a href="#pricing">Pricing</a>
            </div>

            <div class="footer-section">
              <h4>Company</h4>
              <a href="#contact">Contact Us</a>
              <a href="#" (click)="contactSales()">Support</a>
            </div>

            <div class="footer-section">
              <h4>Legal</h4>
              <a href="#">Privacy Policy</a>
              <a href="#">Terms of Service</a>
              <a href="#">Security</a>
            </div>
          </div>

          <app-divider />

          <div class="footer-bottom">
            <p>&copy; 2025 MorisHR. All rights reserved. Made in Mauritius.</p>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [`
    .landing-page {
      width: 100%;
      min-height: 100vh;
      background: #ffffff;
      scroll-behavior: smooth;
    }

    /* Global smooth scrolling */
    html {
      scroll-behavior: smooth;
    }

    /* Container */
    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 24px;
    }

    /* Header */
    .header {
      position: sticky;
      top: 0;
      background: #ffffff;
      border-bottom: 1px solid #e0e0e0;
      z-index: 1000;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      height: 72px;
    }

    .logo h1 {
      margin: 0;
      font-size: 28px;
      font-weight: 600;
      color: #1a237e;
      line-height: 1;
    }

    .logo .tagline {
      font-size: 12px;
      color: #666;
      font-weight: 400;
    }

    .nav {
      display: flex;
      align-items: center;
      gap: 32px;
    }

    .nav-link {
      text-decoration: none;
      color: #424242;
      font-weight: 500;
      transition: color 0.3s ease, transform 0.2s;
      position: relative;
      padding-bottom: 4px;
    }

    .nav-link:hover {
      color: #1a237e;
    }

    .nav-link::after {
      content: '';
      position: absolute;
      width: 0;
      height: 2px;
      bottom: 0;
      left: 0;
      background-color: #1a237e;
      transition: width 0.3s ease;
    }

    .nav-link:hover::after {
      width: 100%;
    }

    /* Hero Section */
    .hero {
      background: linear-gradient(135deg, #f5f7fa 0%, #ffffff 100%);
      padding: 80px 0 100px;
    }

    .hero-content {
      text-align: center;
      max-width: 800px;
      margin: 0 auto;
    }

    .hero-title {
      font-size: 56px;
      font-weight: 700;
      line-height: 1.1;
      color: #212121;
      margin: 0 0 24px;
    }

    .hero-title .highlight {
      color: #1a237e;
    }

    .hero-subtitle {
      font-size: 20px;
      line-height: 1.6;
      color: #616161;
      margin: 0 0 40px;
    }

    .hero-actions {
      display: flex;
      gap: 16px;
      justify-content: center;
      margin-bottom: 24px;
    }

    .cta-primary,
    .cta-secondary {
      height: 56px;
      padding: 0 32px;
      font-size: 16px;
      font-weight: 500;
      border-radius: 4px;
      transition: all 0.3s ease;
      cursor: pointer;
    }

    .cta-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 24px rgba(26, 35, 126, 0.3) !important;
    }

    .cta-secondary {
      background: #ffffff;
      border: 2px solid #1a237e;
      color: #1a237e;
    }

    .cta-secondary:hover {
      background: #f5f5f5;
      transform: translateY(-2px);
      box-shadow: 0 8px 24px rgba(26, 35, 126, 0.2);
    }

    .hero-note {
      color: #757575;
      font-size: 14px;
      margin: 0;
    }

    /* Features Section */
    .features {
      padding: 80px 0;
      background: #ffffff;
    }

    .section-header {
      text-align: center;
      margin-bottom: 64px;
    }

    .section-header h2 {
      font-size: 42px;
      font-weight: 700;
      color: #212121;
      margin: 0 0 16px;
    }

    .section-header p {
      font-size: 18px;
      color: #616161;
      margin: 0;
    }

    .features-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 24px;
    }

    .feature-card {
      padding: 32px;
      text-align: center;
      transition: transform 0.2s, box-shadow 0.2s;
      border: 1px solid #e0e0e0;
    }

    .feature-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
    }

    .feature-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #1a237e;
      margin-bottom: 16px;
    }

    .feature-card h3 {
      font-size: 20px;
      font-weight: 600;
      color: #212121;
      margin: 0 0 12px;
    }

    .feature-card p {
      font-size: 14px;
      line-height: 1.6;
      color: #616161;
      margin: 0;
    }

    /* Compliance Section */
    .compliance {
      padding: 80px 0;
      background: #f5f7fa;
    }

    .compliance-content {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 64px;
      align-items: center;
    }

    .compliance-text h2 {
      font-size: 42px;
      font-weight: 700;
      color: #212121;
      margin: 0 0 16px;
    }

    .compliance-text .lead {
      font-size: 18px;
      color: #616161;
      margin: 0 0 32px;
    }

    .compliance-features {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .compliance-item {
      display: flex;
      gap: 16px;
    }

    .compliance-item mat-icon {
      color: #4caf50;
      flex-shrink: 0;
    }

    .compliance-item h4 {
      font-size: 18px;
      font-weight: 600;
      color: #212121;
      margin: 0 0 8px;
    }

    .compliance-item p {
      font-size: 14px;
      color: #616161;
      margin: 0;
    }

    .compliance-card {
      padding: 48px;
      text-align: center;
      background: linear-gradient(135deg, #1a237e 0%, #3949ab 100%);
      color: #ffffff !important;
      box-shadow: 0 8px 24px rgba(26, 35, 126, 0.3);
    }

    .compliance-badge {
      font-size: 72px;
      width: 72px;
      height: 72px;
      margin-bottom: 16px;
      color: #ffffff !important;
    }

    .compliance-card h3 {
      font-size: 24px;
      font-weight: 600;
      margin: 0 0 12px;
      color: #ffffff !important;
    }

    .compliance-card p {
      font-size: 14px;
      margin: 0;
      color: rgba(255, 255, 255, 0.95) !important;
      line-height: 1.5;
    }

    /* Pricing Section */
    .pricing {
      padding: 80px 0;
      background: #ffffff;
    }

    .pricing-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 32px;
      max-width: 1000px;
      margin: 0 auto;
    }

    .pricing-card {
      padding: 40px 32px;
      position: relative;
      border: 1px solid #e0e0e0;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .pricing-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
    }

    .pricing-card.featured {
      border: 2px solid #1a237e;
      transform: scale(1.05);
    }

    .pricing-card.featured:hover {
      transform: scale(1.05) translateY(-4px);
    }

    .pricing-card .badge {
      position: absolute;
      top: -12px;
      left: 50%;
      transform: translateX(-50%);
      background: #1a237e;
      color: #ffffff;
      padding: 4px 16px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
    }

    .pricing-header {
      text-align: center;
      margin-bottom: 24px;
    }

    .pricing-header h3 {
      font-size: 24px;
      font-weight: 600;
      color: #212121;
      margin: 0 0 16px;
    }

    .price {
      display: flex;
      align-items: baseline;
      justify-content: center;
      gap: 4px;
      margin-bottom: 8px;
    }

    .price .currency {
      font-size: 20px;
      color: #616161;
    }

    .price .amount {
      font-size: 48px;
      font-weight: 700;
      color: #212121;
    }

    .billing {
      font-size: 14px;
      color: #757575;
      margin: 0;
    }

    .features-list {
      list-style: none;
      padding: 24px 0;
      margin: 0;
    }

    .features-list li {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 0;
      font-size: 14px;
      color: #424242;
    }

    .features-list mat-icon {
      color: #4caf50;
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .pricing-cta {
      width: 100%;
      height: 48px;
      margin-top: 24px;
    }

    /* CTA Section */
    .cta {
      padding: 80px 0;
      background: linear-gradient(135deg, #1a237e 0%, #3949ab 100%);
      color: #ffffff;
    }

    .cta-content {
      text-align: center;
      max-width: 700px;
      margin: 0 auto;
    }

    .cta-content h2 {
      font-size: 42px;
      font-weight: 700;
      margin: 0 0 16px;
      color: #ffffff;
    }

    .cta-content p {
      font-size: 18px;
      margin: 0 0 40px;
      color: rgba(255, 255, 255, 0.95);
    }

    .cta-actions {
      display: flex;
      gap: 16px;
      justify-content: center;
    }

    .cta-button {
      height: 56px;
      padding: 0 32px !important;
      font-size: 16px;
      font-weight: 500;
      border-radius: 4px;
      transition: all 0.3s ease;
    }

    /* Primary CTA button - solid white background */
    .cta-actions .mat-mdc-raised-button {
      background-color: #ffffff !important;
      color: #1a237e !important;
    }

    .cta-actions .mat-mdc-raised-button:hover {
      background-color: #f5f5f5 !important;
      transform: translateY(-2px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2) !important;
    }

    /* Secondary CTA button - outlined white */
    .cta-actions .mat-mdc-outlined-button,
    .cta-actions button[mat-stroked-button] {
      border: 2px solid #ffffff !important;
      color: #ffffff !important;
      background-color: transparent !important;
    }

    .cta-actions .mat-mdc-outlined-button:hover,
    .cta-actions button[mat-stroked-button]:hover {
      background-color: rgba(255, 255, 255, 0.1) !important;
      transform: translateY(-2px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
    }

    .cta-actions .mat-mdc-button mat-icon,
    .cta-actions .mat-mdc-raised-button mat-icon,
    .cta-actions .mat-mdc-outlined-button mat-icon,
    .cta-actions button mat-icon {
      margin-right: 8px;
    }

    /* Ensure button text is visible on dark background */
    .cta-actions button[mat-stroked-button] .mdc-button__label {
      color: #ffffff !important;
    }

    /* Footer */
    .footer {
      padding: 64px 0 32px;
      background: #212121;
      color: #ffffff;
    }

    .footer-content {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr 1fr;
      gap: 48px;
      margin-bottom: 48px;
    }

    .footer-section h3 {
      font-size: 24px;
      font-weight: 600;
      margin: 0 0 16px;
      color: #ffffff;
    }

    .footer-section h4 {
      font-size: 16px;
      font-weight: 600;
      margin: 0 0 16px;
      color: #ffffff;
    }

    .footer-section p {
      color: #b0b0b0;
      line-height: 1.6;
      margin: 0;
    }

    .footer-section a {
      display: block;
      color: #b0b0b0;
      text-decoration: none;
      margin-bottom: 12px;
      transition: color 0.2s;
    }

    .footer-section a:hover {
      color: #ffffff;
    }

    .footer-bottom {
      text-align: center;
      padding-top: 32px;
    }

    .footer-bottom p {
      color: #757575;
      margin: 0;
      font-size: 14px;
    }

    /* Responsive Design */
    @media (max-width: 768px) {
      /* Hide desktop navigation on mobile */
      .nav {
        display: none;
      }

      /* Mobile header adjustments */
      .header-content {
        padding: 12px 0;
      }

      .logo h1 {
        font-size: 24px;
      }

      .logo .tagline {
        font-size: 11px;
      }

      /* Hero section mobile optimizations */
      .hero {
        padding: 60px 0;
      }

      .hero-title {
        font-size: 36px;
        line-height: 1.2;
      }

      .hero-subtitle {
        font-size: 16px;
        line-height: 1.5;
      }

      .hero-actions {
        flex-direction: column;
        width: 100%;
      }

      .cta-primary,
      .cta-secondary {
        width: 100%;
      }

      /* Section headers mobile */
      .section-header h2 {
        font-size: 32px;
      }

      .section-header p {
        font-size: 16px;
      }

      /* Features grid mobile */
      .features-grid {
        grid-template-columns: 1fr;
      }

      /* Compliance section mobile */
      .compliance-content {
        grid-template-columns: 1fr;
        gap: 32px;
      }

      .compliance-text h2 {
        font-size: 32px;
      }

      /* Pricing grid mobile */
      .pricing-grid {
        grid-template-columns: 1fr;
      }

      .pricing-card.featured {
        transform: scale(1);
      }

      /* CTA section mobile */
      .cta {
        padding: 60px 0;
      }

      .cta-content h2 {
        font-size: 32px;
      }

      .cta-content p {
        font-size: 16px;
      }

      .cta-actions {
        flex-direction: column;
        width: 100%;
      }

      .cta-button {
        width: 100%;
      }

      /* Footer mobile */
      .footer-content {
        grid-template-columns: 1fr;
        gap: 32px;
      }

      /* Reduce padding for mobile */
      .container {
        padding: 0 16px;
      }
    }

    /* Tablet optimizations */
    @media (min-width: 769px) and (max-width: 1024px) {
      .container {
        padding: 0 32px;
      }

      .features-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .pricing-grid {
        grid-template-columns: 1fr;
        max-width: 600px;
      }
    }
  `]
})
export class LandingPageComponent {
  constructor(private router: Router) {}

  navigateToLogin(): void {
    this.router.navigate(['/auth/subdomain']);
  }

  contactSales(): void {
    window.location.href = 'mailto:sales@morishr.mu?subject=MorisHR Inquiry';
  }
}
