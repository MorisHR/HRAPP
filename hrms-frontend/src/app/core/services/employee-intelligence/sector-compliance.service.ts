// ═══════════════════════════════════════════════════════════════
// SECTOR COMPLIANCE SERVICE - Production Grade
// Sector-specific rules validation
// NO AI - Business rules engine
// Multi-tenant safe, zero crashes
// ═══════════════════════════════════════════════════════════════

import { Injectable } from '@angular/core';
import { SectorRules, SectorComplianceResult, ComplianceWarning, ComplianceRequirement } from '../../models/employee-intelligence.model';

@Injectable({
  providedIn: 'root'
})
export class SectorComplianceService {

  private readonly SECTOR_RULES_DB: Record<string, SectorRules> = {
    'EPZ': {
      sectorName: 'Export Processing Zone',
      sectorCode: 'EPZ',
      corporateTaxRate: 3,
      expatQuotaPercent: 60,
      minimumSalary: null,
      specialBenefits: [
        'Corporate tax: 3% (vs 15% standard)',
        'Import duty exemption on raw materials',
        'VAT zero-rated on exports',
        'Reduced employer CSG: 6% (vs 9%)',
        'No minimum salary requirement'
      ],
      restrictions: [
        'Must export 70%+ of production',
        'Cannot sell > 30% locally',
        'Quarterly export reports mandatory',
        'Annual EDB audit required'
      ],
      requiredLicenses: ['EDB registration certificate', 'Export license'],
      complianceChecks: ['Verify EDB certificate valid', 'Check export quota compliance', 'Validate skills transfer plan'],
      regulatoryBodies: ['Economic Development Board (EDB)', 'Mauritius Revenue Authority (MRA)']
    },
    'Financial Services': {
      sectorName: 'Financial Services',
      sectorCode: 'FIN',
      corporateTaxRate: 15,
      expatQuotaPercent: 50,
      minimumSalary: 60000,
      specialBenefits: [
        'Global Business License incentives',
        'Double tax treaty access',
        'Repatriation of profits allowed',
        'Substance requirements waiver (if conditions met)'
      ],
      restrictions: [
        'FSC license mandatory',
        'Fit & proper test required for directors',
        'AML/CFT compliance mandatory',
        'Annual audited financials required'
      ],
      requiredLicenses: ['FSC license', 'Category 1/2 Global Business License'],
      complianceChecks: ['FSC license active', 'Fit & proper approval', 'AML training completed', 'Substance test compliance'],
      regulatoryBodies: ['Financial Services Commission (FSC)', 'MRA', 'Bank of Mauritius']
    },
    'ICT': {
      sectorName: 'Information & Communication Technology',
      sectorCode: 'ICT',
      corporateTaxRate: 15,
      expatQuotaPercent: null,
      minimumSalary: 60000,
      specialBenefits: [
        'No expatriate quota limit',
        'Fast-track work permits (3-5 days)',
        'Tax holidays for approved projects',
        'R&D incentives available'
      ],
      restrictions: [],
      requiredLicenses: ['ICTA registration'],
      complianceChecks: ['Verify ICTA registration', 'IP protection compliance'],
      regulatoryBodies: ['Information and Communication Technologies Authority (ICTA)']
    },
    'Tourism': {
      sectorName: 'Tourism & Hospitality',
      sectorCode: 'TOUR',
      corporateTaxRate: 15,
      expatQuotaPercent: 40,
      minimumSalary: 45000,
      specialBenefits: [
        'Seasonal work permits available',
        'MTPA marketing support',
        'Tourism development incentives'
      ],
      restrictions: [
        'MTPA registration required',
        'Expatriate quota strictly enforced (40%)',
        'Skills transfer mandatory'
      ],
      requiredLicenses: ['MTPA tourism license', 'Hotel/restaurant license'],
      complianceChecks: ['MTPA license valid', 'Expatriate quota compliance', 'Health & safety certification'],
      regulatoryBodies: ['Mauritius Tourism Promotion Authority (MTPA)', 'Ministry of Tourism']
    },
    'Manufacturing': {
      sectorName: 'Manufacturing',
      sectorCode: 'MFG',
      corporateTaxRate: 15,
      expatQuotaPercent: 50,
      minimumSalary: 40000,
      specialBenefits: [
        'Investment incentives available',
        'Accelerated depreciation',
        'Export credits'
      ],
      restrictions: [
        'Environmental compliance required',
        'Factory inspection mandatory',
        'Labour law compliance (45h work week)'
      ],
      requiredLicenses: ['Factory license', 'Environmental permit'],
      complianceChecks: ['Factory license valid', 'Environmental clearance', 'Safety compliance'],
      regulatoryBodies: ['Ministry of Industry', 'EDB', 'Environment Ministry']
    }
  };

  /**
   * Validate sector compliance
   * Performance: < 10ms
   * Multi-tenant safe: Stateless
   */
  validateCompliance(
    sector: string,
    salary: number,
    expatPercent: number,
    hasLicense: boolean = false
  ): SectorComplianceResult {
    try {
      const rules = this.findSectorRules(sector);

      if (!rules) {
        return this.createUnknownSectorResult();
      }

      const warnings = this.checkWarnings(rules, salary, expatPercent, hasLicense);
      const requirements = this.generateRequirements(rules);
      const score = this.calculateScore(warnings);
      const taxSavings = this.estimateTaxSavings(rules);

      return {
        valid: warnings.filter(w => w.severity === 'error').length === 0,
        score,
        warnings,
        requirements,
        benefits: rules.specialBenefits,
        estimatedTaxSavings: taxSavings
      };

    } catch (error) {
      console.error('[SectorComplianceService] Validation error:', error);
      return this.createErrorResult();
    }
  }

  /**
   * Get sector rules
   */
  getSectorRules(sector: string): SectorRules | null {
    return this.findSectorRules(sector);
  }

  /**
   * Get all supported sectors
   */
  getAllSectors(): SectorRules[] {
    return Object.values(this.SECTOR_RULES_DB);
  }

  // Private methods

  private findSectorRules(sector: string): SectorRules | null {
    const normalized = sector.toLowerCase();

    for (const [key, rules] of Object.entries(this.SECTOR_RULES_DB)) {
      if (normalized.includes(key.toLowerCase()) ||
          normalized.includes(rules.sectorName.toLowerCase())) {
        return rules;
      }
    }

    return null;
  }

  private checkWarnings(
    rules: SectorRules,
    salary: number,
    expatPercent: number,
    hasLicense: boolean
  ): ComplianceWarning[] {
    const warnings: ComplianceWarning[] = [];

    // Check minimum salary
    if (rules.minimumSalary && salary < rules.minimumSalary) {
      warnings.push({
        severity: 'error',
        message: `Salary (MUR ${salary}) below sector minimum (MUR ${rules.minimumSalary})`,
        field: 'baseSalary',
        resolution: `Increase salary to at least MUR ${rules.minimumSalary}`
      });
    }

    // Check expat quota
    if (rules.expatQuotaPercent && expatPercent > rules.expatQuotaPercent) {
      warnings.push({
        severity: 'error',
        message: `Expatriate percentage (${expatPercent}%) exceeds quota (${rules.expatQuotaPercent}%)`,
        field: 'employeeType',
        resolution: `Ensure workforce has max ${rules.expatQuotaPercent}% expatriates`
      });
    }

    // Check required licenses
    if (!hasLicense && rules.requiredLicenses.length > 0) {
      warnings.push({
        severity: 'warning',
        message: `Required licenses not uploaded: ${rules.requiredLicenses.join(', ')}`,
        resolution: 'Upload required licenses before submission'
      });
    }

    return warnings;
  }

  private generateRequirements(rules: SectorRules): ComplianceRequirement[] {
    return [
      ...rules.requiredLicenses.map(license => ({
        name: license,
        description: `Obtain and maintain ${license}`,
        mandatory: true,
        status: 'pending' as const
      })),
      ...rules.complianceChecks.map(check => ({
        name: check,
        description: check,
        mandatory: true,
        status: 'pending' as const
      }))
    ];
  }

  private calculateScore(warnings: ComplianceWarning[]): number {
    const errorCount = warnings.filter(w => w.severity === 'error').length;
    const warningCount = warnings.filter(w => w.severity === 'warning').length;

    return Math.max(0, 100 - (errorCount * 25) - (warningCount * 10));
  }

  private estimateTaxSavings(rules: SectorRules): number {
    const standardRate = 15;
    const savings = standardRate - rules.corporateTaxRate;
    return savings * 100000; // Example: MUR 100K salary
  }

  private createUnknownSectorResult(): SectorComplianceResult {
    return {
      valid: false,
      score: 0,
      warnings: [{
        severity: 'warning',
        message: 'Sector not found in compliance database',
        resolution: 'Select a valid sector from the dropdown'
      }],
      requirements: [],
      benefits: [],
      estimatedTaxSavings: 0
    };
  }

  private createErrorResult(): SectorComplianceResult {
    return {
      valid: false,
      score: 0,
      warnings: [{
        severity: 'error',
        message: 'Compliance check failed',
        resolution: 'Please retry'
      }],
      requirements: [],
      benefits: [],
      estimatedTaxSavings: 0
    };
  }
}
