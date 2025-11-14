# Menu Component

Production-ready dropdown menu component to replace Angular Material's `mat-menu`.

## Location
`/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/menu/`

## Features

- **Trigger-based positioning** - Positions relative to trigger element
- **Menu items with icons** - Support for Font Awesome, Material Icons, etc.
- **Disabled items** - Visual and functional disabled state
- **Dividers** - Separator lines between menu items
- **Click outside to close** - Automatically closes when clicking outside
- **ESC to close** - Keyboard accessibility
- **Keyboard navigation** - Arrow keys, Home, End, Enter/Space
- **Submenu support** - Nested menu items (optional)
- **Smooth animations** - Slide-in animation with reduced motion support
- **Smart positioning** - Auto-adjusts to stay within viewport
- **Responsive design** - Mobile-friendly
- **Dark mode support** - Ready for dark theme
- **Accessibility** - Full ARIA support, screen reader compatible

## API

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `trigger` | `HTMLElement` | required | Reference to trigger element |
| `items` | `MenuItem[]` | `[]` | Array of menu items |
| `position` | `MenuPosition` | `'bottom-left'` | Menu position relative to trigger |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `itemClick` | `EventEmitter<any>` | Emits when menu item is clicked |

### Methods

| Method | Description |
|--------|-------------|
| `open()` | Opens the menu |
| `close()` | Closes the menu |
| `toggle()` | Toggles menu open/closed state |

### Types

```typescript
interface MenuItem {
  label: string;
  value: any;
  icon?: string;
  disabled?: boolean;
  divider?: boolean;
  submenu?: MenuItem[];
}

type MenuPosition = 'bottom-left' | 'bottom-right' | 'top-left' | 'top-right';
```

## Usage

### Basic Example

```typescript
import { MenuComponent, MenuItem } from '@app/shared/ui';

@Component({
  selector: 'app-example',
  template: `
    <button #menuTrigger>Open Menu</button>
    <app-menu
      [trigger]="menuTrigger"
      [items]="menuItems"
      (itemClick)="handleAction($event)">
    </app-menu>
  `
})
export class ExampleComponent {
  menuItems: MenuItem[] = [
    { label: 'Profile', value: 'profile' },
    { label: 'Settings', value: 'settings' },
    { label: 'Logout', value: 'logout' }
  ];

  handleAction(value: any): void {
    console.log('Menu action:', value);
  }
}
```

### With Icons

```typescript
menuItems: MenuItem[] = [
  { label: 'Edit', value: 'edit', icon: 'fas fa-edit' },
  { label: 'Delete', value: 'delete', icon: 'fas fa-trash' },
  { label: 'Share', value: 'share', icon: 'fas fa-share' }
];
```

### With Dividers

```typescript
menuItems: MenuItem[] = [
  { label: 'New', value: 'new', icon: 'fas fa-plus' },
  { label: 'Open', value: 'open', icon: 'fas fa-folder-open' },
  { label: '', value: '', divider: true }, // Divider
  { label: 'Exit', value: 'exit', icon: 'fas fa-sign-out-alt' }
];
```

### With Disabled Items

```typescript
menuItems: MenuItem[] = [
  { label: 'Save', value: 'save', icon: 'fas fa-save' },
  { label: 'Save As', value: 'save-as', icon: 'fas fa-save', disabled: true }
];
```

### Different Positions

```html
<!-- Bottom Left (default) -->
<app-menu [trigger]="trigger" [items]="items"></app-menu>

<!-- Bottom Right -->
<app-menu [trigger]="trigger" [items]="items" position="bottom-right"></app-menu>

<!-- Top Left -->
<app-menu [trigger]="trigger" [items]="items" position="top-left"></app-menu>

<!-- Top Right -->
<app-menu [trigger]="trigger" [items]="items" position="top-right"></app-menu>
```

### Programmatic Control

```typescript
@ViewChild('menu') menu!: MenuComponent;

openMenu(): void {
  this.menu.open();
}

closeMenu(): void {
  this.menu.close();
}

toggleMenu(): void {
  this.menu.toggle();
}
```

## Keyboard Navigation

- **Arrow Down** - Move to next item
- **Arrow Up** - Move to previous item
- **Home** - Move to first item
- **End** - Move to last item
- **Enter/Space** - Select focused item
- **Escape** - Close menu

## Migration from mat-menu

### Before (Angular Material)

```html
<button [matMenuTriggerFor]="menu">Menu</button>
<mat-menu #menu="matMenu">
  <button mat-menu-item (click)="action1()">Action 1</button>
  <button mat-menu-item (click)="action2()">Action 2</button>
  <mat-divider></mat-divider>
  <button mat-menu-item disabled>Disabled</button>
</mat-menu>
```

### After (Custom Menu)

```html
<button #menuTrigger>Menu</button>
<app-menu
  [trigger]="menuTrigger"
  [items]="menuItems"
  (itemClick)="handleAction($event)">
</app-menu>
```

```typescript
menuItems: MenuItem[] = [
  { label: 'Action 1', value: 'action1' },
  { label: 'Action 2', value: 'action2' },
  { label: '', value: '', divider: true },
  { label: 'Disabled', value: 'disabled', disabled: true }
];

handleAction(value: any): void {
  switch (value) {
    case 'action1':
      this.action1();
      break;
    case 'action2':
      this.action2();
      break;
  }
}
```

## Styling

The component uses design tokens from the HRMS design system:

- Colors: `_colors.scss`
- Spacing: `_spacing.scss`
- Elevation: `_elevation.scss`
- Motion: `_motion.scss`

To customize styles, override CSS classes:

```scss
.menu-panel {
  // Custom panel styles
}

.menu-item {
  // Custom item styles
}
```

## Accessibility

- Full ARIA support with `role="menu"` and `role="menuitem"`
- Keyboard navigation support
- Focus management
- Screen reader compatible
- High contrast mode support

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Examples

See `USAGE_EXAMPLE.ts` for comprehensive examples including:

1. Basic menu
2. Menu with icons
3. Menu with dividers
4. Menu with disabled items
5. Programmatic control
6. Different positions
7. Context menu (right-click)

## Notes

- Menu automatically adjusts position to stay within viewport
- Animations respect `prefers-reduced-motion` setting
- Component cleans up event listeners on destroy
- Supports both light and dark themes
