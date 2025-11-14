# Sidenav Component

**Location:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/sidenav/`

## Overview

A production-ready, enterprise-grade Sidenav/Drawer component built as a drop-in replacement for Angular Material's `mat-sidenav`. This component provides a fully-featured navigation drawer with multiple display modes, smooth animations, accessibility features, and responsive design.

## Files Generated

- `sidenav.ts` - Component TypeScript logic
- `sidenav.html` - Component HTML template
- `sidenav.scss` - Component styles with animations
- `USAGE_EXAMPLE.md` - Comprehensive usage documentation
- `README.md` - This file

## Features

### Core Functionality
- **Three Display Modes:**
  - `over` - Overlay mode (drawer floats over content with backdrop)
  - `push` - Push mode (drawer pushes content to the side)
  - `side` - Side mode (drawer is always visible alongside content)

- **Flexible Positioning:**
  - Left or right side positioning
  - Configurable width (default: 280px)

- **Backdrop Overlay:**
  - Optional backdrop for overlay mode
  - Click backdrop to close
  - Smooth fade animation

### Interactions
- **Programmatic Control:**
  - `open()` - Opens the sidenav
  - `close()` - Closes the sidenav
  - `toggle()` - Toggles open/closed state

- **Keyboard Support:**
  - ESC key closes the drawer (except in side mode)
  - Tab key for focus navigation
  - Focus trap within drawer when open

### Animations
- Smooth slide transitions (translateX)
- Backdrop fade in/out
- Uses CSS transforms for performance
- Respects `prefers-reduced-motion` accessibility setting

### Accessibility
- ARIA attributes (`role`, `aria-hidden`)
- Focus trap when drawer is open
- Focus restoration when closed
- Keyboard navigation support
- High contrast mode support
- Screen reader friendly

### Responsive Design
- Full-width drawer on mobile devices (max-width: 320px)
- Automatic mode switching on mobile (forces overlay mode)
- Touch-friendly interactions

### Theme Support
- Dark mode support via `prefers-color-scheme`
- Custom scrollbar styling
- High contrast mode support
- Configurable colors via CSS variables

## API Reference

### Inputs

```typescript
@Input() opened: boolean = false;          // Controls open/closed state
@Input() mode: 'over' | 'push' | 'side' = 'over';  // Display mode
@Input() position: 'left' | 'right' = 'left';      // Side position
@Input() width: string = '280px';          // Drawer width
@Input() backdrop: boolean = true;          // Show backdrop (over mode only)
```

### Outputs

```typescript
@Output() openedChange = new EventEmitter<boolean>();  // State change events
```

### Methods

```typescript
open(): void      // Opens the sidenav
close(): void     // Closes the sidenav
toggle(): void    // Toggles the open/closed state
```

## Component Architecture

### TypeScript (sidenav.ts)
- Standalone Angular component
- Change detection: OnPush for performance
- Implements AfterViewInit and OnDestroy lifecycle hooks
- ViewChild reference to sidenav content for focus management
- HostListener for keyboard events
- Private methods for focus trap and element management

### HTML (sidenav.html)
- Container structure with backdrop
- Aside element for drawer
- Content projection for main content
- Dynamic classes and styles based on inputs
- Accessibility attributes

### SCSS (sidenav.scss)
- Fixed positioning for overlay modes
- CSS transforms for animations
- Z-index layering (backdrop: 999, sidenav: 1000)
- Responsive breakpoints (@media queries)
- Custom scrollbar styling
- Dark theme support
- Print styles
- Reduced motion support

## Integration with UI Module

The component is exported from `src/app/shared/ui/ui.module.ts`:

```typescript
import { Sidenav } from './components/sidenav/sidenav';

@NgModule({
  imports: [..., Sidenav],
  exports: [..., Sidenav]
})
export class UiModule {}

export { Sidenav } from './components/sidenav/sidenav';
```

## Usage Example

```typescript
// Component
import { Sidenav } from '@app/shared/ui';

@Component({
  standalone: true,
  imports: [Sidenav],
  template: `
    <app-sidenav
      [(opened)]="isOpen"
      [mode]="'over'"
      [position]="'left'">

      <!-- Navigation Content -->
      <nav class="menu">
        <a href="/dashboard">Dashboard</a>
        <a href="/profile">Profile</a>
      </nav>

      <!-- Main Content -->
      <div sidenavContent>
        <button (click)="isOpen = !isOpen">Toggle Menu</button>
        <h1>Page Content</h1>
      </div>
    </app-sidenav>
  `
})
export class MyComponent {
  isOpen = false;
}
```

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Performance Considerations

- Uses CSS transforms for GPU acceleration
- OnPush change detection strategy
- Efficient focus management
- Minimal DOM manipulation
- Will-change property for optimized animations

## Testing

The component can be tested using Angular testing utilities:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Sidenav } from './sidenav';

describe('Sidenav', () => {
  let component: Sidenav;
  let fixture: ComponentFixture<Sidenav>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Sidenav]
    }).compileComponents();

    fixture = TestBed.createComponent(Sidenav);
    component = fixture.componentInstance;
  });

  it('should open when open() is called', () => {
    component.open();
    expect(component.opened).toBe(true);
  });
});
```

## Migration from mat-sidenav

This component provides API compatibility with Angular Material's mat-sidenav:

### Before (Material)
```html
<mat-sidenav-container>
  <mat-sidenav #sidenav mode="over">
    Nav content
  </mat-sidenav>
  <mat-sidenav-content>
    Main content
  </mat-sidenav-content>
</mat-sidenav-container>
```

### After (Custom Component)
```html
<app-sidenav [(opened)]="isOpen" mode="over">
  Nav content
  <div sidenavContent>
    Main content
  </div>
</app-sidenav>
```

## Customization

### Custom Styling
```scss
app-sidenav {
  ::ng-deep .sidenav {
    background-color: #1e1e1e;
    color: #ffffff;
  }

  ::ng-deep .sidenav-backdrop {
    background-color: rgba(255, 0, 0, 0.5);
  }
}
```

### Custom Width
```html
<app-sidenav [width]="'400px'">
  <!-- content -->
</app-sidenav>
```

## Future Enhancements

Potential improvements for future iterations:
- Swipe gestures for mobile
- Multiple sidenav support
- Resize handle for user-adjustable width
- Animation duration configuration
- Custom backdrop templates

## Support

For issues, questions, or contributions, please refer to the project documentation or contact the development team.

---

**Version:** 1.0.0
**Last Updated:** 2025-11-14
**Compatibility:** Angular 15+
