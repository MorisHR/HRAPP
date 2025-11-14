import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type CardElevation = 0 | 1 | 2 | 3 | 4 | 5;

@Component({
  selector: 'app-card',
  imports: [CommonModule],
  templateUrl: './card.html',
  styleUrl: './card.scss'
})
export class CardComponent {
  @Input() elevation: CardElevation = 1;
  @Input() hoverable: boolean = false;
  @Input() clickable: boolean = false;
  @Input() padding: 'none' | 'small' | 'medium' | 'large' = 'medium';

  get cardClasses(): string[] {
    return [
      'card',
      `card--elevation-${this.elevation}`,
      `card--padding-${this.padding}`,
      this.hoverable ? 'card--hoverable' : '',
      this.clickable ? 'card--clickable' : ''
    ].filter(Boolean);
  }
}
