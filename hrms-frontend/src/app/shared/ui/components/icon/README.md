# Icon Component

Production-ready icon component to replace Angular Material's `mat-icon` with support for multiple icon libraries.

## Location
`/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/icon/`

## Features

- **Multiple Icon Libraries**: Material Icons, Heroicons, Lucide
- **Three Size Variants**: Small (16px), Medium (24px), Large (32px)
- **Color Customization**: CSS colors or design tokens
- **Inline SVG Rendering**: Performance-optimized SVG rendering
- **Fallback Support**: Automatic fallback for missing icons
- **Accessibility**: Built-in ARIA labels and role attributes
- **20+ Common Icons**: Pre-registered Material Design icons

## Usage

### Basic Usage

```typescript
import { IconComponent } from '@app/shared/ui';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [IconComponent],
  template: `
    <app-icon name="home"></app-icon>
  `
})
export class ExampleComponent {}
```

### With Custom Size

```html
<!-- Small (16px) -->
<app-icon name="search" size="small"></app-icon>

<!-- Medium (24px) - Default -->
<app-icon name="menu" size="medium"></app-icon>

<!-- Large (32px) -->
<app-icon name="settings" size="large"></app-icon>
```

### With Custom Color

```html
<!-- CSS color -->
<app-icon name="check" color="#28a745"></app-icon>

<!-- Design token -->
<app-icon name="error" color="var(--color-error)"></app-icon>

<!-- Inline style -->
<app-icon name="info" [color]="'#007bff'"></app-icon>
```

### Different Icon Libraries

```html
<!-- Material Icons (default) -->
<app-icon name="home" library="material"></app-icon>

<!-- Heroicons (outline style) -->
<app-icon name="home" library="heroicons"></app-icon>

<!-- Lucide Icons -->
<app-icon name="home" library="lucide"></app-icon>
```

### In Buttons

```html
<button class="btn btn-primary">
  <app-icon name="add" size="small"></app-icon>
  Create New
</button>

<button class="btn btn-danger">
  <app-icon name="delete" size="small"></app-icon>
  Delete
</button>
```

### With Accessibility

```html
<app-icon
  name="settings"
  ariaLabel="Open settings menu">
</app-icon>
```

## API

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `string` | `''` | Icon name (e.g., 'home', 'menu', 'search') |
| `size` | `'small' \| 'medium' \| 'large'` | `'medium'` | Icon size (16px, 24px, 32px) |
| `color` | `string` | `undefined` | Custom color (CSS color or design token) |
| `library` | `'material' \| 'heroicons' \| 'lucide'` | `'material'` | Icon library to use |
| `ariaLabel` | `string` | `undefined` | Accessibility label (defaults to icon name) |

### CSS Classes

| Class | Description |
|-------|-------------|
| `.app-icon` | Base icon class |
| `.app-icon--small` | Small size (16px) |
| `.app-icon--medium` | Medium size (24px) |
| `.app-icon--large` | Large size (32px) |
| `.app-icon--fallback` | Applied when icon not found |
| `.app-icon--interactive` | Clickable/interactive state |
| `.app-icon--disabled` | Disabled state |
| `.app-icon--spin` | Spinning animation |
| `.app-icon--primary` | Primary color |
| `.app-icon--success` | Success color |
| `.app-icon--warning` | Warning color |
| `.app-icon--error` | Error color |
| `.app-icon--info` | Info color |
| `.app-icon--muted` | Muted/secondary color |

## Available Material Icons

### Navigation
- `home`, `menu`, `close`, `arrow_back`, `arrow_forward`
- `expand_more`, `expand_less`, `chevron_right`, `chevron_left`

### Actions
- `search`, `add`, `remove`, `edit`, `delete`, `save`
- `check`, `cancel`, `refresh`

### Content
- `copy`, `content_copy`, `attach_file`, `download`, `upload`

### Communication
- `mail`, `notifications`, `chat`

### User
- `person`, `person_add`, `group`, `account_circle`

### Settings
- `settings`

### Status
- `info`, `warning`, `error`, `check_circle`

### Other
- `visibility`, `visibility_off`, `filter_list`, `sort`
- `calendar`, `more_vert`, `more_horiz`

## Icon Registry Service

### Register Custom Icons

```typescript
import { IconRegistryService } from '@app/shared/ui';

constructor(private iconRegistry: IconRegistryService) {
  // Register a custom Material icon
  this.iconRegistry.registerIcon(
    'custom-icon',
    '<path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>',
    'material'
  );

  // Register a custom Heroicon
  this.iconRegistry.registerIcon(
    'custom-hero',
    '<path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15"/>',
    'heroicons'
  );
}
```

### Check Icon Availability

```typescript
// Check if icon exists
if (this.iconRegistry.hasIcon('home', 'material')) {
  console.log('Home icon is available');
}

// Get all available icons for a library
const materialIcons = this.iconRegistry.getAvailableIcons('material');
console.log('Material icons:', materialIcons);
```

## Styling Examples

### Custom Utility Classes

```html
<!-- With spacing -->
<app-icon name="search" class="app-icon--mr-sm"></app-icon> Search

<!-- With color classes -->
<app-icon name="check_circle" class="app-icon--success"></app-icon>
<app-icon name="error" class="app-icon--error"></app-icon>
<app-icon name="info" class="app-icon--info"></app-icon>

<!-- Interactive state -->
<app-icon name="settings" class="app-icon--interactive"></app-icon>

<!-- Spinning animation -->
<app-icon name="refresh" class="app-icon--spin"></app-icon>

<!-- Disabled state -->
<app-icon name="edit" class="app-icon--disabled"></app-icon>
```

### Custom SCSS

```scss
.my-component {
  .app-icon {
    // Custom size
    &--custom {
      width: 20px;
      height: 20px;
    }

    // Custom color
    &--brand {
      color: var(--brand-color);
    }

    // Custom animation
    &--pulse {
      animation: pulse 2s ease-in-out infinite;
    }
  }
}
```

## Migration from mat-icon

### Before (Material)
```html
<mat-icon>home</mat-icon>
<mat-icon color="primary">settings</mat-icon>
<mat-icon [style.font-size.px]="16">search</mat-icon>
```

### After (Icon Component)
```html
<app-icon name="home"></app-icon>
<app-icon name="settings" color="var(--color-primary)"></app-icon>
<app-icon name="search" size="small"></app-icon>
```

## Performance

- **Inline SVG**: No external icon font loading
- **Tree-shaking**: Only used icons are included
- **Change Detection**: OnPush strategy for optimal performance
- **No DOM queries**: All icons rendered via templates
- **Lazy loading**: Icons loaded on-demand from registry

## Accessibility

- Automatic `role="img"` attribute
- Automatic `aria-label` (defaults to icon name)
- Custom `ariaLabel` input for specific contexts
- `aria-hidden="true"` on inner SVG elements
- High contrast mode support
- Screen reader friendly

## Browser Support

- Chrome/Edge: 90+
- Firefox: 88+
- Safari: 14+
- All modern browsers with SVG support

## Files

- `icon.ts` - Component logic and TypeScript types
- `icon.html` - Template (simple innerHTML binding)
- `icon.scss` - Comprehensive styles with utilities
- `icon-registry.service.ts` - Icon management service

## Notes

- Icons are registered globally in the IconRegistryService
- Fallback icon (info/warning icon) shown for missing icons
- Console warning logged when icon not found
- SVG sanitization handled by Angular's DomSanitizer
- ViewEncapsulation.None for global style cascade
