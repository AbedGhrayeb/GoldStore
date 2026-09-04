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

function toUtcIso(localValue: string, endOfDay = false): string | null {
  if (!localValue) return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(localValue)) {
    const [y, m, d] = localValue.split('-').map(Number);
    const date = new Date(y, (m ?? 1) - 1, d ?? 1, endOfDay ? 23 : 0, endOfDay ? 59 : 0, endOfDay ? 59 : 0, endOfDay ? 999 : 0);
    if (Number.isNaN(date.getTime())) return null;
    return date.toISOString();
  }
  const date = new Date(localValue);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}

function toLocalInput(iso: string | null | undefined): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

@Component({
  selector: 'app-renew-subscription-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, Badge, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="تجديد الاشتراك"
      [subtitle]="tenant()?.name ?? ''"
      icon="refresh-cw"
      maxWidth="max-w-2xl"
      (openChange)="onDismiss()"
    >
      <div class="space-y-5">
        @if (subscription(); as sub) {
          <div class="overflow-hidden rounded-xl border border-amber-200/50 bg-gradient-to-br from-amber-50/60 to-white">
            <div class="flex items-center gap-2 border-b border-amber-100 px-4 py-2.5">
              <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-gold text-white"><lucide-icon [img]="awardIcon" [size]="14" /></span>
              <p class="text-xs font-bold tracking-widest text-amber-800">الاشتراك الحالي</p>
              <app-badge [variant]="sub.status.toLowerCase() === 'active' ? 'success' : 'warning'" class="ms-auto">{{ sub.status }}</app-badge>
            </div>
            <div class="space-y-2 px-4 py-3 text-xs">
              <p class="flex items-center gap-2 data-mono"><lucide-icon [img]="layersIcon" [size]="12" class="text-gray-400" />الخطة: <span class="font-semibold">{{ sub.plan.name }}</span> <span class="text-gray-500">({{ sub.plan.key }})</span></p>
              <p class="flex items-center gap-2 data-mono" dir="ltr"><lucide-icon [img]="calendarIcon" [size]="12" class="text-gray-400" />{{ sub.startsAtUtc }} → {{ sub.endsAtUtc }}</p>
              <div class="h-1.5 overflow-hidden rounded-full bg-gray-100">
                <div class="h-full rounded-full bg-gradient-to-r from-gold to-amber-500" [style.width.%]="progress(sub)"></div>
              </div>
            </div>
          </div>
        } @else {
          <div class="rounded-lg border border-dashed border-gray-300 bg-gray-50 px-4 py-3 text-xs text-gray-500">سيتم تحميل بيانات الاشتراك الحالي تلقائياً.</div>
        }

        <label class="block">
          <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="layersIcon" [size]="14" class="text-gray-400" />خطة جديدة (اختياري — اتركه فارغاً للتجديد على نفس الخطة)</span>
          <select
            [value]="newPlanId()"
            (change)="newPlanId.set($any($event.target).value)"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
          >
            <option value="">— نفس الخطة —</option>
            @for (plan of plans(); track plan.id) {
              <option [value]="plan.id">{{ plan.name }} — {{ plan.key }}</option>
            }
          </select>
          @if (newPlanId()) {
            <span class="mt-1.5 block text-xs text-emerald-700">سيتم التبديل إلى الخطة المحددة عند التجديد.</span>
          }
        </label>

        <div>
          <span class="mb-2 block text-sm font-medium text-gray-700">دورة الفوترة</span>
          <div class="inline-flex rounded-full bg-gray-100 p-1">
            <button type="button" class="rounded-full px-5 py-1.5 text-sm font-medium transition" [class]="billingCycle() === 0 ? 'bg-gray-900 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900'" (click)="billingCycle.set(0)">شهري</button>
            <button type="button" class="rounded-full px-5 py-1.5 text-sm font-medium transition" [class]="billingCycle() === 1 ? 'bg-gray-900 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900'" (click)="billingCycle.set(1)">سنوي</button>
          </div>
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <label class="block">
            <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="calendarIcon" [size]="12" />بداية الاشتراك — تاريخ فقط <span class="text-error">*</span></span>
            <input type="date" dir="ltr" autocomplete="off" [value]="startsAtUtc()" (change)="startsAtUtc.set($any($event.target).value)" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
            <span class="mt-1 block text-xs text-gray-500">الوقت افتراضي 00:00</span>
          </label>
          <label class="block">
            <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="calendarIcon" [size]="12" />نهاية الاشتراك — تاريخ فقط <span class="text-error">*</span></span>
            <input type="date" dir="ltr" autocomplete="off" [value]="endsAtUtc()" (change)="endsAtUtc.set($any($event.target).value)" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
            <span class="mt-1 block text-xs text-gray-500">الوقت افتراضي 23:59</span>
          </label>
        </div>
        @if (periodError(); as err) {
          <p class="rounded-lg bg-error/10 px-3 py-2 text-xs font-medium text-error">{{ err }}</p>
        }

        @if (saveError(); as error) {
          <p class="rounded-input border border-red-200 bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button variant="secondary" type="button" [disabled]="saving()" (clicked)="onDismiss()">إلغاء</app-button>
          <app-button type="button" [loading]="saving()" [disabled]="invalid()" (clicked)="onSubmit()">تأكيد التجديد</app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class RenewSubscriptionDialog {
  private readonly store = inject(HostStore);

  readonly open = input(false);
  readonly tenant = input<TenantSummaryResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly plans = this.store.plans;
  readonly subscription = this.store.subscription;

  readonly awardIcon = resolveIcon('award');
  readonly layersIcon = resolveIcon('layers');
  readonly calendarIcon = resolveIcon('calendar');

  readonly newPlanId = signal('');
  readonly billingCycle = signal<number>(0);
  readonly startsAtUtc = signal('');
  readonly endsAtUtc = signal('');

  readonly periodError = computed(() => {
    const s = this.startsAtUtc();
    const e = this.endsAtUtc();
    if (!s || !e) return null;
    const sIso = toUtcIso(s, false);
    const eIso = toUtcIso(e, true);
    if (sIso === null || eIso === null) return null;
    if (new Date(eIso).getTime() <= new Date(sIso).getTime()) return 'تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية';
    return null;
  });

  readonly invalid = computed(() => {
    if (!this.startsAtUtc() || !this.endsAtUtc()) return true;
    if (this.periodError() !== null) return true;
    return false;
  });

  constructor() {
    effect(() => {
      if (this.open() && this.tenant()) {
        this.store.clearSaveError();
        if (this.store.plans() === null && !this.store.plansLoading()) {
          void this.store.loadPlans();
        }
        const sub = this.store.subscription();
        if (sub !== null) {
          this.billingCycle.set(sub.billingCycle.toLowerCase() === 'annual' ? 1 : 0);
          this.startsAtUtc.set(toLocalInput(sub.startsAtUtc));
          this.endsAtUtc.set(toLocalInput(sub.endsAtUtc));
          this.newPlanId.set('');
        } else {
          this.startsAtUtc.set('');
          this.endsAtUtc.set('');
        }
      }
    });
  }

  progress(sub: { startsAtUtc: string; endsAtUtc: string }): number {
    const start = new Date(sub.startsAtUtc).getTime();
    const end = new Date(sub.endsAtUtc).getTime();
    const now = Date.now();
    if (Number.isNaN(start) || Number.isNaN(end) || end <= start) return 50;
    return Math.max(4, Math.min(100, Math.round(((now - start) / (end - start)) * 100)));
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  async onSubmit(): Promise<void> {
    const tenant = this.tenant();
    if (tenant === null || this.invalid()) return;
    const startsIso = toUtcIso(this.startsAtUtc(), false);
    const endsIso = toUtcIso(this.endsAtUtc(), true);
    if (startsIso === null || endsIso === null) return;

    const ok = await this.store.renewSubscription(tenant.id, {
      newPlanId: this.newPlanId().trim() || null,
      billingCycle: Number(this.billingCycle()),
      startsAtUtc: startsIso,
      endsAtUtc: endsIso,
    });

    if (ok) {
      this.saved.emit();
      this.onDismiss();
    }
  }
}
