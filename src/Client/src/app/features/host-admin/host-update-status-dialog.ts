import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';

import { LucideAngularModule } from 'lucide-angular';

import type { ApiError } from '../../core/http/api-error';
import { Badge, Button, Dialog } from '../../shared/ui';
import { resolveIcon } from '../../shared/ui/icon-registry';
import type { TenantSummaryResponse } from './host-api.service';
import { HostStore } from './host-store';

function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) return message;
  }
  return null;
}

const STATUS_OPTIONS: ReadonlyArray<{ value: number; label: string; hint: string }> = [
  { value: 0, label: 'قيد الانتظار', hint: 'Pending — قبل التفعيل' },
  { value: 1, label: 'تجريبي', hint: 'Trial — فترة اختبار' },
  { value: 2, label: 'نشط', hint: 'Active — تشغيل كامل' },
  { value: 3, label: 'ملغى', hint: 'Cancelled — إيقاف مع سماح قراءة' },
];

function toUtcIso(localValue: string): string | null {
  if (!localValue) return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(localValue)) {
    const [y, m, d] = localValue.split('-').map(Number);
    const date = new Date(y, (m ?? 1) - 1, d ?? 1, 0, 0, 0, 0);
    if (Number.isNaN(date.getTime())) return null;
    return date.toISOString();
  }
  const date = new Date(localValue);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}

/**
 * P3.13 — update a tenant's status. Posts `UpdateTenantStatusRequest`
 * (`newStatus`, `transitionAtUtc`) to `PATCH /host/api/v1/tenants/{id}/status`.
 * Signature: status pipeline — gold rail with 4 sovereign states.
 */
@Component({
  selector: 'app-host-update-status-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Badge, Button, Dialog, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="تغيير حالة المستأجر"
      [subtitle]="tenant()?.name ?? ''"
      icon="server"
      (openChange)="onDismiss()"
    >
      <div class="space-y-5">
        @if (tenant(); as t) {
          <div class="flex items-center gap-3 rounded-xl border border-amber-200/50 bg-amber-50/60 px-4 py-3">
            <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-gray-900 text-white text-xs font-bold">{{ t.name.charAt(0) }}</span>
            <div class="min-w-0">
              <p class="truncate text-sm font-semibold text-gray-900">{{ t.name }}</p>
              <p class="text-xs text-gray-500 data-mono">{{ t.key }} — {{ statusLabel(t.status) }}</p>
            </div>
            <app-badge [variant]="statusVariant(t.status)" class="ms-auto">{{ statusLabel(t.status) }}</app-badge>
          </div>
        }

        <!-- Pipeline -->
        <div class="rounded-xl border border-gray-200 bg-gray-50/50 p-4">
          <p class="mb-3 text-xs font-bold tracking-widest text-gray-500">مسار الحالة</p>
          <div class="flex items-center gap-1">
            @for (opt of statusOptions; track opt.value) {
              <div class="flex flex-1 flex-col items-center gap-1.5">
                <button
                  type="button"
                  (click)="newStatus.set(opt.value)"
                  class="flex h-9 w-9 items-center justify-center rounded-full border-2 text-xs font-bold transition"
                  [class]="pipelineDotClass(opt.value)"
                  [attr.aria-label]="opt.label"
                >
                  @if (newStatus() === opt.value) {
                    <lucide-icon [img]="checkIcon" [size]="14" />
                  } @else {
                    {{ opt.value + 1 }}
                  }
                </button>
                <span class="text-center text-[11px] font-medium leading-tight" [class]="newStatus() === opt.value ? 'text-gray-900' : 'text-gray-500'">{{ opt.label }}</span>
              </div>
              @if (opt.value < 3) {
                <span class="mb-6 h-px flex-1" [class]="newStatus() > opt.value ? 'bg-gold' : 'bg-gray-200'"></span>
              }
            }
          </div>
          <p class="mt-3 text-center text-xs text-gray-500">{{ selectedHint() }}</p>
        </div>

        <div>
          <label class="mb-2 flex items-center gap-1.5 text-sm font-medium text-gray-700" for="host-status">
            <lucide-icon [img]="shieldIcon" [size]="14" class="text-gray-400" />
            الحالة الجديدة <span class="text-error">*</span>
          </label>
          <select
            id="host-status"
            [value]="newStatus()"
            (change)="newStatus.set(+$any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            @for (option of statusOptions; track option.value) {
              <option [value]="option.value">{{ option.label }} — {{ option.hint }}</option>
            }
          </select>
        </div>

        <div>
          <label class="mb-2 flex items-center gap-1.5 text-sm font-medium text-gray-700" for="host-transition">
            <lucide-icon [img]="calendarIcon" [size]="14" class="text-gray-400" />
            تاريخ الانتقال (اختياري)
          </label>
          <input
            id="host-transition"
            type="date"
            dir="ltr"
            autocomplete="off"
            [value]="transitionAtUtc()"
            (change)="transitionAtUtc.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          <p class="mt-1.5 rounded-lg bg-gray-50 px-3 py-2 text-xs leading-5 text-gray-600">
            للتغيير إلى تجريبي: نهاية التجربة. للإلغاء: نهاية فترة السماح للقراءة فقط.
            @if (newStatus() === 3) {
              <span class="font-bold text-error"> — الإلغاء إجراء حساس، سيُقيّد المتجر للقراءة فقط.</span>
            }
          </p>
        </div>

        @if (saveError(); as error) {
          <p class="rounded-input border border-red-200 bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button variant="secondary" type="button" [disabled]="saving()" (clicked)="onDismiss()">
            إلغاء
          </app-button>
          <app-button type="button" [loading]="saving()" [disabled]="invalid()" (clicked)="onSubmit()">
            تأكيد التغيير
          </app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class HostUpdateStatusDialog {
  private readonly store = inject(HostStore);

  readonly open = input(false);
  readonly tenant = input<TenantSummaryResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly newStatus = signal<number>(2);
  readonly transitionAtUtc = signal('');

  readonly statusOptions = STATUS_OPTIONS;
  readonly checkIcon = resolveIcon('check-circle');
  readonly shieldIcon = resolveIcon('shield');
  readonly calendarIcon = resolveIcon('calendar');

  readonly invalid = computed(() => this.newStatus() === null || Number.isNaN(this.newStatus()));

  readonly selectedHint = computed(() => {
    const v = this.newStatus();
    return this.statusOptions.find((o) => o.value === v)?.hint ?? '';
  });

  constructor() {
    effect(() => {
      if (this.open() && this.tenant()) {
        const tenant = this.tenant()!;
        const statusNum = this.statusToNumber(tenant.status);
        this.newStatus.set(statusNum);
        this.transitionAtUtc.set('');
        this.store.clearSaveError();
      }
    });
  }

  pipelineDotClass(value: number): string {
    const active = this.newStatus() === value;
    const passed = this.newStatus() > value;
    if (active) return 'border-gold bg-gold text-white shadow-sm ring-2 ring-gold/30';
    if (passed) return 'border-emerald-300 bg-emerald-500 text-white';
    return 'border-gray-300 bg-white text-gray-500';
  }

  statusVariant(status: string): 'success' | 'warning' | 'error' | 'neutral' {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'success';
    if (lower === 'trial') return 'warning';
    if (lower === 'cancelled') return 'error';
    return 'neutral';
  }

  statusLabel(status: string): string {
    const lower = status.toLowerCase();
    if (lower === 'active') return 'نشط';
    if (lower === 'trial') return 'تجريبي';
    if (lower === 'cancelled') return 'ملغى';
    if (lower === 'pending') return 'قيد الانتظار';
    return status;
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  async onSubmit(): Promise<void> {
    const tenant = this.tenant();
    if (!tenant || this.invalid()) return;
    const transitionIso = toUtcIso(this.transitionAtUtc());
    const ok = await this.store.updateTenantStatus(tenant.id as string, {
      newStatus: Number(this.newStatus()),
      transitionAtUtc: transitionIso,
    });
    if (ok) {
      this.saved.emit();
      this.onDismiss();
    }
  }

  private statusToNumber(status: string): number {
    const lower = status.toLowerCase();
    if (lower === 'trial') return 1;
    if (lower === 'active') return 2;
    if (lower === 'cancelled') return 3;
    return 0;
  }
}
