// ═══════════════════════════════════════════════════════════
// DIALOG SERVICE
// Part of the Fortune 500-grade HRMS design system
// Manages dialog/modal creation, lifecycle, and overlay management
// ═══════════════════════════════════════════════════════════

import {
  Injectable,
  ComponentRef,
  ApplicationRef,
  createComponent,
  EnvironmentInjector,
  Type,
  Injector,
  inject
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { DialogRef } from './dialog-ref';
import { DialogContainerComponent } from '../components/dialog-container/dialog-container';

/**
 * Configuration options for opening a dialog
 */
export interface DialogConfig<D = any> {
  /** Data to inject into the dialog component */
  data?: D;

  /** Width of the dialog (CSS value, e.g., '500px', '50%') */
  width?: string;

  /** Height of the dialog (CSS value, e.g., '400px', 'auto') */
  height?: string;

  /** Maximum width of the dialog */
  maxWidth?: string;

  /** Maximum height of the dialog */
  maxHeight?: string;

  /** Minimum width of the dialog */
  minWidth?: string;

  /** Minimum height of the dialog */
  minHeight?: string;

  /** Whether clicking the backdrop closes the dialog */
  disableClose?: boolean;

  /** Custom CSS class for the backdrop */
  backdropClass?: string | string[];

  /** Custom CSS class for the dialog panel */
  panelClass?: string | string[];

  /** Whether to show the close button */
  hasCloseButton?: boolean;

  /** Aria label for accessibility */
  ariaLabel?: string;

  /** Aria described by for accessibility */
  ariaDescribedBy?: string;

  /** Role attribute for accessibility */
  role?: 'dialog' | 'alertdialog';

  /** Auto focus on first focusable element */
  autoFocus?: boolean;
}

/**
 * Default dialog configuration
 */
const DEFAULT_CONFIG: DialogConfig = {
  width: '600px',
  height: 'auto',
  maxWidth: '90vw',
  maxHeight: '90vh',
  disableClose: false,
  hasCloseButton: true,
  role: 'dialog',
  autoFocus: true
};

@Injectable({
  providedIn: 'root',
})
export class DialogService {
  private readonly document = inject(DOCUMENT);
  private readonly appRef = inject(ApplicationRef);
  private readonly injector = inject(EnvironmentInjector);

  /** Map of all open dialogs */
  private openDialogs = new Map<string, DialogRef>();

  /** Counter for generating unique dialog IDs */
  private dialogIdCounter = 0;

  /**
   * Opens a dialog containing the given component.
   * @param component The component to display in the dialog
   * @param config Optional configuration for the dialog
   * @returns A reference to the opened dialog
   */
  open<T, D = any, R = any>(
    component: Type<T>,
    config?: DialogConfig<D>
  ): DialogRef<T, R> {
    // Merge with defaults
    const dialogConfig: DialogConfig<D> = { ...DEFAULT_CONFIG, ...config };

    // Generate unique ID
    const dialogId = `dialog-${this.dialogIdCounter++}`;

    // Create dialog reference
    const dialogRef = new DialogRef<T, R>(dialogId, dialogConfig.data);

    // Create the dialog container
    const containerRef = this.createDialogContainer(dialogRef, dialogConfig);

    // Create the content component
    const contentRef = this.createContentComponent(component, containerRef, dialogRef);

    // Store references
    dialogRef.containerRef = containerRef;
    dialogRef.componentRef = contentRef;

    // Store in open dialogs map
    this.openDialogs.set(dialogId, dialogRef);

    // Handle dialog closure
    dialogRef.afterClosed().subscribe(() => {
      this.removeDialog(dialogRef);
    });

    // Mark as opened after next tick (for animations)
    setTimeout(() => dialogRef._emitOpened(), 0);

    return dialogRef;
  }

  /**
   * Closes the specified dialog.
   * @param dialogRef The dialog reference to close
   * @param result Optional result to pass back
   */
  close<R = any>(dialogRef: DialogRef<any, R>, result?: R): void {
    if (!dialogRef) {
      return;
    }

    dialogRef.close(result);
  }

  /**
   * Closes all open dialogs.
   */
  closeAll(): void {
    const dialogs = Array.from(this.openDialogs.values());
    dialogs.forEach(dialog => this.close(dialog));
  }

  /**
   * Gets all currently open dialogs.
   * @returns Array of dialog references
   */
  getOpenDialogs(): DialogRef[] {
    return Array.from(this.openDialogs.values());
  }

  /**
   * Gets a dialog by its ID.
   * @param id The dialog ID
   * @returns The dialog reference or undefined
   */
  getDialogById(id: string): DialogRef | undefined {
    return this.openDialogs.get(id);
  }

  /**
   * Creates the dialog container component.
   */
  private createDialogContainer<D>(
    dialogRef: DialogRef,
    config: DialogConfig<D>
  ): ComponentRef<DialogContainerComponent> {
    // Create container component
    const containerRef = createComponent(DialogContainerComponent, {
      environmentInjector: this.injector
    });

    // Set container inputs
    const container = containerRef.instance;
    container.dialogRef = dialogRef;
    container.config = config;

    // Attach to application
    this.appRef.attachView(containerRef.hostView);

    // Append to body
    const domElem = (containerRef.hostView as any).rootNodes[0] as HTMLElement;
    this.document.body.appendChild(domElem);

    return containerRef;
  }

  /**
   * Creates the content component inside the dialog container.
   */
  private createContentComponent<T>(
    component: Type<T>,
    containerRef: ComponentRef<DialogContainerComponent>,
    dialogRef: DialogRef
  ): ComponentRef<T> {
    // Create an injector with the dialog ref
    const contentInjector = Injector.create({
      providers: [
        { provide: DialogRef, useValue: dialogRef }
      ],
      parent: this.injector
    });

    // Create the content component
    const contentRef = createComponent(component, {
      environmentInjector: this.injector,
      elementInjector: contentInjector
    });

    // Attach to container
    containerRef.instance.attachContentComponent(contentRef);

    return contentRef;
  }

  /**
   * Removes a dialog from the DOM and cleans up.
   */
  private removeDialog(dialogRef: DialogRef): void {
    // Remove from map
    this.openDialogs.delete(dialogRef.id);

    // Destroy container (which will destroy content too)
    if (dialogRef.containerRef) {
      this.appRef.detachView(dialogRef.containerRef.hostView);
      dialogRef.containerRef.destroy();
    }
  }
}
