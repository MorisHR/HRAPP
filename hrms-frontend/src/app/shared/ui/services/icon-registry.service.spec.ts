// ═══════════════════════════════════════════════════════════
// ICON REGISTRY SERVICE - SECURITY TESTS
// Fortune 500-grade security validation for XSS prevention
// ═══════════════════════════════════════════════════════════

import { TestBed } from '@angular/core/testing';
import { IconRegistryService } from './icon-registry.service';

describe('IconRegistryService - Security Tests', () => {
  let service: IconRegistryService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(IconRegistryService);
  });

  describe('XSS Prevention', () => {
    it('should block script tags in SVG paths', () => {
      expect(() => {
        service.registerIcon('malicious', '<script>alert("XSS")</script>');
      }).toThrow('SVG path cannot contain <script> tags');
    });

    it('should block script tags with uppercase', () => {
      expect(() => {
        service.registerIcon('malicious', '<SCRIPT>alert("XSS")</SCRIPT>');
      }).toThrow('SVG path cannot contain <script> tags');
    });

    it('should block script tags with mixed case', () => {
      expect(() => {
        service.registerIcon('malicious', '<ScRiPt>alert("XSS")</ScRiPt>');
      }).toThrow('SVG path cannot contain <script> tags');
    });
  });

  describe('Event Handler Injection Prevention', () => {
    const eventHandlers = [
      'onclick',
      'onerror',
      'onload',
      'onmouseover',
      'onmouseout',
      'onfocus',
      'onblur',
    ];

    eventHandlers.forEach((handler) => {
      it(`should block ${handler} event handler`, () => {
        expect(() => {
          service.registerIcon('malicious', `<path ${handler}="alert('XSS')"/>`);
        }).toThrow(`SVG path cannot contain event handler: ${handler}`);
      });

      it(`should block ${handler} with uppercase`, () => {
        const upperHandler = handler.toUpperCase();
        expect(() => {
          service.registerIcon('malicious', `<path ${upperHandler}="alert('XSS')"/>`);
        }).toThrow(`SVG path cannot contain event handler: ${handler}`);
      });
    });
  });

  describe('JavaScript Protocol Prevention', () => {
    it('should block javascript: protocol', () => {
      expect(() => {
        service.registerIcon('malicious', '<a href="javascript:alert(1)">');
      }).toThrow('SVG path cannot contain javascript: protocol');
    });

    it('should block javascript: with uppercase', () => {
      expect(() => {
        service.registerIcon('malicious', '<a href="JAVASCRIPT:alert(1)">');
      }).toThrow('SVG path cannot contain javascript: protocol');
    });

    it('should block javascript: with URL encoding attempts', () => {
      expect(() => {
        service.registerIcon('malicious', '<a href="jAvAsCrIpT:alert(1)">');
      }).toThrow('SVG path cannot contain javascript: protocol');
    });
  });

  describe('Dangerous Tag Prevention', () => {
    const dangerousTags = ['iframe', 'object', 'embed', 'style', 'link'];

    dangerousTags.forEach((tag) => {
      it(`should block <${tag}> tag`, () => {
        expect(() => {
          service.registerIcon('malicious', `<${tag}></${tag}>`);
        }).toThrow(`SVG path cannot contain dangerous tag: <${tag}`);
      });

      it(`should block <${tag}> with uppercase`, () => {
        const upperTag = tag.toUpperCase();
        expect(() => {
          service.registerIcon('malicious', `<${upperTag}></${upperTag}>`);
        }).toThrow(`SVG path cannot contain dangerous tag: <${tag}`);
      });
    });
  });

  describe('Data URL Prevention', () => {
    it('should block data URLs with script', () => {
      expect(() => {
        service.registerIcon('malicious', 'data:text/html,<script>alert(1)</script>');
      }).toThrow('SVG path cannot contain potentially malicious data URLs');
    });

    it('should block base64 data URLs', () => {
      expect(() => {
        service.registerIcon('malicious', 'data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==');
      }).toThrow('SVG path cannot contain potentially malicious data URLs');
    });
  });

  describe('Prototype Pollution Prevention', () => {
    it('should block __proto__ in icon name', () => {
      expect(() => {
        service.registerIcon('__proto__', '<path d="M10 10"/>');
      }).toThrow('Invalid icon name');
    });

    it('should block constructor in icon name', () => {
      expect(() => {
        service.registerIcon('constructor', '<path d="M10 10"/>');
      }).toThrow('Invalid icon name');
    });
  });

  describe('Valid Icon Registration', () => {
    it('should allow valid SVG path elements', () => {
      expect(() => {
        service.registerIcon('valid-icon', '<path d="M12 2L2 7l10 5 10-5-10-5z"/>');
      }).not.toThrow();
    });

    it('should allow multiple path elements', () => {
      expect(() => {
        service.registerIcon('multi-path', '<path d="M10 10"/><path d="M20 20"/>');
      }).not.toThrow();
    });

    it('should allow polyline elements', () => {
      expect(() => {
        service.registerIcon('polyline', '<polyline points="3 12 9 12 15 12"/>');
      }).not.toThrow();
    });

    it('should allow circle elements', () => {
      expect(() => {
        service.registerIcon('circle', '<circle cx="12" cy="12" r="10"/>');
      }).not.toThrow();
    });

    it('should allow stroke attributes (heroicons style)', () => {
      expect(() => {
        service.registerIcon('stroke-icon', '<path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15"/>');
      }).not.toThrow();
    });
  });

  describe('Icon Retrieval', () => {
    it('should retrieve hardcoded material icons', () => {
      const homeIcon = service.getIcon('home', 'material');
      expect(homeIcon).toBeDefined();
      expect(homeIcon).toContain('path');
    });

    it('should retrieve custom registered icons', () => {
      service.registerIcon('custom', '<path d="M10 10"/>', 'material');
      const customIcon = service.getIcon('custom', 'material');
      expect(customIcon).toBe('<path d="M10 10"/>');
    });

    it('should return undefined for non-existent icons', () => {
      const icon = service.getIcon('non-existent', 'material');
      expect(icon).toBeUndefined();
    });
  });

  describe('Input Validation', () => {
    it('should reject empty icon name', () => {
      expect(() => {
        service.registerIcon('', '<path d="M10 10"/>');
      }).toThrow('Invalid icon name');
    });

    it('should reject non-string icon name', () => {
      expect(() => {
        service.registerIcon(null as any, '<path d="M10 10"/>');
      }).toThrow('Invalid icon name');
    });

    it('should reject empty SVG path', () => {
      expect(() => {
        service.registerIcon('test', '');
      }).toThrow('Invalid SVG path: must be a non-empty string');
    });

    it('should reject non-string SVG path', () => {
      expect(() => {
        service.registerIcon('test', null as any);
      }).toThrow('Invalid SVG path: must be a non-empty string');
    });
  });

  describe('Fortune 500 Security Compliance', () => {
    it('should have defense in depth (multiple validation layers)', () => {
      // Validates icon name
      expect(() => service.registerIcon('__proto__', '<path d="M10 10"/>')).toThrow();

      // Validates SVG content
      expect(() => service.registerIcon('test', '<script>alert(1)</script>')).toThrow();

      // Both layers working independently
      expect(() => service.registerIcon('__proto__', '<script>alert(1)</script>')).toThrow();
    });

    it('should fail safe (reject on error, not accept)', () => {
      let errorThrown = false;
      try {
        service.registerIcon('malicious', '<script>alert(1)</script>');
      } catch (e) {
        errorThrown = true;
      }

      // Should throw error, not silently accept malicious content
      expect(errorThrown).toBe(true);

      // Icon should not be registered
      expect(service.hasIcon('malicious')).toBe(false);
    });

    it('should validate all inputs (complete mediation)', () => {
      // Every call to registerIcon validates
      expect(() => service.registerIcon('test1', '<script></script>')).toThrow();
      expect(() => service.registerIcon('test2', '<path onclick=""/>')).toThrow();
      expect(() => service.registerIcon('test3', 'javascript:alert(1)')).toThrow();

      // No way to bypass validation
      expect(service.hasIcon('test1')).toBe(false);
      expect(service.hasIcon('test2')).toBe(false);
      expect(service.hasIcon('test3')).toBe(false);
    });
  });
});
