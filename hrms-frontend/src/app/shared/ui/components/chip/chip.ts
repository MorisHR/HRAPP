import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ChipColor = 'primary' | 'success' | 'warning' | 'error' | 'neutral';
export type ChipVariant = 'filled' | 'outlined';

@Component({
  selector: 'app-chip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chip.html',
  styleUrl: './chip.scss',
})
export class Chip {
  @Input() label: string = '';
  @Input() color: ChipColor = 'neutral';
  @Input() variant: ChipVariant = 'filled';
  @Input() removable: boolean = false;
  @Input() avatar: string = ''; // Image URL or initials
  @Input() icon: string = '';
  @Output() remove = new EventEmitter<void>();

  get chipClasses(): string {
    return `chip-${this.color} chip-${this.variant}`;
  }

  get isAvatarImage(): boolean {
    return this.avatar.startsWith('http') || this.avatar.startsWith('/') || this.avatar.startsWith('.');
  }

  get avatarInitials(): string {
    return this.avatar;
  }

  onRemove(event: Event): void {
    event.stopPropagation();
    this.remove.emit();
  }
}
