import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'app-theme';

  // Signal for reactive theme state
  private themeSignal = signal<Theme>(this.getInitialTheme());

  readonly theme = this.themeSignal.asReadonly();
  readonly isDark = () => this.themeSignal() === 'dark';

  constructor() {
    // Effect to apply theme changes to DOM
    effect(() => {
      const theme = this.themeSignal();
      this.applyTheme(theme);
    });
  }

  private getInitialTheme(): Theme {
    const savedTheme = localStorage.getItem(this.THEME_KEY) as Theme;
    if (savedTheme) {
      return savedTheme;
    }

    // Check system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }

  private applyTheme(theme: Theme): void {
    const htmlElement = document.documentElement;

    if (theme === 'dark') {
      htmlElement.classList.add('dark-theme');
      htmlElement.classList.remove('light-theme');
    } else {
      htmlElement.classList.add('light-theme');
      htmlElement.classList.remove('dark-theme');
    }

    localStorage.setItem(this.THEME_KEY, theme);
  }

  toggleTheme(): void {
    const newTheme: Theme = this.themeSignal() === 'light' ? 'dark' : 'light';
    this.themeSignal.set(newTheme);
  }

  setTheme(theme: Theme): void {
    this.themeSignal.set(theme);
  }
}
