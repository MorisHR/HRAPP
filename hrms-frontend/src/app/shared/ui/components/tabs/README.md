# Tabs Component

A production-ready, accessible tabs component to replace Angular Material's mat-tab.

## Features

- **Multiple Variants**: Default, pills, and underline styles
- **Full Keyboard Navigation**: Arrow keys, Home, End, Enter, Space
- **Accessibility**: ARIA attributes, focus management, screen reader support
- **Icon Support**: Optional icons for each tab
- **Disabled State**: Individual tabs can be disabled
- **Smooth Animations**: Transition effects for active indicators
- **Responsive**: Horizontal scrolling on mobile devices
- **Design System Integration**: Uses HRMS design tokens
- **Performance**: TrackBy optimization, smooth scrolling

## Basic Usage

### 1. Import in your module

```typescript
import { UiModule, Tab } from '@app/shared/ui/ui.module';

@Component({
  standalone: true,
  imports: [UiModule],
  // ...
})
export class MyComponent {}
```

### 2. Define tabs in your component

```typescript
export class MyComponent {
  tabs: Tab[] = [
    { label: 'Overview', value: 'overview' },
    { label: 'Details', value: 'details' },
    { label: 'Settings', value: 'settings', disabled: true },
  ];

  activeTab = 'overview';

  onTabChange(value: string): void {
    this.activeTab = value;
    console.log('Active tab:', value);
  }
}
```

### 3. Use in your template

```html
<app-tabs
  [tabs]="tabs"
  [activeTab]="activeTab"
  [variant]="'default'"
  (tabChange)="onTabChange($event)">

  <div *ngIf="activeTab === 'overview'">
    <!-- Overview content -->
  </div>

  <div *ngIf="activeTab === 'details'">
    <!-- Details content -->
  </div>

  <div *ngIf="activeTab === 'settings'">
    <!-- Settings content -->
  </div>
</app-tabs>
```

## Variants

### Default (with bottom border)

```html
<app-tabs [tabs]="tabs" [activeTab]="activeTab" variant="default"></app-tabs>
```

### Pills (rounded background)

```html
<app-tabs [tabs]="tabs" [activeTab]="activeTab" variant="pills"></app-tabs>
```

### Underline (minimal style)

```html
<app-tabs [tabs]="tabs" [activeTab]="activeTab" variant="underline"></app-tabs>
```

## Advanced Examples

### With Icons

```typescript
tabs: Tab[] = [
  {
    label: 'Dashboard',
    value: 'dashboard',
    icon: '<svg>...</svg>' // Or icon font class
  },
  {
    label: 'Users',
    value: 'users',
    icon: '👥' // Emoji works too
  },
];
```

### Disabled Tabs

```typescript
tabs: Tab[] = [
  { label: 'Available', value: 'available' },
  { label: 'Coming Soon', value: 'coming-soon', disabled: true },
];
```

### Dynamic Tabs

```typescript
export class MyComponent {
  tabs: Tab[] = [];
  activeTab = '';

  ngOnInit(): void {
    this.loadTabs();
  }

  loadTabs(): void {
    // Load from API or service
    this.tabs = [
      { label: 'Tab 1', value: 'tab1' },
      { label: 'Tab 2', value: 'tab2' },
    ];
    this.activeTab = this.tabs[0].value;
  }

  addTab(): void {
    const newTab: Tab = {
      label: `Tab ${this.tabs.length + 1}`,
      value: `tab${this.tabs.length + 1}`
    };
    this.tabs = [...this.tabs, newTab];
  }
}
```

## Props

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `tabs` | `Tab[]` | `[]` | Array of tab configuration objects |
| `activeTab` | `string` | `''` | Value of the currently active tab |
| `variant` | `'default' \| 'pills' \| 'underline'` | `'default'` | Visual style variant |
| `tabChange` | `EventEmitter<string>` | - | Emits when a tab is selected |

## Tab Interface

```typescript
interface Tab {
  label: string;        // Display text
  value: string;        // Unique identifier
  disabled?: boolean;   // Whether tab is disabled
  icon?: string;        // HTML string for icon (SVG, emoji, etc.)
}
```

## Keyboard Navigation

| Key | Action |
|-----|--------|
| `Arrow Right` | Move to next enabled tab |
| `Arrow Left` | Move to previous enabled tab |
| `Home` | Move to first enabled tab |
| `End` | Move to last enabled tab |
| `Enter` or `Space` | Activate focused tab |
| `Tab` | Move focus to next element |

## Accessibility

- Full ARIA support (`role="tablist"`, `role="tab"`, `role="tabpanel"`)
- Keyboard navigation follows WAI-ARIA best practices
- Focus indicators meet WCAG 2.1 requirements
- Screen reader friendly labels and states
- High contrast mode support
- Reduced motion support

## Styling Customization

The component uses design tokens from `/src/styles/`. To customize:

### Colors
Edit `/src/styles/_colors.scss` to change primary colors, borders, text colors.

### Spacing
Edit `/src/styles/_spacing.scss` to adjust padding and gaps.

### Animations
Edit `/src/styles/_motion.scss` to change transition durations and easing.

### Custom Styles
Override in your component's styles:

```scss
app-tabs {
  .tabs__button {
    // Your custom styles
  }
}
```

## Responsive Behavior

- **Desktop**: Full horizontal layout with hover effects
- **Tablet (< 768px)**: Reduced padding, smaller icons
- **Mobile (< 480px)**: Horizontal scroll, compact spacing

## Performance Considerations

- Uses `trackBy` for efficient list rendering
- Smooth scrolling optimized for mobile
- Minimal re-renders with OnPush strategy (can be enabled)
- Lazy content projection with ng-content

## Browser Support

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- Mobile browsers (iOS Safari, Chrome Mobile)

## Migration from mat-tab

### Before (Material):
```html
<mat-tab-group [(selectedIndex)]="selectedIndex">
  <mat-tab label="First">Content 1</mat-tab>
  <mat-tab label="Second">Content 2</mat-tab>
</mat-tab-group>
```

### After (Custom):
```typescript
tabs = [
  { label: 'First', value: 'first' },
  { label: 'Second', value: 'second' }
];
activeTab = 'first';
```

```html
<app-tabs [tabs]="tabs" [activeTab]="activeTab" (tabChange)="activeTab = $event">
  <div *ngIf="activeTab === 'first'">Content 1</div>
  <div *ngIf="activeTab === 'second'">Content 2</div>
</app-tabs>
```

## Examples

### Form Wizard

```typescript
tabs: Tab[] = [
  { label: 'Personal Info', value: 'personal' },
  { label: 'Address', value: 'address' },
  { label: 'Review', value: 'review', disabled: !this.isFormValid },
];
```

### Settings Panel

```typescript
tabs: Tab[] = [
  { label: 'Profile', value: 'profile', icon: '👤' },
  { label: 'Security', value: 'security', icon: '🔒' },
  { label: 'Notifications', value: 'notifications', icon: '🔔' },
];
```

### Data Visualization

```html
<app-tabs [tabs]="chartTabs" [activeTab]="activeChart" variant="underline">
  <app-line-chart *ngIf="activeChart === 'line'"></app-line-chart>
  <app-bar-chart *ngIf="activeChart === 'bar'"></app-bar-chart>
  <app-pie-chart *ngIf="activeChart === 'pie'"></app-pie-chart>
</app-tabs>
```

## Troubleshooting

### Tabs not showing
- Verify `tabs` array is not empty
- Check that `activeTab` matches one of the tab values

### Keyboard navigation not working
- Ensure component has focus
- Verify no disabled tabs are being focused

### Content not updating
- Check `*ngIf` conditions match tab values
- Ensure change detection is running

### Styles not applying
- Import styles in `angular.json` if needed
- Check that design token files are accessible

## Contributing

When adding new features:
1. Update this README
2. Add accessibility attributes
3. Test keyboard navigation
4. Verify responsive behavior
5. Check all three variants work

## License

Part of the HRMS Fortune 500-grade design system.
