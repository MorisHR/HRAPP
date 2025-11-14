import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeColor = 'primary' | 'success' | 'warning' | 'error';
export type BadgePosition = 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './badge.html',
  styleUrl: './badge.scss',
})
export class Badge {
  @Input() content: string | number = '';
  @Input() color: BadgeColor = 'primary';
  @Input() position: BadgePosition = 'top-right';
  @Input() dot: boolean = false;

  get positionClasses(): string {
    return `badge-${this.position}`;
  }

  get colorClass(): string {
    return `badge-${this.color}`;
  }

  get shouldShowContent(): boolean {
    return !this.dot && (this.content !== null && this.content !== undefined && this.content !== '');
  }
}
