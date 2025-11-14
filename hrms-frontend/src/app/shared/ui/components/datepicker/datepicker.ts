// ═══════════════════════════════════════════════════════════
// PREMIUM DATEPICKER COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready date picker to replace Material datepicker
// ═══════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

interface CalendarDate {
  date: Date;
  day: number;
  month: number;
  year: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  isDisabled: boolean;
  isWeekend: boolean;
}

@Component({
  selector: 'app-datepicker',
  imports: [CommonModule],
  templateUrl: './datepicker.html',
  styleUrl: './datepicker.scss'
})
export class DatepickerComponent {
  @Input() label: string = '';
  @Input() value: Date | null = null;
  @Input() minDate: Date | null = null;
  @Input() maxDate: Date | null = null;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;
  @Input() placeholder: string = 'MM/DD/YYYY';
  @Input() error: string | null = null;

  @Output() valueChange = new EventEmitter<Date>();

  isOpen: boolean = false;
  viewDate: Date = new Date(); // Current month/year being viewed
  focusedDate: Date | null = null;

  // Month and year arrays for dropdowns
  months: string[] = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];

  years: number[] = [];

  // Days of week
  weekDays: string[] = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  constructor(private elementRef: ElementRef) {
    this.initializeYears();
  }

  ngOnInit(): void {
    // Set view date to selected value or today
    if (this.value) {
      this.viewDate = new Date(this.value);
    }
  }

  // ============================================================================
  // INITIALIZATION
  // ============================================================================

  initializeYears(): void {
    const currentYear = new Date().getFullYear();
    const startYear = currentYear - 100;
    const endYear = currentYear + 10;

    for (let year = startYear; year <= endYear; year++) {
      this.years.push(year);
    }
  }

  // ============================================================================
  // CALENDAR GENERATION
  // ============================================================================

  get calendarDates(): CalendarDate[] {
    const dates: CalendarDate[] = [];
    const year = this.viewDate.getFullYear();
    const month = this.viewDate.getMonth();

    // First day of the month
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    // Start from the beginning of the week containing the first day
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - startDate.getDay());

    // Generate 6 weeks (42 days) to ensure consistent grid
    for (let i = 0; i < 42; i++) {
      const date = new Date(startDate);
      date.setDate(date.getDate() + i);

      const calendarDate: CalendarDate = {
        date: date,
        day: date.getDate(),
        month: date.getMonth(),
        year: date.getFullYear(),
        isCurrentMonth: date.getMonth() === month,
        isToday: this.isToday(date),
        isSelected: this.isSelectedDate(date),
        isDisabled: this.isDateDisabled(date),
        isWeekend: date.getDay() === 0 || date.getDay() === 6
      };

      dates.push(calendarDate);
    }

    return dates;
  }

  // ============================================================================
  // DATE UTILITIES
  // ============================================================================

  isToday(date: Date): boolean {
    const today = new Date();
    return this.isSameDay(date, today);
  }

  isSelectedDate(date: Date): boolean {
    if (!this.value) return false;
    return this.isSameDay(date, this.value);
  }

  isSameDay(date1: Date, date2: Date): boolean {
    return date1.getFullYear() === date2.getFullYear() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getDate() === date2.getDate();
  }

  isDateDisabled(date: Date): boolean {
    if (this.minDate && date < this.minDate) {
      return true;
    }
    if (this.maxDate && date > this.maxDate) {
      return true;
    }
    return false;
  }

  // ============================================================================
  // DISPLAY VALUES
  // ============================================================================

  get displayValue(): string {
    if (!this.value) return '';
    return this.formatDate(this.value);
  }

  formatDate(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const year = date.getFullYear();
    return `${month}/${day}/${year}`;
  }

  get currentMonth(): number {
    return this.viewDate.getMonth();
  }

  get currentYear(): number {
    return this.viewDate.getFullYear();
  }

  // ============================================================================
  // USER INTERACTIONS
  // ============================================================================

  toggleCalendar(): void {
    if (this.disabled) return;
    this.isOpen = !this.isOpen;

    if (this.isOpen) {
      // Reset view to selected date or today
      if (this.value) {
        this.viewDate = new Date(this.value);
      } else {
        this.viewDate = new Date();
      }
      this.focusedDate = this.value || new Date();
    }
  }

  selectDate(calendarDate: CalendarDate): void {
    if (calendarDate.isDisabled) return;

    this.value = calendarDate.date;
    this.valueChange.emit(this.value);
    this.isOpen = false;
  }

  selectToday(): void {
    const today = new Date();
    if (!this.isDateDisabled(today)) {
      this.value = today;
      this.valueChange.emit(this.value);
      this.isOpen = false;
    }
  }

  clearDate(): void {
    this.value = null;
    this.valueChange.emit(null as any);
    this.isOpen = false;
  }

  // ============================================================================
  // NAVIGATION
  // ============================================================================

  previousMonth(): void {
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() - 1, 1);
  }

  nextMonth(): void {
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() + 1, 1);
  }

  onMonthChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newMonth = parseInt(select.value, 10);
    this.viewDate = new Date(this.viewDate.getFullYear(), newMonth, 1);
  }

  onYearChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newYear = parseInt(select.value, 10);
    this.viewDate = new Date(newYear, this.viewDate.getMonth(), 1);
  }

  // ============================================================================
  // KEYBOARD NAVIGATION
  // ============================================================================

  handleKeyDown(event: KeyboardEvent): void {
    if (!this.isOpen || this.disabled) return;

    const currentFocus = this.focusedDate || this.value || new Date();

    switch (event.key) {
      case 'Escape':
        event.preventDefault();
        this.isOpen = false;
        break;

      case 'Enter':
        event.preventDefault();
        if (this.focusedDate && !this.isDateDisabled(this.focusedDate)) {
          this.value = this.focusedDate;
          this.valueChange.emit(this.value);
          this.isOpen = false;
        }
        break;

      case 'ArrowLeft':
        event.preventDefault();
        this.focusedDate = new Date(currentFocus);
        this.focusedDate.setDate(this.focusedDate.getDate() - 1);
        this.updateViewForFocusedDate();
        break;

      case 'ArrowRight':
        event.preventDefault();
        this.focusedDate = new Date(currentFocus);
        this.focusedDate.setDate(this.focusedDate.getDate() + 1);
        this.updateViewForFocusedDate();
        break;

      case 'ArrowUp':
        event.preventDefault();
        this.focusedDate = new Date(currentFocus);
        this.focusedDate.setDate(this.focusedDate.getDate() - 7);
        this.updateViewForFocusedDate();
        break;

      case 'ArrowDown':
        event.preventDefault();
        this.focusedDate = new Date(currentFocus);
        this.focusedDate.setDate(this.focusedDate.getDate() + 7);
        this.updateViewForFocusedDate();
        break;

      case 'Home':
        event.preventDefault();
        this.focusedDate = new Date(currentFocus.getFullYear(), currentFocus.getMonth(), 1);
        this.updateViewForFocusedDate();
        break;

      case 'End':
        event.preventDefault();
        this.focusedDate = new Date(currentFocus.getFullYear(), currentFocus.getMonth() + 1, 0);
        this.updateViewForFocusedDate();
        break;

      case 'PageUp':
        event.preventDefault();
        this.previousMonth();
        break;

      case 'PageDown':
        event.preventDefault();
        this.nextMonth();
        break;
    }
  }

  updateViewForFocusedDate(): void {
    if (!this.focusedDate) return;

    // If focused date is in a different month, update view
    if (this.focusedDate.getMonth() !== this.viewDate.getMonth() ||
        this.focusedDate.getFullYear() !== this.viewDate.getFullYear()) {
      this.viewDate = new Date(this.focusedDate);
    }
  }

  isDateFocused(date: Date): boolean {
    if (!this.focusedDate) return false;
    return this.isSameDay(date, this.focusedDate);
  }

  // ============================================================================
  // CLICK OUTSIDE TO CLOSE
  // ============================================================================

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  // ============================================================================
  // COMPUTED CLASSES
  // ============================================================================

  get containerClasses(): string[] {
    return [
      'datepicker-container',
      this.error ? 'datepicker-container--error' : '',
      this.disabled ? 'datepicker-container--disabled' : ''
    ].filter(Boolean);
  }

  get inputClasses(): string[] {
    return [
      'datepicker-input',
      this.value ? 'datepicker-input--filled' : '',
      this.error ? 'datepicker-input--error' : '',
      this.disabled ? 'datepicker-input--disabled' : '',
      this.isOpen ? 'datepicker-input--focused' : ''
    ].filter(Boolean);
  }
}
