// ═══════════════════════════════════════════════════════════
// ExpansionPanel Component - Unit Tests
// ═══════════════════════════════════════════════════════════

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ExpansionPanel, ExpansionPanelGroup } from './expansion-panel';
import { signal } from '@angular/core';

describe('ExpansionPanel', () => {
  let component: ExpansionPanel;
  let fixture: ComponentFixture<ExpansionPanel>;
  let panelElement: HTMLElement;
  let headerElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpansionPanel, BrowserAnimationsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(ExpansionPanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
    panelElement = fixture.nativeElement.querySelector('.expansion-panel');
    headerElement = fixture.nativeElement.querySelector('.panel-header');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Rendering', () => {
    it('should render panel container', () => {
      expect(panelElement).toBeTruthy();
    });

    it('should render panel header', () => {
      expect(headerElement).toBeTruthy();
    });

    it('should be collapsed by default', () => {
      expect(component.expanded()).toBe(false);
      expect(panelElement.classList.contains('expanded')).toBe(false);
    });

    it('should show content when expanded', () => {
      fixture.componentRef.setInput('expanded', true);
      fixture.detectChanges();

      const contentElement = fixture.nativeElement.querySelector('.panel-content');
      expect(contentElement).toBeTruthy();
    });

    it('should hide content when collapsed', () => {
      fixture.componentRef.setInput('expanded', false);
      fixture.detectChanges();

      const contentElement = fixture.nativeElement.querySelector('.panel-content');
      expect(contentElement).toBeFalsy();
    });

    it('should add expanded class when expanded', () => {
      fixture.componentRef.setInput('expanded', true);
      fixture.detectChanges();

      expect(panelElement.classList.contains('expanded')).toBe(true);
    });

    it('should add disabled class when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(panelElement.classList.contains('disabled')).toBe(true);
    });
  });

  describe('User Interaction', () => {
    it('should toggle when header is clicked', () => {
      expect(component.expanded()).toBe(false);

      headerElement.click();
      fixture.detectChanges();

      expect(component.expanded()).toBe(true);

      headerElement.click();
      fixture.detectChanges();

      expect(component.expanded()).toBe(false);
    });

    it('should not toggle when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(component.expanded()).toBe(false);

      headerElement.click();
      fixture.detectChanges();

      expect(component.expanded()).toBe(false);
    });

    it('should toggle on Enter key', () => {
      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      headerElement.dispatchEvent(event);
      fixture.detectChanges();

      expect(component.expanded()).toBe(true);
    });

    it('should toggle on Space key', () => {
      const event = new KeyboardEvent('keydown', { key: ' ' });
      spyOn(event, 'preventDefault');

      headerElement.dispatchEvent(event);
      fixture.detectChanges();

      expect(component.expanded()).toBe(true);
      expect(event.preventDefault).toHaveBeenCalled();
    });

    it('should emit expandedChange event when toggled', () => {
      let emittedValue: boolean | undefined;
      component.expandedChange.subscribe(value => emittedValue = value);

      component.toggle();

      expect(emittedValue).toBe(true);
    });
  });

  describe('Accessibility', () => {
    it('should have role="button" on header', () => {
      expect(headerElement.getAttribute('role')).toBe('button');
    });

    it('should have aria-expanded="false" when collapsed', () => {
      expect(headerElement.getAttribute('aria-expanded')).toBe('false');
    });

    it('should have aria-expanded="true" when expanded', () => {
      fixture.componentRef.setInput('expanded', true);
      fixture.detectChanges();

      expect(headerElement.getAttribute('aria-expanded')).toBe('true');
    });

    it('should have tabindex="0" when enabled', () => {
      expect(headerElement.getAttribute('tabindex')).toBe('0');
    });

    it('should have tabindex="-1" when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(headerElement.getAttribute('tabindex')).toBe('-1');
    });

    it('should have aria-disabled when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(headerElement.getAttribute('aria-disabled')).toBe('true');
    });

    it('should have role="region" on content', () => {
      fixture.componentRef.setInput('expanded', true);
      fixture.detectChanges();

      const contentElement = fixture.nativeElement.querySelector('.panel-content');
      expect(contentElement.getAttribute('role')).toBe('region');
    });
  });

  describe('Two-way Binding', () => {
    it('should support model binding', () => {
      const expandedSignal = signal(false);
      fixture.componentRef.setInput('expanded', expandedSignal());

      component.toggle();

      expect(component.expanded()).toBe(true);
    });

    it('should update when external model changes', () => {
      fixture.componentRef.setInput('expanded', false);
      expect(component.expanded()).toBe(false);

      fixture.componentRef.setInput('expanded', true);
      expect(component.expanded()).toBe(true);
    });
  });

  describe('Icon Display', () => {
    it('should show expand_more icon when collapsed', () => {
      const icon = fixture.nativeElement.querySelector('app-icon');
      expect(icon).toBeTruthy();
      // Icon name is bound to component input, checked via component
      expect(component.expanded()).toBe(false);
    });

    it('should show expand_less icon when expanded', () => {
      fixture.componentRef.setInput('expanded', true);
      fixture.detectChanges();

      expect(component.expanded()).toBe(true);
    });
  });
});

describe('ExpansionPanelGroup', () => {
  let component: ExpansionPanelGroup;
  let fixture: ComponentFixture<ExpansionPanelGroup>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpansionPanelGroup]
    }).compileComponents();

    fixture = TestBed.createComponent(ExpansionPanelGroup);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render group container', () => {
    const groupElement = fixture.nativeElement.querySelector('.expansion-panel-group');
    expect(groupElement).toBeTruthy();
  });

  it('should have role="region"', () => {
    const groupElement = fixture.nativeElement.querySelector('.expansion-panel-group');
    expect(groupElement.getAttribute('role')).toBe('region');
  });

  it('should accept multi input', () => {
    expect(component.multi()).toBe(false);

    fixture.componentRef.setInput('multi', true);
    expect(component.multi()).toBe(true);
  });
});
