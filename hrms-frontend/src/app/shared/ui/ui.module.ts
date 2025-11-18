// ═══════════════════════════════════════════════════════════
// PREMIUM UI MODULE
// Part of the Fortune 500-grade HRMS design system
// Central module for shared UI components, directives, and pipes
// ═══════════════════════════════════════════════════════════

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from './components/button/button';
import { CardComponent } from './components/card/card';
import { CheckboxComponent } from './components/checkbox/checkbox';
import { InputComponent } from './components/input/input';
import { SelectComponent } from './components/select/select';
import { DialogContainerComponent } from './components/dialog-container/dialog-container';
import { Tabs } from './components/tabs/tabs';
import { ProgressBar } from './components/progress-bar/progress-bar';
import { ProgressSpinner } from './components/progress-spinner/progress-spinner';
import { DialogService } from './services/dialog';
import { ToastService } from './services/toast';
import { ToastContainerComponent } from './components/toast-container/toast-container';
import { Radio } from './components/radio/radio';
import { RadioGroup } from './components/radio-group/radio-group';
import { TableComponent } from './components/table/table';
import { MenuComponent } from './components/menu/menu';
import { TooltipDirective } from './directives/tooltip.directive';
import { Toggle } from './components/toggle/toggle';
import { Stepper } from './components/stepper/stepper';
import { Autocomplete } from './components/autocomplete/autocomplete';
import { IconComponent } from './components/icon/icon';
import { Badge } from './components/badge/badge';
import { Chip } from './components/chip/chip';
import { Paginator } from './components/paginator/paginator';
import { Toolbar } from './components/toolbar/toolbar';
import { Sidenav } from './components/sidenav/sidenav';
import { Divider } from './components/divider/divider';
import { ExpansionPanel, ExpansionPanelGroup } from './components/expansion-panel/expansion-panel';
import { List, ListItem } from './components/list/list';
import { Pagination } from './components/pagination/pagination';
import { Datepicker } from './components/datepicker/datepicker';

@NgModule({
  imports: [
    CommonModule,
    ButtonComponent,
    CardComponent,
    CheckboxComponent,
    InputComponent,
    SelectComponent,
    DialogContainerComponent,
    ToastContainerComponent,
    Radio,
    RadioGroup,
    Tabs,
    ProgressBar,
    ProgressSpinner,
    TableComponent,
    MenuComponent,
    TooltipDirective,
    Toggle,
    Stepper,
    Autocomplete,
    IconComponent,
    Badge,
    Chip,
    Paginator,
    Toolbar,
    Sidenav,
    Divider,
    ExpansionPanel,
    ExpansionPanelGroup,
    List,
    ListItem,
    Pagination,
    Datepicker
  ],
  exports: [
    ButtonComponent,
    CardComponent,
    CheckboxComponent,
    InputComponent,
    SelectComponent,
    Radio,
    RadioGroup,
    DialogContainerComponent,
    ToastContainerComponent,
    Tabs,
    ProgressBar,
    ProgressSpinner,
    TableComponent,
    MenuComponent,
    TooltipDirective,
    Toggle,
    Stepper,
    Autocomplete,
    IconComponent,
    Badge,
    Chip,
    Paginator,
    Toolbar,
    Sidenav,
    Divider,
    ExpansionPanel,
    ExpansionPanelGroup,
    List,
    ListItem,
    Pagination,
    Datepicker
  ],
  providers: [
    DialogService,
    ToastService
  ]
})
export class UiModule {}

// Export dialog service and utilities for direct imports
export { DialogService } from './services/dialog';
export type { DialogConfig } from './services/dialog';
export { DialogRef } from './services/dialog-ref';

// Export toast service and utilities for direct imports
export { ToastService } from './services/toast';
export type { ToastConfig, ToastType, ToastPosition } from './services/toast';
export { ToastRef } from './services/toast-ref';

// Export tabs component and types
export { Tabs } from './components/tabs/tabs';
export type { Tab } from './components/tabs/tabs';

// Export progress components and types
export { ProgressBar } from './components/progress-bar/progress-bar';
export type { ProgressBarColor, ProgressBarHeight } from './components/progress-bar/progress-bar';
export { ProgressSpinner } from './components/progress-spinner/progress-spinner';
export type { ProgressSpinnerColor, ProgressSpinnerSize } from './components/progress-spinner/progress-spinner';

// Export table component, directive, and types
export { TableComponent, TableColumnDirective } from './components/table/table';
export type { TableColumn, SortEvent } from './components/table/table';

// Export radio components and types
export { Radio } from './components/radio/radio';
export { RadioGroup } from './components/radio-group/radio-group';
export type { RadioOption } from './components/radio-group/radio-group';


// Export tooltip directive and types
export { TooltipDirective } from './directives/tooltip.directive';
export type { TooltipPosition } from './directives/tooltip.directive';

// Export menu component and types
export { MenuComponent } from './components/menu/menu';
export type { MenuItem, MenuPosition } from './components/menu/menu';

// Export toggle component
export { Toggle } from './components/toggle/toggle';

// Export stepper component
export { Stepper } from './components/stepper/stepper';

// Export autocomplete component
export { Autocomplete } from './components/autocomplete/autocomplete';

// Export icon component
export { IconComponent } from './components/icon/icon';

// Export badge component and types
export { Badge } from './components/badge/badge';
export type { BadgeColor, BadgePosition } from './components/badge/badge';

// Export chip component and types
export { Chip } from './components/chip/chip';
export type { ChipColor, ChipVariant } from './components/chip/chip';

// Export paginator component and types
export { Paginator } from './components/paginator/paginator';
export type { PageEvent } from './components/paginator/paginator';

// Export toolbar component
export { Toolbar } from './components/toolbar/toolbar';

// Export sidenav component
export { Sidenav } from './components/sidenav/sidenav';

// Export divider component
export { Divider } from './components/divider/divider';

// Export expansion panel components
export { ExpansionPanel, ExpansionPanelGroup } from './components/expansion-panel/expansion-panel';

// Export list components
export { List, ListItem } from './components/list/list';

// Export pagination component
export { Pagination } from './components/pagination/pagination';

// Export new datepicker component (custom implementation)
export { Datepicker } from './components/datepicker/datepicker';
