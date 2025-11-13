import { Component, Input } from '@angular/core';

import { UserError } from '../../../core/services/error-handler.service';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [],
  templateUrl: './error-message.component.html',
  styleUrls: ['./error-message.component.scss']
})
export class ErrorMessageComponent {
  @Input() error: UserError | null = null;
  @Input() dismissible = false;

  onDismiss(): void {
    this.error = null;
  }

  copyErrorId(): void {
    if (this.error?.correlationId) {
      navigator.clipboard.writeText(this.error.correlationId).then(() => {
        // Could show a toast notification here
        console.log('Error ID copied to clipboard');
      });
    }
  }
}
