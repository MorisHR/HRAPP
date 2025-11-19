import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

/**
 * LANGUAGE SWITCHER COMPONENT
 *
 * Bilingual support for Mauritius deployment (English/French)
 *
 * Features:
 * - Toggle between English and French
 * - Persists language preference in localStorage
 * - Responsive design
 * - Accessible with ARIA labels
 * - Smooth transitions
 *
 * Usage:
 * <app-language-switcher />
 */

export interface Language {
  code: string;
  name: string;
  flag: string;
}

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="language-switcher">
      <button
        class="language-button"
        (click)="toggleLanguage()"
        [attr.aria-label]="'Switch to ' + (currentLanguage().code === 'en' ? 'French' : 'English')"
        type="button"
      >
        <span class="flag">{{ currentLanguage().flag }}</span>
        <span class="language-code">{{ currentLanguage().code.toUpperCase() }}</span>
        <span class="language-name">{{ currentLanguage().name }}</span>
      </button>

      <!-- Dropdown variant (optional) -->
      @if (showDropdown()) {
        <div class="language-dropdown">
          @for (lang of languages; track lang.code) {
            <button
              class="language-option"
              [class.active]="lang.code === currentLanguage().code"
              (click)="selectLanguage(lang.code)"
              [attr.aria-label]="'Switch to ' + lang.name"
              type="button"
            >
              <span class="flag">{{ lang.flag }}</span>
              <span class="language-name">{{ lang.name }}</span>
              @if (lang.code === currentLanguage().code) {
                <span class="checkmark">✓</span>
              }
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .language-switcher {
      position: relative;
      display: inline-block;
    }

    .language-button {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: var(--color-surface, #ffffff);
      border: 1px solid var(--color-neutral-300, #e0e0e0);
      border-radius: var(--border-radius-md, 8px);
      cursor: pointer;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary, #212121);
      transition: all 0.2s ease;
    }

    .language-button:hover {
      background: var(--color-neutral-50, #f5f5f5);
      border-color: var(--color-primary, #1976d2);
    }

    .language-button:focus {
      outline: 2px solid var(--color-primary, #1976d2);
      outline-offset: 2px;
    }

    .flag {
      font-size: 1.25rem;
      line-height: 1;
    }

    .language-code {
      font-weight: 600;
      text-transform: uppercase;
    }

    .language-name {
      display: none;
    }

    @media (min-width: 768px) {
      .language-name {
        display: inline;
      }
    }

    .language-dropdown {
      position: absolute;
      top: calc(100% + 0.5rem);
      right: 0;
      min-width: 200px;
      background: var(--color-surface, #ffffff);
      border: 1px solid var(--color-neutral-300, #e0e0e0);
      border-radius: var(--border-radius-md, 8px);
      box-shadow: var(--elevation-3, 0 4px 6px rgba(0, 0, 0, 0.1));
      z-index: 1000;
      overflow: hidden;
    }

    .language-option {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      width: 100%;
      padding: 0.75rem 1rem;
      background: transparent;
      border: none;
      cursor: pointer;
      font-size: 0.875rem;
      color: var(--color-text-primary, #212121);
      transition: background 0.2s ease;
      text-align: left;
    }

    .language-option:hover {
      background: var(--color-neutral-50, #f5f5f5);
    }

    .language-option.active {
      background: var(--color-primary-50, #e3f2fd);
      color: var(--color-primary, #1976d2);
      font-weight: 600;
    }

    .language-option:focus {
      outline: 2px solid var(--color-primary, #1976d2);
      outline-offset: -2px;
    }

    .checkmark {
      margin-left: auto;
      color: var(--color-primary, #1976d2);
      font-weight: bold;
    }
  `]
})
export class LanguageSwitcherComponent {
  private translate = inject(TranslateService);

  // Available languages for Mauritius
  readonly languages: Language[] = [
    { code: 'en', name: 'English', flag: '🇬🇧' },
    { code: 'fr', name: 'Français', flag: '🇫🇷' }
  ];

  // State
  showDropdown = signal(false);
  currentLanguage = computed(() => {
    const currentLang = this.translate.currentLang || this.translate.defaultLang || 'en';
    return this.languages.find(lang => lang.code === currentLang) || this.languages[0];
  });

  constructor() {
    // Initialize language from localStorage or browser preference
    this.initializeLanguage();
  }

  /**
   * Initialize language on component load
   */
  private initializeLanguage(): void {
    // Check localStorage first
    const savedLang = localStorage.getItem('preferred-language');

    if (savedLang && this.languages.some(l => l.code === savedLang)) {
      this.translate.use(savedLang);
    } else {
      // Check browser language
      const browserLang = this.translate.getBrowserLang() || 'en';
      const defaultLang = this.languages.some(l => l.code === browserLang) ? browserLang : 'en';
      this.translate.use(defaultLang);
      localStorage.setItem('preferred-language', defaultLang);
    }
  }

  /**
   * Toggle between English and French
   */
  toggleLanguage(): void {
    const newLang = this.currentLanguage().code === 'en' ? 'fr' : 'en';
    this.selectLanguage(newLang);
  }

  /**
   * Select a specific language
   */
  selectLanguage(langCode: string): void {
    if (this.languages.some(l => l.code === langCode)) {
      this.translate.use(langCode);
      localStorage.setItem('preferred-language', langCode);
      this.showDropdown.set(false);

      // Dispatch custom event for other components to react
      window.dispatchEvent(new CustomEvent('languageChanged', { detail: { language: langCode } }));
    }
  }

  /**
   * Toggle dropdown visibility
   */
  toggleDropdown(): void {
    this.showDropdown.update(value => !value);
  }
}
