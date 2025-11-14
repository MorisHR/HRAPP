import { Component, Input, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toolbar.html',
  styleUrl: './toolbar.scss',
})
export class Toolbar {
  @Input() color: 'primary' | 'secondary' | 'neutral' = 'primary';
  @Input() position: 'static' | 'fixed' | 'sticky' = 'static';
  @Input() elevated: boolean = true;

  @HostBinding('class')
  get hostClasses(): string {
    return [
      `toolbar-${this.color}`,
      `toolbar-${this.position}`,
      this.elevated ? 'toolbar-elevated' : ''
    ].join(' ');
  }

  @HostBinding('attr.role')
  get role(): string {
    return 'toolbar';
  }
}
