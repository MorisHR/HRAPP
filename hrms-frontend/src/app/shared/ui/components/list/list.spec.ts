// ═══════════════════════════════════════════════════════════
// List Component - Unit Tests
// ═══════════════════════════════════════════════════════════

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { List, ListItem } from './list';

describe('List', () => {
  let component: List;
  let fixture: ComponentFixture<List>;
  let ulElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [List]
    }).compileComponents();

    fixture = TestBed.createComponent(List);
    component = fixture.componentInstance;
    fixture.detectChanges();
    ulElement = fixture.nativeElement.querySelector('ul');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Rendering', () => {
    it('should render ul element', () => {
      expect(ulElement).toBeTruthy();
      expect(ulElement.tagName).toBe('UL');
    });

    it('should have role="list"', () => {
      expect(ulElement.getAttribute('role')).toBe('list');
    });

    it('should apply default classes', () => {
      expect(ulElement.classList.contains('list')).toBe(true);
    });

    it('should apply dense class when dense is true', () => {
      fixture.componentRef.setInput('dense', true);
      fixture.detectChanges();

      expect(ulElement.classList.contains('dense')).toBe(true);
    });

    it('should apply bordered class when bordered is true', () => {
      fixture.componentRef.setInput('bordered', true);
      fixture.detectChanges();

      expect(ulElement.classList.contains('bordered')).toBe(true);
    });

    it('should apply elevated class when elevated is true', () => {
      fixture.componentRef.setInput('elevated', true);
      fixture.detectChanges();

      expect(ulElement.classList.contains('elevated')).toBe(true);
    });

    it('should apply multiple classes when multiple inputs are true', () => {
      fixture.componentRef.setInput('dense', true);
      fixture.componentRef.setInput('bordered', true);
      fixture.componentRef.setInput('elevated', true);
      fixture.detectChanges();

      expect(ulElement.classList.contains('dense')).toBe(true);
      expect(ulElement.classList.contains('bordered')).toBe(true);
      expect(ulElement.classList.contains('elevated')).toBe(true);
    });
  });

  describe('Input Reactivity', () => {
    it('should react to dense changes', () => {
      expect(component.dense()).toBe(false);

      fixture.componentRef.setInput('dense', true);
      expect(component.dense()).toBe(true);
    });

    it('should react to bordered changes', () => {
      expect(component.bordered()).toBe(false);

      fixture.componentRef.setInput('bordered', true);
      expect(component.bordered()).toBe(true);
    });

    it('should react to elevated changes', () => {
      expect(component.elevated()).toBe(false);

      fixture.componentRef.setInput('elevated', true);
      expect(component.elevated()).toBe(true);
    });
  });
});

describe('ListItem', () => {
  let component: ListItem;
  let fixture: ComponentFixture<ListItem>;
  let liElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListItem]
    }).compileComponents();

    fixture = TestBed.createComponent(ListItem);
    component = fixture.componentInstance;
    fixture.detectChanges();
    liElement = fixture.nativeElement.querySelector('li');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Rendering', () => {
    it('should render li element', () => {
      expect(liElement).toBeTruthy();
      expect(liElement.tagName).toBe('LI');
    });

    it('should have role="listitem" by default', () => {
      expect(liElement.getAttribute('role')).toBe('listitem');
    });

    it('should have role="button" when clickable', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.detectChanges();

      expect(liElement.getAttribute('role')).toBe('button');
    });

    it('should apply clickable class when clickable is true', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.detectChanges();

      expect(liElement.classList.contains('clickable')).toBe(true);
    });

    it('should apply disabled class when disabled is true', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(liElement.classList.contains('disabled')).toBe(true);
    });

    it('should apply selected class when selected is true', () => {
      fixture.componentRef.setInput('selected', true);
      fixture.detectChanges();

      expect(liElement.classList.contains('selected')).toBe(true);
    });

    it('should apply dense class when dense is true', () => {
      fixture.componentRef.setInput('dense', true);
      fixture.detectChanges();

      expect(liElement.classList.contains('dense')).toBe(true);
    });
  });

  describe('Accessibility', () => {
    it('should have tabindex="-1" by default', () => {
      expect(liElement.getAttribute('tabindex')).toBe('-1');
    });

    it('should have tabindex="0" when clickable', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.detectChanges();

      expect(liElement.getAttribute('tabindex')).toBe('0');
    });

    it('should have tabindex="-1" when disabled', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(liElement.getAttribute('tabindex')).toBe('-1');
    });

    it('should have aria-disabled when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      expect(liElement.getAttribute('aria-disabled')).toBe('true');
    });
  });

  describe('User Interaction', () => {
    it('should emit itemClick when clicked and clickable', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.detectChanges();

      let clicked = false;
      component.itemClick.subscribe(() => clicked = true);

      liElement.click();

      expect(clicked).toBe(true);
    });

    it('should not emit itemClick when clicked and not clickable', () => {
      fixture.componentRef.setInput('clickable', false);
      fixture.detectChanges();

      let clicked = false;
      component.itemClick.subscribe(() => clicked = true);

      liElement.click();

      expect(clicked).toBe(false);
    });

    it('should not emit itemClick when disabled', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      let clicked = false;
      component.itemClick.subscribe(() => clicked = true);

      liElement.click();

      expect(clicked).toBe(false);
    });

    it('should emit itemClick on Enter key', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.detectChanges();

      let clicked = false;
      component.itemClick.subscribe(() => clicked = true);

      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      spyOn(event, 'preventDefault');
      liElement.dispatchEvent(event);

      expect(clicked).toBe(true);
      expect(event.preventDefault).toHaveBeenCalled();
    });

    it('should emit itemClick on Space key', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.detectChanges();

      let clicked = false;
      component.itemClick.subscribe(() => clicked = true);

      const event = new KeyboardEvent('keydown', { key: ' ' });
      spyOn(event, 'preventDefault');
      liElement.dispatchEvent(event);

      expect(clicked).toBe(true);
      expect(event.preventDefault).toHaveBeenCalled();
    });

    it('should not respond to keyboard when disabled', () => {
      fixture.componentRef.setInput('clickable', true);
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      let clicked = false;
      component.itemClick.subscribe(() => clicked = true);

      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      liElement.dispatchEvent(event);

      expect(clicked).toBe(false);
    });
  });

  describe('State Management', () => {
    it('should update classes when state changes', () => {
      expect(liElement.classList.contains('selected')).toBe(false);

      fixture.componentRef.setInput('selected', true);
      fixture.detectChanges();

      expect(liElement.classList.contains('selected')).toBe(true);

      fixture.componentRef.setInput('selected', false);
      fixture.detectChanges();

      expect(liElement.classList.contains('selected')).toBe(false);
    });
  });
});
