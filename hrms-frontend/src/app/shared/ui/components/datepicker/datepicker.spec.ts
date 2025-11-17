import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Datepicker } from './datepicker';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

describe('Datepicker Component', () => {
  let component: Datepicker;
  let fixture: ComponentFixture<Datepicker>;
  let compiled: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Datepicker]
    }).compileComponents();

    fixture = TestBed.createComponent(Datepicker);
    component = fixture.componentInstance;
    compiled = fixture.nativeElement;
    fixture.detectChanges();
  });

  describe('Initialization', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should start with calendar hidden', () => {
      expect(component['showCalendar']()).toBe(false);
    });

    it('should initialize with no value', () => {
      expect(component.value()).toBeNull();
    });

    it('should render input component', () => {
      const input = compiled.querySelector('app-input');
      expect(input).toBeTruthy();
    });
  });

  describe('Date Formatting', () => {
    it('should format date as MM/DD/YYYY', () => {
      const testDate = new Date(2025, 0, 15); // January 15, 2025
      fixture.componentRef.setInput('value', testDate);
      fixture.detectChanges();

      expect(component['formattedValue']()).toBe('01/15/2025');
    });

    it('should return empty string when no value', () => {
      expect(component['formattedValue']()).toBe('');
    });

    it('should pad single digit months and days', () => {
      const testDate = new Date(2025, 4, 5); // May 5, 2025
      fixture.componentRef.setInput('value', testDate);
      fixture.detectChanges();

      expect(component['formattedValue']()).toBe('05/05/2025');
    });
  });

  describe('Calendar Toggle', () => {
    it('should show calendar when clicked', () => {
      const input = compiled.querySelector('app-input') as HTMLElement;
      input.click();
      fixture.detectChanges();

      expect(component['showCalendar']()).toBe(true);
    });

    it('should not show calendar when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      component['toggleCalendar']();
      expect(component['showCalendar']()).toBe(false);
    });

    it('should close calendar when overlay is clicked', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const overlay = compiled.querySelector('.calendar-overlay') as HTMLElement;
      overlay.click();
      fixture.detectChanges();

      expect(component['showCalendar']()).toBe(false);
    });

    it('should set current date to value when opening with existing value', () => {
      const testDate = new Date(2025, 5, 15);
      fixture.componentRef.setInput('value', testDate);
      fixture.detectChanges();

      component['toggleCalendar']();
      fixture.detectChanges();

      const currentDate = component['currentDate']();
      expect(currentDate.getMonth()).toBe(5);
      expect(currentDate.getFullYear()).toBe(2025);
    });
  });

  describe('Month Navigation', () => {
    beforeEach(() => {
      component['currentDate'].set(new Date(2025, 5, 15)); // June 2025
    });

    it('should navigate to previous month', () => {
      component['previousMonth']();
      const date = component['currentDate']();
      expect(date.getMonth()).toBe(4); // May
    });

    it('should navigate to next month', () => {
      component['nextMonth']();
      const date = component['currentDate']();
      expect(date.getMonth()).toBe(6); // July
    });

    it('should handle year boundary when going to previous month from January', () => {
      component['currentDate'].set(new Date(2025, 0, 15));
      component['previousMonth']();
      const date = component['currentDate']();
      expect(date.getMonth()).toBe(11); // December
      expect(date.getFullYear()).toBe(2024);
    });

    it('should handle year boundary when going to next month from December', () => {
      component['currentDate'].set(new Date(2025, 11, 15));
      component['nextMonth']();
      const date = component['currentDate']();
      expect(date.getMonth()).toBe(0); // January
      expect(date.getFullYear()).toBe(2026);
    });

    it('should display correct month/year string', () => {
      component['currentDate'].set(new Date(2025, 5, 15));
      expect(component['currentMonthYear']()).toBe('June 2025');
    });
  });

  describe('Calendar Days Generation', () => {
    it('should generate 42 days (6 weeks)', () => {
      component['currentDate'].set(new Date(2025, 5, 15));
      const days = component['calendarDays']();
      expect(days.length).toBe(42);
    });

    it('should mark current month days correctly', () => {
      component['currentDate'].set(new Date(2025, 5, 15)); // June 2025
      const days = component['calendarDays']();
      const currentMonthDays = days.filter(d => d.currentMonth);
      expect(currentMonthDays.length).toBe(30); // June has 30 days
    });

    it('should mark today correctly', () => {
      const today = new Date();
      component['currentDate'].set(today);
      const days = component['calendarDays']();
      const todayCell = days.find(d => d.today);
      expect(todayCell).toBeTruthy();
      expect(todayCell!.day).toBe(today.getDate());
    });

    it('should mark selected date correctly', () => {
      const selectedDate = new Date(2025, 5, 15);
      fixture.componentRef.setInput('value', selectedDate);
      component['currentDate'].set(selectedDate);
      fixture.detectChanges();

      const days = component['calendarDays']();
      const selectedCell = days.find(d => d.selected);
      expect(selectedCell).toBeTruthy();
      expect(selectedCell!.day).toBe(15);
    });

    it('should include previous month overflow days', () => {
      component['currentDate'].set(new Date(2025, 5, 1)); // June 1, 2025 (Sunday)
      const days = component['calendarDays']();
      const prevMonthDays = days.filter((d, i) => !d.currentMonth && i < 15);
      expect(prevMonthDays.length).toBeGreaterThan(0);
    });

    it('should include next month overflow days', () => {
      component['currentDate'].set(new Date(2025, 5, 15));
      const days = component['calendarDays']();
      const nextMonthDays = days.filter((d, i) => !d.currentMonth && i > 15);
      expect(nextMonthDays.length).toBeGreaterThan(0);
    });
  });

  describe('Date Selection', () => {
    it('should select a date', () => {
      const testDate = new Date(2025, 5, 15);
      component['selectDate'](testDate);

      expect(component.value()).toEqual(testDate);
    });

    it('should close calendar after selecting date', () => {
      component['showCalendar'].set(true);
      component['selectDate'](new Date(2025, 5, 15));

      expect(component['showCalendar']()).toBe(false);
    });

    it('should select today when "Today" button clicked', () => {
      const today = new Date();
      component['selectToday']();

      const selectedDate = component.value();
      expect(selectedDate?.toDateString()).toBe(today.toDateString());
    });

    it('should clear date when "Clear" button clicked', () => {
      component.value.set(new Date(2025, 5, 15));
      component['clearDate']();

      expect(component.value()).toBeNull();
    });

    it('should close calendar after clearing date', () => {
      component['showCalendar'].set(true);
      component['clearDate']();

      expect(component['showCalendar']()).toBe(false);
    });
  });

  describe('Min/Max Date Validation', () => {
    it('should not select date before minDate', () => {
      const minDate = new Date(2025, 5, 10);
      const testDate = new Date(2025, 5, 5);
      fixture.componentRef.setInput('minDate', minDate);
      fixture.detectChanges();

      component['selectDate'](testDate);
      expect(component.value()).toBeNull();
    });

    it('should not select date after maxDate', () => {
      const maxDate = new Date(2025, 5, 20);
      const testDate = new Date(2025, 5, 25);
      fixture.componentRef.setInput('maxDate', maxDate);
      fixture.detectChanges();

      component['selectDate'](testDate);
      expect(component.value()).toBeNull();
    });

    it('should select date within min/max range', () => {
      const minDate = new Date(2025, 5, 10);
      const maxDate = new Date(2025, 5, 20);
      const testDate = new Date(2025, 5, 15);
      fixture.componentRef.setInput('minDate', minDate);
      fixture.componentRef.setInput('maxDate', maxDate);
      fixture.detectChanges();

      component['selectDate'](testDate);
      expect(component.value()).toEqual(testDate);
    });
  });

  describe('Input Properties', () => {
    it('should accept placeholder', () => {
      fixture.componentRef.setInput('placeholder', 'Pick a date');
      fixture.detectChanges();
      expect(component.placeholder()).toBe('Pick a date');
    });

    it('should accept label', () => {
      fixture.componentRef.setInput('label', 'Birth Date');
      fixture.detectChanges();
      expect(component.label()).toBe('Birth Date');
    });

    it('should accept required flag', () => {
      fixture.componentRef.setInput('required', true);
      fixture.detectChanges();
      expect(component.required()).toBe(true);
    });

    it('should accept disabled flag', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();
      expect(component.disabled()).toBe(true);
    });

    it('should accept error message', () => {
      fixture.componentRef.setInput('error', 'Date is required');
      fixture.detectChanges();
      expect(component.error()).toBe('Date is required');
    });
  });

  describe('Keyboard & Mouse Interactions', () => {
    it('should close calendar when clicking outside', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      // Simulate click outside
      const event = new MouseEvent('click', { bubbles: true });
      Object.defineProperty(event, 'target', { value: document.body });
      component.onDocumentClick(event);

      expect(component['showCalendar']()).toBe(false);
    });

    it('should not close calendar when clicking inside', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const datepicker = compiled.querySelector('.datepicker') as HTMLElement;
      const event = new MouseEvent('click', { bubbles: true });
      Object.defineProperty(event, 'target', { value: datepicker });
      component.onDocumentClick(event);

      expect(component['showCalendar']()).toBe(true);
    });
  });

  describe('Accessibility', () => {
    it('should have calendar icon for visual indicator', () => {
      expect(compiled.querySelector('.calendar-icon')).toBeTruthy();
    });

    it('should disable day cells for other months', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const dayCells = compiled.querySelectorAll('.day-cell');
      const otherMonthCells = Array.from(dayCells).filter(cell =>
        cell.classList.contains('other-month')
      );
      otherMonthCells.forEach(cell => {
        expect((cell as HTMLButtonElement).disabled).toBe(true);
      });
    });

    it('should apply proper classes for selected date', () => {
      const testDate = new Date(2025, 5, 15);
      fixture.componentRef.setInput('value', testDate);
      component['currentDate'].set(testDate);
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const selectedCell = compiled.querySelector('.day-cell.selected');
      expect(selectedCell).toBeTruthy();
    });

    it('should apply proper classes for today', () => {
      const today = new Date();
      component['currentDate'].set(today);
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const todayCell = compiled.querySelector('.day-cell.today');
      expect(todayCell).toBeTruthy();
    });
  });

  describe('Edge Cases', () => {
    it('should handle null value gracefully', () => {
      fixture.componentRef.setInput('value', null);
      fixture.detectChanges();
      expect(component['formattedValue']()).toBe('');
    });

    it('should handle February in leap year', () => {
      component['currentDate'].set(new Date(2024, 1, 15)); // Feb 2024 (leap year)
      const days = component['calendarDays']();
      const febDays = days.filter(d => d.currentMonth);
      expect(febDays.length).toBe(29);
    });

    it('should handle February in non-leap year', () => {
      component['currentDate'].set(new Date(2025, 1, 15)); // Feb 2025 (non-leap)
      const days = component['calendarDays']();
      const febDays = days.filter(d => d.currentMonth);
      expect(febDays.length).toBe(28);
    });

    it('should handle month with 31 days', () => {
      component['currentDate'].set(new Date(2025, 0, 15)); // January 2025
      const days = component['calendarDays']();
      const janDays = days.filter(d => d.currentMonth);
      expect(janDays.length).toBe(31);
    });

    it('should handle month with 30 days', () => {
      component['currentDate'].set(new Date(2025, 3, 15)); // April 2025
      const days = component['calendarDays']();
      const aprDays = days.filter(d => d.currentMonth);
      expect(aprDays.length).toBe(30);
    });
  });

  describe('UI Rendering', () => {
    it('should render weekday headers', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const headers = compiled.querySelectorAll('.weekday');
      expect(headers.length).toBe(7);
    });

    it('should render calendar header with month/year', () => {
      component['currentDate'].set(new Date(2025, 5, 15));
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const monthYear = compiled.querySelector('.month-year');
      expect(monthYear?.textContent?.trim()).toBe('June 2025');
    });

    it('should render Today and Clear buttons', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const buttons = compiled.querySelectorAll('.calendar-footer app-button');
      expect(buttons.length).toBe(2);
    });

    it('should render navigation buttons', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      const navButtons = compiled.querySelectorAll('.calendar-header app-button');
      expect(navButtons.length).toBe(2);
    });

    it('should show overlay when calendar is open', () => {
      component['showCalendar'].set(true);
      fixture.detectChanges();

      expect(compiled.querySelector('.calendar-overlay')).toBeTruthy();
    });

    it('should not show overlay when calendar is closed', () => {
      component['showCalendar'].set(false);
      fixture.detectChanges();

      expect(compiled.querySelector('.calendar-overlay')).toBeFalsy();
    });
  });
});
