import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { errorInterceptor } from './app/interceptors/error.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([errorInterceptor]))
  ]
}).catch(err => {
  console.error('Bootstrap failed:', err);
  document.body.innerHTML = `
    <div style="padding:40px;font-family:monospace;color:#c62828;background:#fff3f3;
                border:1px solid #ffcdd2;margin:40px;border-radius:8px;">
      <h2>Bootstrap Error</h2>
      <pre>${err?.message ?? err}</pre>
    </div>
  `;
});
