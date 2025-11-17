// ═══════════════════════════════════════════════════════════
// Divider Component - Unit Tests
// ═══════════════════════════════════════════════════════════

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Divider } from './divider';

describe('Divider', () => {
  let component: Divider;
  let fixture: ComponentFixture<Divider>;
  let hrElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Divider]
    }).compileComponents();

    fixture = TestBed.createComponent(Divider);
    component = fixture.componentInstance;
    fixture.detectChanges();
    hrElement = fixture.nativeElement.querySelector('hr');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Rendering', () => {
    it('should render an hr element', () => {
      expect(hrElement).toBeTruthy();
      expect(hrElement.tagName).toBe('HR');
    });

    it('should render horizontal divider by default', () => {
      expect(hrElement.className).not.toContain('vertical');
      expect(hrElement.className).toContain('horizontal');
    });

    it('should render vertical divider when orientation is vertical', () => {
      fixture.componentRef.setInput('orientation', 'vertical');
      fixture.detectChanges();

      expect(hrElement.className).toContain('vertical');
    });

    it('should add inset class when inset is true', () => {
      fixture.componentRef.setInput('inset', true);
      fixture.detectChanges();

      expect(hrElement.className).toContain('inset');
    });

    it('should add dense class when dense is true', () => {
      fixture.componentRef.setInput('dense', true);
      fixture.detectChanges();

      expect(hrElement.className).toContain('dense');
    });

    it('should combine multiple classes correctly', () => {
      fixture.componentRef.setInput('orientation', 'vertical');
      fixture.componentRef.setInput('dense', true);
      fixture.detectChanges();

      expect(hrElement.className).toContain('vertical');
      expect(hrElement.className).toContain('dense');
    });
  });

  describe('Accessibility', () => {
    it('should have role="separator"', () => {
      expect(hrElement.getAttribute('role')).toBe('separator');
    });

    it('should have aria-orientation="horizontal" by default', () => {
      expect(hrElement.getAttribute('aria-orientation')).toBe('horizontal');
    });

    it('should have aria-orientation="vertical" when vertical', () => {
      fixture.componentRef.setInput('orientation', 'vertical');
      fixture.detectChanges();

      expect(hrElement.getAttribute('aria-orientation')).toBe('vertical');
    });

    it('should update aria-orientation when orientation changes', () => {
      fixture.componentRef.setInput('orientation', 'vertical');
      fixture.detectChanges();
      expect(hrElement.getAttribute('aria-orientation')).toBe('vertical');

      fixture.componentRef.setInput('orientation', 'horizontal');
      fixture.detectChanges();
      expect(hrElement.getAttribute('aria-orientation')).toBe('horizontal');
    });
  });

  describe('Host Element', () => {
    it('should add vertical class to host when orientation is vertical', () => {
      fixture.componentRef.setInput('orientation', 'vertical');
      fixture.detectChanges();

      expect(fixture.nativeElement.classList.contains('vertical')).toBe(true);
    });

    it('should not have vertical class on host when horizontal', () => {
      expect(fixture.nativeElement.classList.contains('vertical')).toBe(false);
    });
  });

  describe('Input Reactivity', () => {
    it('should react to orientation changes', () => {
      expect(component.orientation()).toBe('horizontal');

      fixture.componentRef.setInput('orientation', 'vertical');
      expect(component.orientation()).toBe('vertical');
    });

    it('should react to inset changes', () => {
      expect(component.inset()).toBe(false);

      fixture.componentRef.setInput('inset', true);
      expect(component.inset()).toBe(true);
    });

    it('should react to dense changes', () => {
      expect(component.dense()).toBe(false);

      fixture.componentRef.setInput('dense', true);
      expect(component.dense()).toBe(true);
    });
  });

  describe('Computed Classes', () => {
    it('should compute correct classes for default state', () => {
      const classes = hrElement.className.split(' ').filter(c => c);
      expect(classes).toContain('horizontal');
      expect(classes).not.toContain('inset');
      expect(classes).not.toContain('dense');
    });

    it('should compute correct classes for all options enabled', () => {
      fixture.componentRef.setInput('orientation', 'vertical');
      fixture.componentRef.setInput('inset', true);
      fixture.componentRef.setInput('dense', true);
      fixture.detectChanges();

      const classes = hrElement.className.split(' ').filter(c => c);
      expect(classes).toContain('vertical');
      expect(classes).toContain('inset');
      expect(classes).toContain('dense');
    });
  });
});
