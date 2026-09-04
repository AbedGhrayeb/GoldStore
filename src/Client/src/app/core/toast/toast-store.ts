import { Injectable, signal } from '@angular/core';

export type ToastType = 'error' | 'success' | 'info';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

const DURATIONS: Record<ToastType, number> = {
  error: 8000,
  success: 4000,
  info: 5000,
};

@Injectable({ providedIn: 'root' })
export class ToastStore {
  private readonly toastsSignal = signal<Toast[]>([]);
  private nextId = 1;

  readonly toasts = this.toastsSignal.asReadonly();

  error(message: string): void {
    this.show('error', message);
  }

  success(message: string): void {
    this.show('success', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  dismiss(id: number): void {
    this.toastsSignal.update((toasts) => toasts.filter((toast) => toast.id !== id));
  }

  private show(type: ToastType, message: string): void {
    const toast: Toast = { id: this.nextId++, type, message };
    this.toastsSignal.update((toasts) => [...toasts, toast]);
    setTimeout(() => this.dismiss(toast.id), DURATIONS[type]);
  }
}
