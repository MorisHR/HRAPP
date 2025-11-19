import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

/**
 * Custom Translation Loader for ngx-translate
 * Loads translation files from public/assets/i18n/
 *
 * Supports: English (en), French (fr)
 * For Mauritius deployment with bilingual support
 */
export class TranslationHttpLoader implements TranslateLoader {
  constructor(private http: HttpClient) {}

  getTranslation(lang: string): Observable<any> {
    // Load from public/assets/i18n/{lang}.json
    return this.http.get(`/assets/i18n/${lang}.json`).pipe(
      map((translations: any) => translations)
    );
  }
}

/**
 * Factory function for creating TranslationHttpLoader
 * Used in app.config.ts for dependency injection
 */
export function createTranslateLoader(http: HttpClient): TranslateLoader {
  return new TranslationHttpLoader(http);
}
