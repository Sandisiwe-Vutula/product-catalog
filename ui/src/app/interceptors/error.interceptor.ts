import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';


export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'An unexpected error occurred. Please try again.';

      if (error.status === 0) {
        message = 'Cannot connect to the server. Please check your connection.';
      } else if (error.status === 400 && error.error?.error) {
        message = error.error.error;
      } else if (error.status === 404) {
        message = error.error?.error ?? 'The requested resource was not found.';
      } else if (error.status === 500) {
        message = 'A server error occurred. Please try again later.';
      }

      const enrichedError = { ...error, friendlyMessage: message };
      return throwError(() => enrichedError);
    })
  );
};
