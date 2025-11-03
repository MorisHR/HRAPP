import { Injectable, signal, effect } from '@angular/core';

export type ColorMode = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly COLOR_MODE_KEY = 'hrms-color-mode';

  // Signal for reactive color mode state
  private colorModeSignal = signal<ColorMode>(this.getInitialColorMode());

  // Readonly computed signal for dark mode check
  readonly isDark = () => this.colorModeSignal() === 'dark';

  constructor() {
    // Effect to apply theme changes
    effect(() => {
      const mode = this.colorModeSignal();
      console.log('🎨 Color mode changing to:', mode);
      this.applyColorMode(mode);
    });
  }

  private getInitialColorMode(): ColorMode {
    const saved = localStorage.getItem(this.COLOR_MODE_KEY) as ColorMode;
    if (saved && ['light', 'dark'].includes(saved)) {
      return saved;
    }
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }

  private applyColorMode(mode: ColorMode): void {
    const htmlElement = document.documentElement;

    if (mode === 'dark') {
      htmlElement.classList.add('dark-theme');
      htmlElement.classList.remove('light-theme');
    } else {
      htmlElement.classList.add('light-theme');
      htmlElement.classList.remove('dark-theme');
    }

    localStorage.setItem(this.COLOR_MODE_KEY, mode);
  }

  public toggleTheme(): void {
    const newMode = this.colorModeSignal() === 'light' ? 'dark' : 'light';
    this.colorModeSignal.set(newMode);
  }

  public setColorMode(mode: ColorMode): void {
    this.colorModeSignal.set(mode);
  }

  public isDarkMode(): boolean {
    return this.colorModeSignal() === 'dark';
  }
}
