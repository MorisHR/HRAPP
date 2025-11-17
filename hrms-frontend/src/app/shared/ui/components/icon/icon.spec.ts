// ═══════════════════════════════════════════════════════════
// ICON COMPONENT TESTS
// Comprehensive test suite for Fortune 500-grade icon component
// Coverage: 25+ tests across all functionality
// ═══════════════════════════════════════════════════════════

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IconComponent } from './icon';
import { IconRegistryService } from '../../services/icon-registry.service';
import { DomSanitizer } from '@angular/platform-browser';

describe('IconComponent', () => {
  let component: IconComponent;
  let fixture: ComponentFixture<IconComponent>;
  let iconRegistryService: jasmine.SpyObj<IconRegistryService>;
  let sanitizer: DomSanitizer;

  const mockIconPath = '<path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/>';
  const fallbackPath = '<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>';

  beforeEach(async () => {
    const iconRegistrySpy = jasmine.createSpyObj('IconRegistryService', ['getIcon']);

    await TestBed.configureTestingModule({
      imports: [IconComponent],
      providers: [
        { provide: IconRegistryService, useValue: iconRegistrySpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(IconComponent);
    component = fixture.componentInstance;
    iconRegistryService = TestBed.inject(IconRegistryService) as jasmine.SpyObj<IconRegistryService>;
    sanitizer = TestBed.inject(DomSanitizer);
  });

  afterEach(() => {
    fixture.destroy();
  });

  // ═══════════════════════════════════════════════════════════
  // COMPONENT INITIALIZATION
  // ═══════════════════════════════════════════════════════════

  describe('Component Initialization', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should have default values', () => {
      expect(component.name).toBe('');
      expect(component.size).toBe('medium');
      expect(component.library).toBe('material');
      expect(component.color).toBeUndefined();
      expect(component.ariaLabel).toBeUndefined();
    });

    it('should initialize as standalone component', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled).toBeTruthy();
    });

    it('should call ngOnInit and load icon', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';

      component.ngOnInit();

      expect(iconRegistryService.getIcon).toHaveBeenCalledWith('home', 'material');
    });
  });

  // ═══════════════════════════════════════════════════════════
  // ICON LOADING
  // ═══════════════════════════════════════════════════════════

  describe('Icon Loading', () => {
    it('should load icon from registry when icon exists', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';

      component.ngOnInit();

      expect(component.iconFound).toBe(true);
      expect(component.svgContent).toContain('svg');
      expect(component.svgContent).toContain(mockIconPath);
    });

    it('should use fallback icon when icon not found', () => {
      iconRegistryService.getIcon.and.returnValue(null);
      component.name = 'nonexistent';

      spyOn(console, 'warn');
      component.ngOnInit();

      expect(component.iconFound).toBe(false);
      expect(component.svgContent).toContain('svg');
      expect(console.warn).toHaveBeenCalledWith(
        'Icon "nonexistent" not found in "material" library. Using fallback icon.'
      );
    });

    it('should use fallback icon when name is empty', () => {
      component.name = '';

      component.ngOnInit();

      expect(component.iconFound).toBe(true); // Empty name doesn't set iconFound to false
      expect(component.svgContent).toContain('svg');
    });

    it('should load icon from different libraries', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'search';
      component.library = 'heroicons';

      component.ngOnInit();

      expect(iconRegistryService.getIcon).toHaveBeenCalledWith('search', 'heroicons');
    });
  });

  // ═══════════════════════════════════════════════════════════
  // ICON SIZES
  // ═══════════════════════════════════════════════════════════

  describe('Icon Sizes', () => {
    it('should apply small size class', () => {
      component.size = 'small';
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.classList.contains('app-icon--small')).toBe(true);
    });

    it('should apply medium size class by default', () => {
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.classList.contains('app-icon--medium')).toBe(true);
    });

    it('should apply large size class', () => {
      component.size = 'large';
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.classList.contains('app-icon--large')).toBe(true);
    });

    it('should update size dynamically', () => {
      component.size = 'small';
      fixture.detectChanges();
      let hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.classList.contains('app-icon--small')).toBe(true);

      component.size = 'large';
      fixture.detectChanges();
      hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.classList.contains('app-icon--large')).toBe(true);
      expect(hostElement.classList.contains('app-icon--small')).toBe(false);
    });
  });

  // ═══════════════════════════════════════════════════════════
  // COLOR CUSTOMIZATION
  // ═══════════════════════════════════════════════════════════

  describe('Color Customization', () => {
    it('should apply custom color', () => {
      component.color = '#FF5733';
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.style.color).toBe('rgb(255, 87, 51)'); // Browser converts hex to rgb
    });

    it('should apply CSS variable color', () => {
      component.color = 'var(--primary-color)';
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.style.color).toBe('var(--primary-color)');
    });

    it('should work without custom color', () => {
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.style.color).toBe('');
    });
  });

  // ═══════════════════════════════════════════════════════════
  // ACCESSIBILITY
  // ═══════════════════════════════════════════════════════════

  describe('Accessibility', () => {
    it('should have role="img" attribute', () => {
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.getAttribute('role')).toBe('img');
    });

    it('should use custom aria-label when provided', () => {
      component.ariaLabel = 'Home Icon';
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.getAttribute('aria-label')).toBe('Home Icon');
    });

    it('should use icon name as aria-label when custom label not provided', () => {
      component.name = 'home';
      fixture.detectChanges();

      const hostElement = fixture.nativeElement as HTMLElement;
      expect(hostElement.getAttribute('aria-label')).toBe('home');
    });

    it('should have aria-hidden="true" on SVG element', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';

      component.ngOnInit();

      expect(component.svgContent.toString()).toContain('aria-hidden="true"');
    });
  });

  // ═══════════════════════════════════════════════════════════
  // SVG RENDERING
  // ═══════════════════════════════════════════════════════════

  describe('SVG Rendering', () => {
    it('should render fill-based SVG for Material icons', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';
      component.library = 'material';

      component.ngOnInit();

      expect(component.svgContent.toString()).toContain('fill="currentColor"');
      expect(component.svgContent.toString()).not.toContain('stroke="currentColor"');
    });

    it('should render stroke-based SVG for Heroicons', () => {
      const heroiconPath = '<path stroke-linecap="round" stroke-linejoin="round" d="M3 12l9-9 9 9"/>';
      iconRegistryService.getIcon.and.returnValue(heroiconPath);
      component.name = 'home';
      component.library = 'heroicons';

      component.ngOnInit();

      expect(component.svgContent.toString()).toContain('stroke="currentColor"');
      expect(component.svgContent.toString()).toContain('fill="none"');
    });

    it('should include viewBox attribute', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';

      component.ngOnInit();

      expect(component.svgContent.toString()).toContain('viewBox="0 0 24 24"');
    });

    it('should include xmlns attribute', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';

      component.ngOnInit();

      expect(component.svgContent.toString()).toContain('xmlns="http://www.w3.org/2000/svg"');
    });
  });

  // ═══════════════════════════════════════════════════════════
  // HOST CLASSES
  // ═══════════════════════════════════════════════════════════

  describe('Host Classes', () => {
    it('should always include base app-icon class', () => {
      const classes = component.hostClasses;
      expect(classes).toContain('app-icon');
    });

    it('should include size class', () => {
      component.size = 'large';
      const classes = component.hostClasses;
      expect(classes).toContain('app-icon--large');
    });

    it('should include fallback class when icon not found', () => {
      iconRegistryService.getIcon.and.returnValue(null);
      component.name = 'nonexistent';

      spyOn(console, 'warn');
      component.ngOnInit();

      const classes = component.hostClasses;
      expect(classes).toContain('app-icon--fallback');
    });

    it('should not include fallback class when icon found', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'home';

      component.ngOnInit();

      const classes = component.hostClasses;
      expect(classes).not.toContain('app-icon--fallback');
    });
  });

  // ═══════════════════════════════════════════════════════════
  // CHANGE DETECTION
  // ═══════════════════════════════════════════════════════════

  describe('Change Detection', () => {
    it('should use OnPush change detection strategy', () => {
      const componentMetadata = (IconComponent as any).ɵcmp;
      expect(componentMetadata.changeDetection).toBe(0); // 0 = OnPush
    });
  });

  // ═══════════════════════════════════════════════════════════
  // EDGE CASES
  // ═══════════════════════════════════════════════════════════

  describe('Edge Cases', () => {
    it('should handle null icon name gracefully', () => {
      component.name = null as any;

      expect(() => component.ngOnInit()).not.toThrow();
    });

    it('should handle undefined icon name gracefully', () => {
      component.name = undefined as any;

      expect(() => component.ngOnInit()).not.toThrow();
    });

    it('should handle special characters in icon name', () => {
      iconRegistryService.getIcon.and.returnValue(mockIconPath);
      component.name = 'icon-with-dash_and_underscore';

      expect(() => component.ngOnInit()).not.toThrow();
      expect(iconRegistryService.getIcon).toHaveBeenCalledWith('icon-with-dash_and_underscore', 'material');
    });

    it('should sanitize SVG content', () => {
      const maliciousSvg = '<script>alert("xss")</script><path d="M10 20"/>';
      iconRegistryService.getIcon.and.returnValue(maliciousSvg);
      component.name = 'malicious';

      component.ngOnInit();

      // Sanitizer should remove script tags
      expect(component.svgContent.toString()).not.toContain('<script>');
    });
  });
});
