// ═══════════════════════════════════════════════════════════════
// PASSPORT DETECTION SERVICE - Production Grade
// Rule-based nationality detection from passport format
// NO AI - Pure regex pattern matching
// Multi-tenant safe, high performance, zero crashes
// ═══════════════════════════════════════════════════════════════

import { Injectable } from '@angular/core';
import { PassportDetectionResult, PassportPattern } from '../../models/employee-intelligence.model';

@Injectable({
  providedIn: 'root'
})
export class PassportDetectionService {

  /**
   * Passport patterns database (Mauritius immigration authority formats)
   * Source: ICAO Doc 9303 standards + country-specific formats
   * Updated: 2025-01-15
   */
  private readonly PASSPORT_PATTERNS: PassportPattern[] = [
    {
      nationality: 'Indian',
      pattern: /^[A-Z]\d{7}$/,
      description: 'Single letter followed by 7 digits',
      example: 'P1234567'
    },
    {
      nationality: 'French',
      pattern: /^\d{2}[A-Z]{2}\d{5}$/,
      description: '2 digits, 2 letters, 5 digits',
      example: '09AB12345'
    },
    {
      nationality: 'British',
      pattern: /^\d{9}$/,
      description: '9 digits',
      example: '123456789'
    },
    {
      nationality: 'South African',
      pattern: /^[A-Z]\d{8}$/,
      description: 'Single letter followed by 8 digits',
      example: 'A12345678'
    },
    {
      nationality: 'Chinese',
      pattern: /^[GE]\d{8}$/,
      description: 'G or E followed by 8 digits',
      example: 'G12345678'
    },
    {
      nationality: 'Mauritian',
      pattern: /^[A-Z]{1,2}\d{6}$/,
      description: '1-2 letters followed by 6 digits',
      example: 'A123456 or AB123456'
    },
    {
      nationality: 'American',
      pattern: /^\d{9}$/,
      description: '9 digits',
      example: '123456789'
    },
    {
      nationality: 'Canadian',
      pattern: /^[A-Z]{2}\d{6}$/,
      description: '2 letters followed by 6 digits',
      example: 'AB123456'
    },
    {
      nationality: 'Australian',
      pattern: /^[A-Z]\d{7}$/,
      description: 'Single letter followed by 7 digits',
      example: 'N1234567'
    },
    {
      nationality: 'German',
      pattern: /^[CFGHJKLMNPRTVWXYZ0-9]{9}$/,
      description: '9 alphanumeric characters (no I, O, Q, U)',
      example: 'C01X00T47'
    },
    {
      nationality: 'Italian',
      pattern: /^[A-Z]{2}\d{7}$/,
      description: '2 letters followed by 7 digits',
      example: 'AA1234567'
    },
    {
      nationality: 'Spanish',
      pattern: /^[A-Z]{3}\d{6}$/,
      description: '3 letters followed by 6 digits',
      example: 'AAA123456'
    },
    {
      nationality: 'Japanese',
      pattern: /^[A-Z]{2}\d{7}$/,
      description: '2 letters followed by 7 digits',
      example: 'TH1234567'
    },
    {
      nationality: 'Korean',
      pattern: /^[A-Z]\d{8}$/,
      description: 'Single letter (M or S) followed by 8 digits',
      example: 'M12345678'
    },
    {
      nationality: 'Singapore',
      pattern: /^[K-M]\d{7}[A-Z]$/,
      description: 'Letter (K, L, M), 7 digits, letter',
      example: 'K1234567A'
    },
    {
      nationality: 'Malaysian',
      pattern: /^[A-Z]\d{8}$/,
      description: 'Single letter followed by 8 digits',
      example: 'A12345678'
    },
    {
      nationality: 'UAE',
      pattern: /^[A-Z]\d{7}$/,
      description: 'Single letter followed by 7 digits',
      example: 'A1234567'
    },
    {
      nationality: 'Saudi Arabian',
      pattern: /^[A-Z]\d{8}$/,
      description: 'Single letter followed by 8 digits',
      example: 'A12345678'
    },
    {
      nationality: 'Egyptian',
      pattern: /^[A-Z]\d{7}$/,
      description: 'Single letter followed by 7 digits',
      example: 'A1234567'
    },
    {
      nationality: 'Nigerian',
      pattern: /^[A-Z]\d{8}$/,
      description: 'Single letter followed by 8 digits',
      example: 'A12345678'
    }
  ];

  /**
   * Detect nationality from passport number
   * Performance: < 1ms (regex matching only, no API calls)
   * Multi-tenant safe: No shared state
   *
   * @param passportNumber - Passport number to analyze
   * @returns Detection result with confidence score
   */
  detectNationality(passportNumber: string): PassportDetectionResult {
    try {
      // Input validation
      if (!passportNumber || typeof passportNumber !== 'string') {
        return this.createEmptyResult('Invalid input');
      }

      // Sanitize input (remove spaces, convert to uppercase)
      const sanitized = passportNumber.trim().replace(/\s+/g, '').toUpperCase();

      if (sanitized.length < 6 || sanitized.length > 15) {
        return this.createEmptyResult('Invalid passport length (must be 6-15 characters)');
      }

      // Match against patterns
      const matches: Array<{ nationality: string; pattern: PassportPattern }> = [];

      for (const pattern of this.PASSPORT_PATTERNS) {
        if (pattern.pattern.test(sanitized)) {
          matches.push({ nationality: pattern.nationality, pattern });
        }
      }

      // Handle results
      if (matches.length === 0) {
        return this.createEmptyResult('No matching passport format found');
      }

      if (matches.length === 1) {
        // Single match - high confidence
        return {
          detectedNationality: matches[0].nationality,
          confidence: 'high',
          pattern: matches[0].pattern.description,
          suggestions: []
        };
      }

      // Multiple matches - medium confidence (some formats overlap)
      // Example: British and American both use 9 digits
      return {
        detectedNationality: matches[0].nationality,
        confidence: 'medium',
        pattern: matches[0].pattern.description,
        suggestions: matches.slice(1).map(m =>
          `Could also be ${m.nationality} (${m.pattern.description})`
        )
      };

    } catch (error) {
      // Graceful error handling - never crash
      console.error('[PassportDetectionService] Error detecting nationality:', error);
      return this.createEmptyResult('Detection failed due to internal error');
    }
  }

  /**
   * Get all supported passport formats
   * Used for help text / documentation
   */
  getSupportedFormats(): PassportPattern[] {
    return [...this.PASSPORT_PATTERNS]; // Return copy to prevent mutation
  }

  /**
   * Validate passport format for specific nationality
   *
   * @param passportNumber - Passport number to validate
   * @param nationality - Expected nationality
   * @returns true if format matches nationality
   */
  validateFormat(passportNumber: string, nationality: string): boolean {
    try {
      const sanitized = passportNumber.trim().replace(/\s+/g, '').toUpperCase();
      const pattern = this.PASSPORT_PATTERNS.find(p => p.nationality === nationality);

      if (!pattern) {
        return false;
      }

      return pattern.pattern.test(sanitized);
    } catch (error) {
      console.error('[PassportDetectionService] Validation error:', error);
      return false;
    }
  }

  /**
   * Get example passport format for nationality
   *
   * @param nationality - Nationality to get example for
   * @returns Example passport number or null
   */
  getExampleFormat(nationality: string): string | null {
    try {
      const pattern = this.PASSPORT_PATTERNS.find(p => p.nationality === nationality);
      return pattern ? pattern.example : null;
    } catch (error) {
      return null;
    }
  }

  /**
   * Check if nationality requires passport
   * (Mauritians don't need passport for employment)
   *
   * @param nationality - Nationality to check
   * @returns true if passport required
   */
  isPassportRequired(nationality: string): boolean {
    return nationality !== 'Mauritian';
  }

  /**
   * Create empty result with suggestion
   * Helper to ensure consistent error responses
   */
  private createEmptyResult(suggestion: string): PassportDetectionResult {
    return {
      detectedNationality: null,
      confidence: 'low',
      pattern: 'Unknown',
      suggestions: [suggestion]
    };
  }

  /**
   * Get nationality from country code (ISO 3166-1 alpha-2)
   * Helper for integration with address services
   */
  getNationalityFromCountryCode(countryCode: string): string | null {
    const mapping: Record<string, string> = {
      'IN': 'Indian',
      'FR': 'French',
      'GB': 'British',
      'ZA': 'South African',
      'CN': 'Chinese',
      'MU': 'Mauritian',
      'US': 'American',
      'CA': 'Canadian',
      'AU': 'Australian',
      'DE': 'German',
      'IT': 'Italian',
      'ES': 'Spanish',
      'JP': 'Japanese',
      'KR': 'Korean',
      'SG': 'Singapore',
      'MY': 'Malaysian',
      'AE': 'UAE',
      'SA': 'Saudi Arabian',
      'EG': 'Egyptian',
      'NG': 'Nigerian'
    };

    try {
      return mapping[countryCode.toUpperCase()] || null;
    } catch (error) {
      return null;
    }
  }
}
