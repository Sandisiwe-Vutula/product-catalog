import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

export interface ToastMessage {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {

  // Single source of truth — all toasts flow through this stream
  private toastSubject = new Subject<ToastMessage>();

  /** Observable stream components subscribe to for rendering toasts */
  readonly toast$: Observable<ToastMessage> = this.toastSubject.asObservable();

  // Cross-route pending message (set before navigation, consumed after)
  private pending: string | null = null;

  // ── Public API ─────────────────────────────────────────────────────────────

  /** Emit a toast immediately to all active subscribers */
  show(message: string): void {
    this.toastSubject.next({ message });
  }

  /** Queue a toast to be shown on the next page after navigation */
  setPending(message: string): void {
    this.pending = message;
  }

  /**
   * Consume and emit any pending cross-route toast.
   * Call once in ngOnInit of the destination component.
   */
  consumePending(): void {
    if (this.pending) {
      this.toastSubject.next({ message: this.pending });
      this.pending = null;
    }
  }
}
