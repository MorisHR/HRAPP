import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Tab {
  label: string;
  value: string;
  disabled?: boolean;
  icon?: string;
}

@Component({
  selector: 'app-tabs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tabs.html',
  styleUrl: './tabs.scss',
})
export class Tabs {
  @Input() tabs: Tab[] = [];
  @Input() activeTab: string = '';
  @Input() variant: 'default' | 'pills' | 'underline' = 'default';
  @Output() tabChange = new EventEmitter<string>();

  private focusedTabIndex: number = 0;

  ngOnInit(): void {
    // Set initial focused tab to active tab
    const activeIndex = this.tabs.findIndex(tab => tab.value === this.activeTab);
    this.focusedTabIndex = activeIndex >= 0 ? activeIndex : 0;
  }

  selectTab(tab: Tab, index: number): void {
    if (tab.disabled) {
      return;
    }

    this.activeTab = tab.value;
    this.focusedTabIndex = index;
    this.tabChange.emit(tab.value);
  }

  isActive(tab: Tab): boolean {
    return this.activeTab === tab.value;
  }

  @HostListener('keydown', ['$event'])
  handleKeyboardNavigation(event: KeyboardEvent): void {
    const enabledTabs = this.tabs.filter(tab => !tab.disabled);
    const currentEnabledIndex = enabledTabs.findIndex(
      tab => tab.value === this.tabs[this.focusedTabIndex]?.value
    );

    switch (event.key) {
      case 'ArrowRight':
        event.preventDefault();
        this.navigateToNextTab(enabledTabs, currentEnabledIndex);
        break;

      case 'ArrowLeft':
        event.preventDefault();
        this.navigateToPreviousTab(enabledTabs, currentEnabledIndex);
        break;

      case 'Home':
        event.preventDefault();
        this.navigateToFirstTab(enabledTabs);
        break;

      case 'End':
        event.preventDefault();
        this.navigateToLastTab(enabledTabs);
        break;

      case 'Enter':
      case ' ':
        event.preventDefault();
        const currentTab = this.tabs[this.focusedTabIndex];
        if (currentTab && !currentTab.disabled) {
          this.selectTab(currentTab, this.focusedTabIndex);
        }
        break;
    }
  }

  private navigateToNextTab(enabledTabs: Tab[], currentIndex: number): void {
    if (enabledTabs.length === 0) return;

    const nextIndex = (currentIndex + 1) % enabledTabs.length;
    const nextTab = enabledTabs[nextIndex];
    const actualIndex = this.tabs.findIndex(tab => tab.value === nextTab.value);

    this.focusedTabIndex = actualIndex;
    this.focusTabButton(actualIndex);
  }

  private navigateToPreviousTab(enabledTabs: Tab[], currentIndex: number): void {
    if (enabledTabs.length === 0) return;

    const prevIndex = currentIndex <= 0 ? enabledTabs.length - 1 : currentIndex - 1;
    const prevTab = enabledTabs[prevIndex];
    const actualIndex = this.tabs.findIndex(tab => tab.value === prevTab.value);

    this.focusedTabIndex = actualIndex;
    this.focusTabButton(actualIndex);
  }

  private navigateToFirstTab(enabledTabs: Tab[]): void {
    if (enabledTabs.length === 0) return;

    const firstTab = enabledTabs[0];
    const actualIndex = this.tabs.findIndex(tab => tab.value === firstTab.value);

    this.focusedTabIndex = actualIndex;
    this.focusTabButton(actualIndex);
  }

  private navigateToLastTab(enabledTabs: Tab[]): void {
    if (enabledTabs.length === 0) return;

    const lastTab = enabledTabs[enabledTabs.length - 1];
    const actualIndex = this.tabs.findIndex(tab => tab.value === lastTab.value);

    this.focusedTabIndex = actualIndex;
    this.focusTabButton(actualIndex);
  }

  private focusTabButton(index: number): void {
    setTimeout(() => {
      const tabButton = document.querySelector(
        `.tabs__button[data-tab-index="${index}"]`
      ) as HTMLButtonElement;
      if (tabButton) {
        tabButton.focus();
      }
    }, 0);
  }

  trackByValue(index: number, tab: Tab): string {
    return tab.value;
  }
}
