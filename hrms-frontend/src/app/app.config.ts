import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, isDevMode, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors, withInterceptorsFromDi, HttpClient, HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { CsrfInterceptor } from './core/interceptors/csrf.interceptor';
import { CsrfService } from './core/services/csrf.service';
import { provideServiceWorker } from '@angular/service-worker';
import { ChunkLoadingErrorHandler } from './core/error-handlers/chunk-loading-error.handler';
import { provideEchartsCore } from 'ngx-echarts';
import * as echarts from 'echarts/core';
import { LineChart, BarChart, PieChart } from 'echarts/charts';
import { GridComponent, TooltipComponent, LegendComponent, TitleComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';

// i18n imports for multi-language support (English/French for Mauritius)
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { createTranslateLoader } from './core/i18n/translation.loader';

/**
 * CSRF Token Initializer Factory
 * Fetches CSRF token from backend before app starts
 * Fortune 500 Security: CSRF protection initialization
 */
export function initializeCsrfToken(csrfService: CsrfService) {
  return () => csrfService.initializeCsrfToken();
}

// Register ECharts components
echarts.use([LineChart, BarChart, PieChart, GridComponent, TooltipComponent, LegendComponent, TitleComponent, CanvasRenderer]);

// Chart.js configuration
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: ErrorHandler, useClass: ChunkLoadingErrorHandler },
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(), // Zoneless change detection (stable in Angular 20)
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor]),
      withInterceptorsFromDi() // Enable class-based interceptors for CSRF
    ),
    // FORTUNE 500 SECURITY: CSRF Protection Interceptor
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CsrfInterceptor,
      multi: true
    },
    // FORTUNE 500 SECURITY: Initialize CSRF token on app startup
    {
      provide: APP_INITIALIZER,
      useFactory: initializeCsrfToken,
      deps: [CsrfService],
      multi: true
    },
    provideAnimationsAsync(),
    provideEchartsCore({ echarts }), // ECharts configuration for Fortune 500 charts
    provideServiceWorker('ngsw-worker.js', {
            enabled: !isDevMode(),
            registrationStrategy: 'registerWhenStable:30000'
          }),
    // i18n Configuration - Multi-language support for Mauritius (English/French)
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        loader: {
          provide: TranslateLoader,
          useFactory: createTranslateLoader,
          deps: [HttpClient]
        }
      })
    )
  ]
};
