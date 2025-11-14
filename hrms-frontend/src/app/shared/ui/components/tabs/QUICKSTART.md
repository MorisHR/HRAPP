# Tabs Component - Quick Start

## 30-Second Setup

### 1. Import
```typescript
import { UiModule, Tab } from '@app/shared/ui/ui.module';

@Component({
  imports: [UiModule],
  // ...
})
```

### 2. Define Tabs
```typescript
tabs: Tab[] = [
  { label: 'First', value: 'first' },
  { label: 'Second', value: 'second' },
];
activeTab = 'first';
```

### 3. Use in Template
```html
<app-tabs [tabs]="tabs" [activeTab]="activeTab" (tabChange)="activeTab = $event">
  <div *ngIf="activeTab === 'first'">Content 1</div>
  <div *ngIf="activeTab === 'second'">Content 2</div>
</app-tabs>
```

## Variants

```html
<!-- Default (with border) -->
<app-tabs variant="default" ...></app-tabs>

<!-- Pills (rounded background) -->
<app-tabs variant="pills" ...></app-tabs>

<!-- Underline (minimal) -->
<app-tabs variant="underline" ...></app-tabs>
```

## With Icons

```typescript
tabs: Tab[] = [
  { label: 'Dashboard', value: 'dash', icon: '📊' },
  { label: 'Users', value: 'users', icon: '👥' },
];
```

## Disabled Tabs

```typescript
tabs: Tab[] = [
  { label: 'Available', value: 'avail' },
  { label: 'Locked', value: 'locked', disabled: true },
];
```

## Keyboard Navigation

- **Arrow Left/Right**: Navigate tabs
- **Home/End**: First/last tab
- **Enter/Space**: Activate tab

## Common Patterns

### Step-by-Step Wizard
```typescript
tabs = [
  { label: 'Step 1', value: 's1' },
  { label: 'Step 2', value: 's2', disabled: true },
  { label: 'Review', value: 'review', disabled: true },
];

completeStep1() {
  this.tabs[1].disabled = false;
  this.activeTab = 's2';
}
```

### Settings Panel
```typescript
tabs = [
  { label: 'Profile', value: 'profile', icon: '👤' },
  { label: 'Security', value: 'security', icon: '🔒' },
  { label: 'Notifications', value: 'notif', icon: '🔔' },
];
```

### Dynamic Tabs
```typescript
addTab() {
  this.tabs = [...this.tabs, {
    label: `Tab ${this.tabs.length + 1}`,
    value: `tab${this.tabs.length + 1}`
  }];
}
```

## That's It!

For more examples, see:
- **README.md** - Complete documentation
- **tabs.example.ts** - 6 working examples
- **tabs.demo.html** - Visual demo

## Questions?

Common issues:
- Content not updating? Check `*ngIf` conditions match tab values
- Tabs not showing? Verify tabs array is not empty
- Keyboard not working? Component needs focus
