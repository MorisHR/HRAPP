# Sidenav Component Usage

## Production-Ready Sidenav/Drawer Component
A fully-featured Material Design inspired sidenav component to replace mat-sidenav.

## Features
- **3 Display Modes**: 'over' (overlay), 'push' (pushes content), 'side' (always visible)
- **Positioning**: Left or right side positioning
- **Backdrop Overlay**: Optional backdrop with click-to-close
- **Smooth Animations**: Slide and fade transitions
- **Keyboard Support**: ESC key to close, Tab focus trap
- **Accessibility**: ARIA attributes, focus management
- **Responsive**: Full-width on mobile devices
- **Theme Support**: Dark mode and high contrast support

## Import

```typescript
import { Sidenav } from '@app/shared/ui';
```

## Basic Usage

### Template
```html
<app-sidenav
  [(opened)]="isOpen"
  [mode]="'over'"
  [position]="'left'"
  [width]="'280px'"
  [backdrop]="true">

  <!-- Sidenav Content -->
  <div class="sidenav-menu">
    <h2>Navigation</h2>
    <ul>
      <li><a href="/dashboard">Dashboard</a></li>
      <li><a href="/profile">Profile</a></li>
      <li><a href="/settings">Settings</a></li>
    </ul>
  </div>

  <!-- Main Content (optional) -->
  <div sidenavContent>
    <button (click)="toggleSidenav()">Toggle Menu</button>
    <h1>Main Content Area</h1>
    <p>Your page content goes here...</p>
  </div>
</app-sidenav>
```

### Component
```typescript
import { Component } from '@angular/core';
import { Sidenav } from '@app/shared/ui';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [Sidenav],
  templateUrl: './layout.html',
})
export class LayoutComponent {
  isOpen = false;

  toggleSidenav() {
    this.isOpen = !this.isOpen;
  }
}
```

## API

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `opened` | `boolean` | `false` | Controls the open/closed state |
| `mode` | `'over' \| 'push' \| 'side'` | `'over'` | Display mode of the sidenav |
| `position` | `'left' \| 'right'` | `'left'` | Side position of the drawer |
| `width` | `string` | `'280px'` | Width of the sidenav |
| `backdrop` | `boolean` | `true` | Show backdrop overlay (over mode only) |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `openedChange` | `EventEmitter<boolean>` | Emits when opened state changes |

### Methods

| Method | Description |
|--------|-------------|
| `open()` | Opens the sidenav |
| `close()` | Closes the sidenav |
| `toggle()` | Toggles the open/closed state |

## Display Modes

### Over Mode (Default)
Sidenav overlays the content with a backdrop.
```html
<app-sidenav [mode]="'over'" [(opened)]="isOpen">
  <!-- content -->
</app-sidenav>
```

### Push Mode
Sidenav pushes the main content aside.
```html
<app-sidenav [mode]="'push'" [(opened)]="isOpen">
  <!-- content -->
</app-sidenav>
```

### Side Mode
Sidenav is always visible alongside content.
```html
<app-sidenav [mode]="'side'" [opened]="true">
  <!-- content -->
</app-sidenav>
```

## Advanced Examples

### With ViewChild Control
```typescript
import { Component, ViewChild } from '@angular/core';
import { Sidenav } from '@app/shared/ui';

@Component({
  selector: 'app-layout',
  template: `
    <app-sidenav #sidenav [mode]="'over'">
      <nav>Navigation Menu</nav>
      <div sidenavContent>
        <button (click)="sidenav.open()">Open</button>
        <button (click)="sidenav.close()">Close</button>
      </div>
    </app-sidenav>
  `
})
export class LayoutComponent {
  @ViewChild('sidenav') sidenav!: Sidenav;
}
```

### Right-Side Navigation
```html
<app-sidenav
  [position]="'right'"
  [width]="'320px'"
  [(opened)]="settingsPanelOpen">
  <div class="settings-panel">
    <h2>Settings</h2>
    <!-- settings content -->
  </div>
</app-sidenav>
```

### Responsive Layout
```typescript
@Component({
  selector: 'app-layout',
  template: `
    <app-sidenav
      [mode]="isMobile ? 'over' : 'side'"
      [(opened)]="sidenavOpen">
      <!-- navigation -->
    </app-sidenav>
  `
})
export class LayoutComponent {
  isMobile = window.innerWidth < 768;
  sidenavOpen = !this.isMobile;

  @HostListener('window:resize')
  onResize() {
    this.isMobile = window.innerWidth < 768;
    this.sidenavOpen = !this.isMobile;
  }
}
```

### With Custom Styling
```html
<app-sidenav
  class="custom-sidenav"
  [width]="'300px'"
  [(opened)]="isOpen">
  <!-- content -->
</app-sidenav>
```

```scss
.custom-sidenav {
  ::ng-deep .sidenav {
    background: linear-gradient(180deg, #667eea 0%, #764ba2 100%);
    color: white;
  }
}
```

## Keyboard Support

- **ESC**: Closes the sidenav (when mode is 'over' or 'push')
- **TAB**: Focus cycles through focusable elements within the sidenav when open
- **Shift+TAB**: Reverse focus cycling

## Accessibility Features

- ARIA attributes for screen readers
- Focus trap when sidenav is open
- Focus restoration when closed
- Keyboard navigation support
- High contrast mode support

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Notes

- The component uses Angular animations for smooth transitions
- Backdrop only shows in 'over' mode
- Mobile devices automatically switch to overlay mode for 'push' and 'side' modes
- Supports reduced motion preferences
- Dark theme support via CSS media queries
