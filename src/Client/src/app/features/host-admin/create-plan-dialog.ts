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
import { Button, Dialog } from '../../shared/ui';
import { resolveIcon } from '../../shared/ui/icon-registry';
import { HostStore } from './host-store';
import type { SubscriptionPlanResponse } from './host-api.service';

function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) return message;
  }
  return null;
}

function slugify(name: string): string {
  let slug = name.trim().toLowerCase();
  slug = slug.replace(/[^a-z0-9\s-]/g, '');
  slug = slug.replace(/\s+/g, '-');
  slug = slug.replace(/-+/g, '-');
  slug = slug.replace(/^-+|-+$/g, '');
  if (!slug) slug = `plan-${Math.random().toString(36).slice(2, 8)}`;
  if (slug.length > 50) slug = slug.slice(0, 50).replace(/-+$/g, '');
  return slug;
}

const DURATION_OPTIONS: ReadonlyArray<{ value: number; label: string }> = [
  { value: 1, label: 'شهر واحد (1)' },
  { value: 3, label: '3 أشهر' },
  { value: 6, label: '6 أشهر' },
  { value: 12, label: 'سنة واحدة (12)' },
  { value: 24, label: 'سنتان (24)' },
];

@Component({
  selector: 'app-create-plan-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      [title]="isEditMode() ? 'تعديل الخطة' : 'خطة اشتراك جديدة'"
      [subtitle]="isEditMode() ? (plan()?.name ?? '') : 'المفتاح يُولّد تلقائياً من الاسم — حدّد المدة والسعر والخصم'"
      [icon]="isEditMode() ? 'pencil' : 'tags'"
      maxWidth="max-w-2xl"
      (openChange)="onDismiss()"
    >
      <div class="space-y-5">
        <!-- Identity -->
        <div class="space-y-3">
          <label class="block">
            <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="sparklesIcon" [size]="12" class="text-gold" />الاسم <span class="text-error">*</span></span>
            <input autocomplete="off" name="plan-name" [value]="name()" (input)="name.set($any($event.target).value)" maxlength="100" placeholder="Standard — سنوي" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
          </label>
          <div class="flex items-center gap-2 rounded-lg border border-dashed border-amber-200 bg-amber-50/50 px-3 py-2">
            <lucide-icon [img]="layersIcon" [size]="12" class="text-amber-600" />
            <span class="text-xs text-gray-600">المفتاح التلقائي:</span>
            <span class="rounded-full bg-white px-2.5 py-1 text-xs font-medium text-gray-700 data-mono border">{{ generatedKey() || '—' }}</span>
            <span class="text-xs text-gray-400">يُحفظ مع الخطة ويظهر للفروع</span>
          </div>
        </div>

        <!-- Trial + Duration + Pricing -->
        <div class="rounded-xl border border-gray-200 bg-gray-50/50 p-4 space-y-4">
          <div class="flex items-center justify-between gap-3">
            <div class="flex items-center gap-3">
              <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-white text-gray-600 shadow-sm"><lucide-icon [img]="zapIcon" [size]="14" /></span>
              <div>
                <p class="text-sm font-semibold text-gray-900">نوع الخطة والمدة</p>
                <p class="text-xs text-gray-500">التجريبية شهر واحد مجاني ولا تُجدّد — يجب الترقية</p>
              </div>
            </div>
            <label class="flex items-center gap-2 rounded-full border bg-white px-3 py-1.5 text-xs font-medium shadow-sm cursor-pointer" [class]="isTrial() ? 'border-amber-300 bg-amber-50 text-amber-800' : 'border-gray-200 text-gray-700'">
              <input type="checkbox" class="rounded" [checked]="isTrial()" (change)="onTrialToggle($any($event.target).checked)" />
              خطة تجريبية (IsTrial)
            </label>
          </div>

          <div class="grid gap-4 sm:grid-cols-2">
            <label class="block">
              <span class="mb-1.5 block text-xs font-medium tracking-widest text-gray-600">المدة بالأشهر</span>
              <select
                [value]="durationInMonths()"
                (change)="durationInMonths.set(+$any($event.target).value)"
                [disabled]="isTrial()"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30 disabled:bg-gray-100 disabled:text-gray-500"
              >
                @for (opt of durationOptions; track opt.value) {
                  <option [value]="opt.value">{{ opt.label }}</option>
                }
              </select>
              @if (isTrial()) {
                <span class="mt-1 block text-xs text-amber-700">التجريبية ثابتة على شهر واحد</span>
              }
            </label>
            <div class="grid grid-cols-2 gap-3">
              <label class="block">
                <span class="mb-1.5 block text-xs font-medium tracking-widest text-gray-600">السعر</span>
                <input type="number" min="0" step="0.01" autocomplete="off" [value]="price()" (input)="price.set($any($event.target).value)" [disabled]="isTrial()" placeholder="0.00" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30 disabled:bg-gray-100" />
              </label>
              <label class="block">
                <span class="mb-1.5 block text-xs font-medium tracking-widest text-gray-600">خصم % (اختياري)</span>
                <input type="number" min="0" max="100" step="0.1" autocomplete="off" [value]="discountPercent()" (input)="discountPercent.set($any($event.target).value)" [disabled]="isTrial()" placeholder="—" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30 disabled:bg-gray-100" />
              </label>
            </div>
          </div>
          <div class="flex flex-wrap items-center gap-2 text-xs">
            <span class="rounded-full bg-white px-3 py-1.5 border shadow-sm data-mono">الفعّال: {{ effectivePriceLabel() }}</span>
            @if (isTrial()) {
              <span class="rounded-full bg-amber-100 px-3 py-1 text-amber-800 border border-amber-200">مجانية — لا تُجدّد</span>
            }
          </div>
          @if (priceError(); as err) {
            <span class="block rounded bg-error/10 px-2 py-1 text-xs font-medium text-error">{{ err }}</span>
          }
          @if (discountError(); as err) {
            <span class="block rounded bg-error/10 px-2 py-1 text-xs font-medium text-error">{{ err }}</span>
          }
        </div>

        <!-- Limits — vault quotas -->
        <div class="rounded-xl border border-gray-200 bg-gray-50/50 p-4">
          <div class="mb-3 flex items-center gap-2">
            <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-white text-gray-600 shadow-sm"><lucide-icon [img]="zapIcon" [size]="14" /></span>
            <p class="text-sm font-semibold text-gray-900">الحدود والحصص</p>
            <span class="text-xs text-gray-500">فارغ = غير محدود — يحكم قدرة المتجر</span>
          </div>
          <div class="grid gap-4 sm:grid-cols-2">
            <label class="block">
              <span class="mb-1.5 flex items-center gap-1.5 text-xs font-medium tracking-widest text-gray-600"><lucide-icon [img]="usersIcon" [size]="12" />الحد الأقصى للمستخدمين</span>
              <input type="number" min="0" autocomplete="off" [value]="maxUsers()" (input)="maxUsers.set($any($event.target).value)" placeholder="فارغ = غير محدود" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
            </label>
            <label class="block">
              <span class="mb-1.5 flex items-center gap-1.5 text-xs font-medium tracking-widest text-gray-600"><lucide-icon [img]="receiptIcon" [size]="12" />الحد للفواتير في الفترة</span>
              <input type="number" min="0" autocomplete="off" [value]="maxInvoices()" (input)="maxInvoices.set($any($event.target).value)" placeholder="فارغ = غير محدود" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
            </label>
          </div>

          <div class="mt-4 grid gap-4 sm:grid-cols-2">
            <label class="block">
              <span class="mb-1.5 flex items-center gap-1.5 text-xs font-medium tracking-widest text-gray-600"><lucide-icon [img]="buildingIcon" [size]="12" />الحد للفروع</span>
              <input type="number" min="0" autocomplete="off" [value]="maxBranches()" (input)="maxBranches.set($any($event.target).value)" placeholder="فارغ = غير محدود" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
            </label>
            <label class="block">
              <span class="mb-1.5 flex items-center gap-1.5 text-xs font-medium tracking-widest text-gray-600"><lucide-icon [img]="databaseIcon" [size]="12" />الحد للتخزين (بايت)</span>
              <input type="number" min="0" autocomplete="off" [value]="maxStorage()" (input)="maxStorage.set($any($event.target).value)" placeholder="فارغ = غير محدود" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
              @if (maxStorage()) {
                <span class="mt-1 block text-xs text-gray-500 data-mono">{{ prettyBytes(maxStorage()) }}</span>
              }
            </label>
          </div>

          <!-- quick preview chips -->
          <div class="mt-4 flex flex-wrap gap-2">
            <span class="rounded-full bg-white px-3 py-1 text-xs shadow-sm data-mono border">مستخدمون: {{ prettyLimit(maxUsers()) }}</span>
            <span class="rounded-full bg-white px-3 py-1 text-xs shadow-sm data-mono border">فواتير: {{ prettyLimit(maxInvoices()) }}</span>
            <span class="rounded-full bg-white px-3 py-1 text-xs shadow-sm data-mono border">فروع: {{ prettyLimit(maxBranches()) }}</span>
            <span class="rounded-full bg-white px-3 py-1 text-xs shadow-sm data-mono border">تخزين: {{ prettyBytes(maxStorage()) }}</span>
          </div>
        </div>

        @if (isEditMode()) {
          <div class="rounded-xl border border-gray-200 bg-white p-4">
            <p class="mb-3 text-sm font-semibold text-gray-900">حالة الخطة</p>
            <div class="flex gap-3">
              <label class="flex flex-1 cursor-pointer items-center gap-3 rounded-lg border-2 px-4 py-3 transition" [class]="isActive() ? 'border-emerald-500 bg-emerald-50' : 'border-gray-200 bg-white hover:border-gray-300'">
                <input type="radio" name="plan-active" [checked]="isActive()" (change)="isActive.set(true)" class="h-4 w-4 border-gray-300 text-emerald-600 focus:ring-emerald-500" />
                <span class="flex items-center gap-2 text-sm font-medium" [class]="isActive() ? 'text-emerald-800' : 'text-gray-600'">
                  <lucide-icon [img]="checkIcon" [size]="14" />
                  نشط
                </span>
                @if (isActive()) {
                  <span class="ms-auto h-2 w-2 rounded-full bg-emerald-500"></span>
                }
              </label>
              <label class="flex flex-1 cursor-pointer items-center gap-3 rounded-lg border-2 px-4 py-3 transition" [class]="!isActive() ? 'border-red-500 bg-red-50' : 'border-gray-200 bg-white hover:border-gray-300'">
                <input type="radio" name="plan-active" [checked]="!isActive()" (change)="isActive.set(false)" class="h-4 w-4 border-gray-300 text-red-600 focus:ring-red-500" />
                <span class="flex items-center gap-2 text-sm font-medium" [class]="!isActive() ? 'text-red-800' : 'text-gray-600'">
                  <lucide-icon [img]="xIcon" [size]="14" />
                  غير نشط
                </span>
                @if (!isActive()) {
                  <span class="ms-auto h-2 w-2 rounded-full bg-red-500"></span>
                }
              </label>
            </div>
            <p class="mt-2 text-xs text-gray-500">الخطة غير النشطة لا تظهر عند تأسيس متجر جديد ولا يمكن اختيارها</p>
          </div>
        }

        @if (saveError(); as error) {
          <p class="rounded-input border border-red-200 bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button variant="secondary" type="button" [disabled]="saving()" (clicked)="onDismiss()">إلغاء</app-button>
          <app-button type="button" [loading]="saving()" [disabled]="invalid()" (clicked)="onSubmit()">{{ isEditMode() ? 'حفظ التعديلات' : 'إنشاء الخطة' }}</app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class CreatePlanDialog {
  private readonly store = inject(HostStore);

  readonly open = input(false);
  readonly plan = input<SubscriptionPlanResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly name = signal('');
  readonly maxUsers = signal('');
  readonly maxInvoices = signal('');
  readonly maxBranches = signal('');
  readonly maxStorage = signal('');
  readonly isTrial = signal(false);
  readonly durationInMonths = signal(12);
  readonly price = signal('');
  readonly discountPercent = signal('');
  readonly isActive = signal(true);

  readonly sparklesIcon = resolveIcon('sparkles');
  readonly layersIcon = resolveIcon('layers');
  readonly zapIcon = resolveIcon('zap');
  readonly usersIcon = resolveIcon('users');
  readonly receiptIcon = resolveIcon('receipt');
  readonly buildingIcon = resolveIcon('building-2');
  readonly databaseIcon = resolveIcon('database');
  readonly checkIcon = resolveIcon('check-circle');
  readonly xIcon = resolveIcon('x');

  readonly durationOptions = DURATION_OPTIONS;

  readonly generatedKey = computed(() => slugify(this.name()));
  readonly isEditMode = computed(() => this.plan() !== null);

  readonly priceError = computed(() => {
    const raw = this.price().trim();
    if (this.isTrial()) return null;
    if (raw === '') return null;
    const n = Number(raw);
    if (Number.isNaN(n) || n < 0) return 'السعر يجب أن يكون صفر أو أكثر';
    return null;
  });

  readonly discountError = computed(() => {
    const raw = this.discountPercent().trim();
    if (raw === '') return null;
    const n = Number(raw);
    if (Number.isNaN(n) || n < 0 || n > 100) return 'نسبة الخصم يجب أن تكون بين 0 و 100';
    return null;
  });

  readonly effectivePriceLabel = computed(() => {
    if (this.isTrial()) return '0.00 (مجانية)';
    const p = Number(this.price().trim() || '0');
    const d = Number(this.discountPercent().trim() || '0');
    if (Number.isNaN(p)) return '—';
    if (!this.discountPercent().trim() || Number.isNaN(d)) return `${p.toFixed(2)}`;
    return `${(p * (1 - d / 100)).toFixed(2)} بعد الخصم`;
  });

  readonly invalid = computed(() => {
    if (!this.name().trim()) return true;
    if (this.priceError() !== null || this.discountError() !== null) return true;
    if (this.isTrial() && this.durationInMonths() !== 1) return true;
    if (!DURATION_OPTIONS.some(o => o.value === this.durationInMonths())) return true;
    if (!this.isTrial()) {
      const p = this.price().trim();
      if (p === '' || Number.isNaN(Number(p)) || Number(p) < 0) return true;
    }
    for (const raw of [this.maxUsers(), this.maxInvoices(), this.maxBranches(), this.maxStorage()]) {
      if (raw !== '' && (Number.isNaN(Number(raw)) || Number(raw) < 0)) return true;
    }
    return false;
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        const p = this.plan();
        if (p) {
          this.name.set(p.name ?? '');
          this.maxUsers.set(p.maximumActiveUsers?.toString() ?? '');
          this.maxInvoices.set(p.maximumPostedInvoicesPerPeriod?.toString() ?? '');
          this.maxBranches.set(p.maximumActiveBranches?.toString() ?? '');
          this.maxStorage.set(p.maximumStorageBytes?.toString() ?? '');
          this.isTrial.set(p.isTrial ?? false);
          this.durationInMonths.set(p.durationInMonths ?? 12);
          this.price.set(p.price?.toString() ?? '');
          this.discountPercent.set(p.discountPercent?.toString() ?? '');
          this.isActive.set(p.isActive ?? true);
        } else {
          this.reset();
        }
        this.store.clearSaveError();
      }
    });
  }

  onTrialToggle(checked: boolean): void {
    this.isTrial.set(checked);
    if (checked) {
      this.durationInMonths.set(1);
      this.price.set('0');
      this.discountPercent.set('');
    } else {
      this.durationInMonths.set(12);
    }
  }

  prettyLimit(raw: string): string {
    const t = raw.trim();
    if (!t) return '∞';
    const n = Number(t);
    return Number.isNaN(n) ? '∞' : String(Math.trunc(n));
  }

  prettyBytes(raw: string): string {
    const t = raw.trim();
    if (!t) return '∞';
    const n = Number(t);
    if (Number.isNaN(n)) return '∞';
    if (n < 1024) return `${n} بايت`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} ك.بايت`;
    return `${(n / (1024 * 1024)).toFixed(1)} م.بايت`;
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  async onSubmit(): Promise<void> {
    if (this.invalid()) return;

    const parseNullableInt = (raw: string): number | null => {
      const trimmed = raw.trim();
      if (trimmed === '') return null;
      const n = Number(trimmed);
      return Number.isNaN(n) ? null : Math.trunc(n);
    };

    const parseNullableLong = (raw: string): number | null => {
      const trimmed = raw.trim();
      if (trimmed === '') return null;
      const n = Number(trimmed);
      return Number.isNaN(n) ? null : Math.trunc(n);
    };

    const priceVal = this.isTrial() ? 0 : Number(this.price().trim() || '0');
    const discountVal = this.discountPercent().trim() === '' ? null : Number(this.discountPercent().trim());

    const payload = {
      name: this.name().trim(),
      key: this.generatedKey(),
      maximumActiveUsers: parseNullableInt(this.maxUsers()),
      maximumPostedInvoicesPerPeriod: parseNullableInt(this.maxInvoices()),
      maximumActiveBranches: parseNullableInt(this.maxBranches()),
      maximumStorageBytes: parseNullableLong(this.maxStorage()),
      isTrial: this.isTrial(),
      durationInMonths: this.durationInMonths(),
      price: priceVal,
      discountPercent: discountVal,
    };

    const editing = this.plan();
    const ok = editing
      ? await this.store.updatePlan(editing.id, { ...payload, isActive: this.isActive() })
      : await this.store.createPlan(payload);

    if (ok) {
      this.reset();
      this.saved.emit();
      this.onDismiss();
    }
  }

  private reset(): void {
    this.name.set('');
    this.maxUsers.set('');
    this.maxInvoices.set('');
    this.maxBranches.set('');
    this.maxStorage.set('');
    this.isTrial.set(false);
    this.durationInMonths.set(12);
    this.price.set('');
    this.discountPercent.set('');
    this.isActive.set(true);
  }
}
