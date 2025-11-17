// ═══════════════════════════════════════════════════════════
// Datepicker Component - Enhanced Full Calendar UI
// Fortune 500-grade date selection component
// Replaces Angular Material's MatDatepickerModule
// ═══════════════════════════════════════════════════════════

import { Component, input, model, computed, signal, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon';
import { InputComponent } from '../input/input';
import { ButtonComponent } from '../button/button';

@Component({
  selector: 'app-datepicker',
  standalone: true,
  imports: [CommonModule, IconComponent, InputComponent, ButtonComponent],
  template: `
    <div class="datepicker">
      <app-input
        [value]="formattedValue()"
        [placeholder]="placeholder()"
        [label]="label()"
        [required]="required()"
        [disabled]="disabled()"
        [error]="error()"
        [readonly]="true"
        (click)="toggleCalendar()"
      >
        <app-icon name="calendar_today" class="calendar-icon"></app-icon>
      </app-input>

      @if (showCalendar()) {
        <div class="calendar-overlay" (click)="closeCalendar()"></div>
        <div class="calendar-popup" #calendarPopup>
          <!-- Calendar Header -->
          <div class="calendar-header">
            <app-button
              variant="ghost"
              size="small"
              (click)="previousMonth()"
              [disabled]="disabled()">
              <app-icon name="chevron_left"></app-icon>
            </app-button>
            <div class="month-year">
              {{ currentMonthYear() }}
            </div>
            <app-button
              variant="ghost"
              size="small"
              (click)="nextMonth()"
              [disabled]="disabled()">
              <app-icon name="chevron_right"></app-icon>
            </app-button>
          </div>

          <!-- Weekday Headers -->
          <div class="weekday-headers">
            @for (day of weekdays; track day) {
              <div class="weekday">{{ day }}</div>
            }
          </div>

          <!-- Calendar Days -->
          <div class="calendar-days">
            @for (day of calendarDays(); track day.date) {
              <button
                type="button"
                class="day-cell"
                [class.other-month]="!day.currentMonth"
                [class.selected]="day.selected"
                [class.today]="day.today"
                [disabled]="disabled() || !day.currentMonth"
                (click)="selectDate(day.date)">
                {{ day.day }}
              </button>
            }
          </div>

          <!-- Calendar Footer -->
          <div class="calendar-footer">
            <app-button
              variant="ghost"
              size="small"
              (click)="selectToday()">
              Today
            </app-button>
            <app-button
              variant="ghost"
              size="small"
              (click)="clearDate()">
              Clear
            </app-button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .datepicker {
      position: relative;
      width: 100%;
    }

    .calendar-icon {
      position: absolute;
      right: 12px;
      top: 50%;
      transform: translateY(-50%);
      pointer-events: none;
      color: #666;
    }

    .calendar-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.3);
      z-index: 998;
    }

    .calendar-popup {
      position: absolute;
      top: calc(100% + 4px);
      left: 0;
      background: white;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      padding: 16px;
      z-index: 999;
      min-width: 320px;
      animation: slideDown 0.2s ease-out;
    }

    @keyframes slideDown {
      from {
        opacity: 0;
        transform: translateY(-8px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .calendar-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 16px;
    }

    .month-year {
      font-size: 16px;
      font-weight: 600;
      color: #1a1a1a;
    }

    .weekday-headers {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 4px;
      margin-bottom: 8px;
    }

    .weekday {
      text-align: center;
      font-size: 12px;
      font-weight: 600;
      color: #666;
      padding: 8px 4px;
    }

    .calendar-days {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 4px;
    }

    .day-cell {
      aspect-ratio: 1;
      border: none;
      background: transparent;
      border-radius: 4px;
      font-size: 14px;
      cursor: pointer;
      transition: all 0.2s;
      color: #1a1a1a;
    }

    .day-cell:hover:not(:disabled) {
      background: #f5f5f5;
    }

    .day-cell.other-month {
      color: #bbb;
    }

    .day-cell.selected {
      background: #1976d2;
      color: white;
      font-weight: 600;
    }

    .day-cell.today {
      border: 2px solid #1976d2;
    }

    .day-cell:disabled {
      cursor: not-allowed;
      opacity: 0.5;
    }

    .calendar-footer {
      display: flex;
      justify-content: space-between;
      margin-top: 12px;
      padding-top: 12px;
      border-top: 1px solid #e0e0e0;
    }
  `]
})
export class Datepicker {
  @ViewChild('calendarPopup', { read: ElementRef }) calendarPopup?: ElementRef;

  value = model<Date | null>(null);
  placeholder = input<string>('Select date');
  label = input<string>('');
  required = input<boolean>(false);
  disabled = input<boolean>(false);
  error = input<string | null>(null);
  minDate = input<Date | null>(null);
  maxDate = input<Date | null>(null);

  protected showCalendar = signal(false);
  protected currentDate = signal(new Date());
  protected weekdays = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

  protected formattedValue = computed(() => {
    const date = this.value();
    if (!date) return '';
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const year = date.getFullYear();
    return `${month}/${day}/${year}`;
  });

  protected currentMonthYear = computed(() => {
    const date = this.currentDate();
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  });

  protected calendarDays = computed(() => {
    const current = this.currentDate();
    const year = current.getFullYear();
    const month = current.getMonth();
    const selectedDate = this.value();
    const today = new Date();

    // First day of month
    const firstDay = new Date(year, month, 1);
    const startingDayOfWeek = firstDay.getDay();

    // Last day of month
    const lastDay = new Date(year, month + 1, 0);
    const daysInMonth = lastDay.getDate();

    // Previous month days
    const prevMonth = new Date(year, month, 0);
    const daysInPrevMonth = prevMonth.getDate();

    const days: Array<{
      date: Date;
      day: number;
      currentMonth: boolean;
      selected: boolean;
      today: boolean;
    }> = [];

    // Previous month overflow days
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const day = daysInPrevMonth - i;
      const date = new Date(year, month - 1, day);
      days.push({
        date,
        day,
        currentMonth: false,
        selected: false,
        today: false
      });
    }

    // Current month days
    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(year, month, day);
      const isSelected = selectedDate
        ? date.toDateString() === selectedDate.toDateString()
        : false;
      const isToday = date.toDateString() === today.toDateString();

      days.push({
        date,
        day,
        currentMonth: true,
        selected: isSelected,
        today: isToday
      });
    }

    // Next month overflow days
    const remainingDays = 42 - days.length; // 6 weeks * 7 days
    for (let day = 1; day <= remainingDays; day++) {
      const date = new Date(year, month + 1, day);
      days.push({
        date,
        day,
        currentMonth: false,
        selected: false,
        today: false
      });
    }

    return days;
  });

  protected toggleCalendar(): void {
    if (this.disabled()) return;
    this.showCalendar.update(v => !v);
    if (this.showCalendar() && this.value()) {
      this.currentDate.set(new Date(this.value()!));
    }
  }

  protected closeCalendar(): void {
    this.showCalendar.set(false);
  }

  protected previousMonth(): void {
    this.currentDate.update(date => {
      const newDate = new Date(date);
      newDate.setMonth(newDate.getMonth() - 1);
      return newDate;
    });
  }

  protected nextMonth(): void {
    this.currentDate.update(date => {
      const newDate = new Date(date);
      newDate.setMonth(newDate.getMonth() + 1);
      return newDate;
    });
  }

  protected selectDate(date: Date): void {
    const min = this.minDate();
    const max = this.maxDate();

    if (min && date < min) return;
    if (max && date > max) return;

    this.value.set(new Date(date));
    this.closeCalendar();
  }

  protected selectToday(): void {
    this.selectDate(new Date());
  }

  protected clearDate(): void {
    this.value.set(null);
    this.closeCalendar();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const clickedInside = target.closest('.datepicker');
    if (!clickedInside && this.showCalendar()) {
      this.closeCalendar();
    }
  }
}
