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

function firstValidationMessage(error: ApiError): string | null {
  for (const messages of Object.values(error.validation ?? {})) {
    const message = messages[0];
    if (message !== undefined) return message;
  }
  return null;
}

function toUtcIso(localValue: string, endOfDay = false): string | null {
  if (!localValue) return null;
  // date-only "YYYY-MM-DD" → interpret as local midnight (start) or end-of-day
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

function todayLocalDate(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

function calcEndDate(startDate: string, durationMonths: number): string {
  if (!startDate || !durationMonths) return '';
  if (!/^\d{4}-\d{2}-\d{2}$/.test(startDate)) return '';
  const [y, m, d] = startDate.split('-').map(Number);
  const date = new Date(y, (m ?? 1) - 1, d ?? 1);
  date.setMonth(date.getMonth() + durationMonths);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

const TIME_ZONES: ReadonlyArray<{ value: string; label: string }> = [
  { value: 'Asia/Amman', label: 'Asia/Amman (الأردن)' },
  { value: 'Asia/Jerusalem', label: 'Asia/Jerusalem (القدس)' },
  { value: 'Asia/Hebron', label: 'Asia/Hebron (الخليل)' },
  { value: 'Asia/Gaza', label: 'Asia/Gaza (غزة)' },
  { value: 'Asia/Riyadh', label: 'Asia/Riyadh (السعودية)' },
  { value: 'Europe/Istanbul', label: 'Europe/Istanbul' },
  { value: 'Asia/Dubai', label: 'Asia/Dubai' },
  { value: 'Asia/Beirut', label: 'Asia/Beirut' },
  { value: 'UTC', label: 'UTC' },
];

/**
 * Host-only provision dialog. Mirrors `ProvisionTenantRequest` key-for-key
 * (11 fields) and posts to `POST /host/api/v1/tenants` via `HostStore`.
 * Signature: vault-provision stepper — 3 chambers (المتجر → المسؤول → الاشتراك)
 * with gold progress rail and chamber icons.
 */
@Component({
  selector: 'app-provision-tenant-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, LucideAngularModule],
  template: `
    <app-dialog
      [open]="open()"
      title="تأسيس متجر جديد"
      subtitle="إنشاء مستأجر مع اشتراكه ومسؤول المتجر وحساباته الافتراضية"
      icon="plus"
      maxWidth="max-w-3xl"
      (openChange)="onDismiss()"
    >
      <!-- Stepper rail -->
      <div class="mb-6 flex items-center gap-2">
        @for (step of steps; track step.index) {
          <div class="flex flex-1 items-center gap-2">
            <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-bold transition-colors"
              [class]="stepperClass(step.index)">
              @if (step.index < currentStep()) {
                <lucide-icon [img]="checkIcon" [size]="14" />
              } @else {
                {{ step.index + 1 }}
              }
            </span>
            <span class="hidden text-xs font-medium sm:inline" [class]="step.index <= currentStep() ? 'text-gray-900' : 'text-gray-400'">{{ step.label }}</span>
            @if (step.index < steps.length - 1) {
              <span class="h-px flex-1" [class]="step.index < currentStep() ? 'bg-gold' : 'bg-gray-200'"></span>
            }
          </div>
        }
      </div>

      <div class="space-y-5">
        <!-- decoy to defeat browser autofill of previous tenant -->
        <input type="text" class="hidden" autocomplete="off" tabindex="-1" aria-hidden="true" />
        <input type="password" class="hidden" autocomplete="new-password" tabindex="-1" aria-hidden="true" />
        <!-- Chamber 1 — Store -->
        <section class="overflow-hidden rounded-xl border bg-card shadow-sm" [class]="chamberBorder(0)">
          <div class="flex items-center gap-3 border-b bg-gradient-to-r px-4 py-3" [class]="chamberHeader(0)">
            <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-white text-gold shadow-sm"><lucide-icon [img]="storeIcon" [size]="18" /></span>
            <div>
              <p class="text-sm font-semibold text-gray-900">بيانات المتجر</p>
              <p class="text-xs text-gray-500">الاسم، المعرّف، والمنطقة الزمنية</p>
            </div>
            <span class="ms-auto text-xs font-bold tracking-widest" [class]="currentStep() === 0 ? 'text-gold' : 'text-gray-400'">01</span>
          </div>
          <div class="space-y-4 p-4">
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="storeIcon" [size]="14" class="text-gray-400" />اسم المتجر <span class="text-error">*</span></span>
                <input
                  autocomplete="off"
                  name="tenant-name"
                  [value]="name()"
                  (input)="name.set($any($event.target).value); touchStep(0)"
                  maxlength="200"
                  placeholder="مجوهرات السرحان"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
              </label>
              <label class="block">
                <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="globeIcon" [size]="14" class="text-gray-400" />المعرّف (key) <span class="text-error">*</span></span>
                <input
                  dir="ltr"
                  autocomplete="off"
                  name="tenant-key"
                  [value]="key()"
                  (input)="key.set($any($event.target).value.toLowerCase()); touchStep(0)"
                  maxlength="63"
                  placeholder="sarhan-gold"
                  class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                />
                <span class="mt-1.5 flex items-center gap-1.5 text-xs text-gray-500 data-mono" dir="ltr">
                  <lucide-icon [img]="globeIcon" [size]="11" />
                  https://{{ keyPreview() }}.goldstore.app
                </span>
                @if (keyError(); as err) {
                  <span class="mt-1 block rounded bg-error/10 px-2 py-1 text-xs font-medium text-error">{{ err }}</span>
                }
              </label>
            </div>

            <label class="block">
              <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="clockIcon" [size]="14" class="text-gray-400" />المنطقة الزمنية <span class="text-error">*</span></span>
              <select
                [value]="timeZoneId()"
                (change)="timeZoneId.set($any($event.target).value); touchStep(0)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                @for (tz of timeZones; track tz.value) {
                  <option [value]="tz.value">{{ tz.label }}</option>
                }
              </select>
            </label>
          </div>
        </section>

        <!-- Chamber 2 — Admin -->
        <section class="overflow-hidden rounded-xl border bg-card shadow-sm" [class]="chamberBorder(1)">
          <div class="flex items-center gap-3 border-b bg-gradient-to-r px-4 py-3" [class]="chamberHeader(1)">
            <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-white text-gold shadow-sm"><lucide-icon [img]="userIcon" [size]="18" /></span>
            <div>
              <p class="text-sm font-semibold text-gray-900">مسؤول المتجر</p>
              <p class="text-xs text-gray-500">حساب المدير الأول — يملك كل الصلاحيات</p>
            </div>
            <span class="ms-auto text-xs font-bold tracking-widest" [class]="currentStep() === 1 ? 'text-gold' : 'text-gray-400'">02</span>
          </div>
          <div class="p-4">
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">الاسم الأول <span class="text-error">*</span></span>
                <input autocomplete="off" name="tenant-admin-first" [value]="adminFirstName()" (input)="adminFirstName.set($any($event.target).value); touchStep(1)" placeholder="أحمد" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
              </label>
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">الاسم الأخير <span class="text-error">*</span></span>
                <input autocomplete="off" name="tenant-admin-last" [value]="adminLastName()" (input)="adminLastName.set($any($event.target).value); touchStep(1)" placeholder="السرحان" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
              </label>
            </div>
            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">البريد الإلكتروني <span class="text-error">*</span></span>
                <input dir="ltr" type="email" autocomplete="off" name="tenant-admin-email" data-lpignore="true" data-form-type="other" [value]="adminEmail()" (input)="adminEmail.set($any($event.target).value); touchStep(1)" placeholder="admin@example.com" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
              </label>
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">كلمة المرور <span class="text-error">*</span></span>
                <input dir="ltr" type="password" autocomplete="new-password" name="tenant-admin-password" data-lpignore="true" data-form-type="other" [value]="adminPassword()" (input)="adminPassword.set($any($event.target).value); touchStep(1)" placeholder="≥ 6 أحرف" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
                <span class="mt-1.5 block text-xs" [class]="passwordHintClass()">{{ passwordHint() }}</span>
              </label>
            </div>
            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">رقم الهاتف</span>
                <input dir="ltr" type="tel" autocomplete="off" name="tenant-admin-phone" placeholder="+962 7XXXXXXXX" [value]="adminPhoneNumber()" (input)="adminPhoneNumber.set($any($event.target).value); touchStep(1)" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
              </label>
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">رقم الواتساب</span>
                <input dir="ltr" type="tel" autocomplete="off" name="tenant-admin-whatsapp" placeholder="+962 7XXXXXXXX" [value]="adminWhatsappNumber()" (input)="adminWhatsappNumber.set($any($event.target).value); touchStep(1)" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
              </label>
            </div>
          </div>
        </section>

        <!-- Chamber 3 — Subscription -->
        <section class="overflow-hidden rounded-xl border bg-card shadow-sm" [class]="chamberBorder(2)">
          <div class="flex items-center gap-3 border-b bg-gradient-to-r px-4 py-3" [class]="chamberHeader(2)">
            <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-white text-gold shadow-sm"><lucide-icon [img]="awardIcon" [size]="18" /></span>
            <div>
              <p class="text-sm font-semibold text-gray-900">الاشتراك</p>
              <p class="text-xs text-gray-500">الخطة ودورة الفوترة والفترة</p>
            </div>
            <span class="ms-auto text-xs font-bold tracking-widest" [class]="currentStep() === 2 ? 'text-gold' : 'text-gray-400'">03</span>
          </div>
          <div class="space-y-4 p-4">
            <label class="block">
              <span class="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-gray-700"><lucide-icon [img]="layersIcon" [size]="14" class="text-gray-400" />خطة الاشتراك <span class="text-error">*</span></span>
              <select
                [value]="subscriptionPlanId()"
                (change)="subscriptionPlanId.set($any($event.target).value); touchStep(2)"
                class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
              >
                <option value="">— اختر خطة —</option>
                @for (plan of plans(); track plan.id) {
                  <option [value]="plan.id">{{ plan.name }} — {{ plan.key }}</option>
                }
              </select>
              @if (plansLoading()) {
                <span class="mt-1.5 block text-xs text-gray-500">جاري تحميل الخطط...</span>
              }
            </label>

            @if (selectedPlan(); as plan) {
              <div class="rounded-lg border border-amber-200 bg-amber-50/50 p-3 flex items-center justify-between">
                <div class="text-xs">
                  <p class="font-medium text-gray-700">الخطة: {{ plan.name }} — {{ plan.durationInMonths }} شهر — {{ plan.price.toFixed(2) }}</p>
                  <p class="text-gray-500">دورة الفوترة: {{ computedBillingCycle() === 1 ? 'سنوي' : 'شهري' }} — محسوبة من المدة</p>
                </div>
                <span class="rounded-full bg-white px-3 py-1 text-xs border data-mono">{{ computedBillingCycle() === 1 ? 'سنوي' : 'شهري' }}</span>
              </div>
            }

            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">بداية الاشتراك — تاريخ فقط <span class="text-error">*</span></span>
                <input type="date" dir="ltr" autocomplete="off" [value]="startsAtUtc()" (change)="startsAtUtc.set($any($event.target).value); touchStep(2)" class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none focus:border-gold focus:ring-2 focus:ring-gold/30" />
                <span class="mt-1 block text-xs text-gray-500">افتراضي اليوم — 00:00</span>
              </label>
              <div class="block">
                <span class="mb-1.5 block text-sm font-medium text-gray-700">نهاية الاشتراك — محسوبة تلقائياً</span>
                <div class="w-full rounded-input border border-gray-200 bg-gray-50 px-3 py-2.5 text-left text-sm text-gray-700 data-mono" dir="ltr">
                  {{ computedEndsAt() || '— اختر الخطة وتاريخ البداية' }}
                </div>
                <span class="mt-1 block text-xs text-gray-500">من الخطة — 23:59</span>
              </div>
            </div>
            @if (periodError(); as err) {
              <p class="rounded-lg bg-error/10 px-3 py-2 text-xs font-medium text-error">{{ err }}</p>
            }
          </div>
        </section>

        @if (saveError(); as error) {
          <p class="rounded-input border border-red-200 bg-error/10 px-3 py-2.5 text-sm text-red-700" role="alert">
            {{ errorMessage(error) }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button variant="secondary" type="button" [disabled]="saving()" (clicked)="onDismiss()">إلغاء</app-button>
          <app-button type="button" [loading]="saving()" [disabled]="invalid()" (clicked)="onSubmit()">
            <lucide-icon [img]="sparklesIcon" [size]="16" />
            تأسيس المتجر
          </app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class ProvisionTenantDialog {
  private readonly store = inject(HostStore);

  readonly open = input(false);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;
  readonly plans = this.store.plans;
  readonly plansLoading = this.store.plansLoading;

  readonly name = signal('');
  readonly key = signal('');
  readonly timeZoneId = signal('Asia/Amman');
  readonly adminFirstName = signal('');
  readonly adminLastName = signal('');
  readonly adminEmail = signal('');
  readonly adminPassword = signal('');
  readonly adminPhoneNumber = signal('');
  readonly adminWhatsappNumber = signal('');
  readonly subscriptionPlanId = signal('');
  readonly billingCycle = signal<number>(0);
  readonly startsAtUtc = signal('');
  readonly endsAtUtc = signal('');

  readonly selectedPlan = computed(() => {
    const id = this.subscriptionPlanId();
    return this.plans()?.find(p => p.id === id) ?? null;
  });

  readonly computedBillingCycle = computed(() => {
    const plan = this.selectedPlan();
    if (!plan) return this.billingCycle();
    return plan.durationInMonths >= 12 ? 1 : 0;
  });

  readonly computedEndsAt = computed(() => {
    const start = this.startsAtUtc();
    const plan = this.selectedPlan();
    if (!start || !plan) return '';
    return calcEndDate(start, plan.durationInMonths);
  });

  readonly currentStep = signal(0);

  readonly timeZones = TIME_ZONES;

  readonly checkIcon = resolveIcon('check-circle');
  readonly storeIcon = resolveIcon('building-2');
  readonly globeIcon = resolveIcon('globe');
  readonly clockIcon = resolveIcon('calendar');
  readonly userIcon = resolveIcon('user');
  readonly awardIcon = resolveIcon('award');
  readonly layersIcon = resolveIcon('layers');
  readonly sparklesIcon = resolveIcon('sparkles');

  readonly steps = [
    { index: 0, label: 'المتجر' },
    { index: 1, label: 'المسؤول' },
    { index: 2, label: 'الاشتراك' },
  ];

  readonly keyPreview = computed(() => {
    const raw = this.key().trim().toLowerCase();
    return raw.length > 0 ? raw : 'example';
  });

  readonly keyError = computed(() => {
    const v = this.key().trim();
    if (v.length === 0) return null;
    if (v.length < 3) return 'المعرّف قصير جداً (3 أحرف على الأقل)';
    if (!/^[a-z0-9]([a-z0-9-]*[a-z0-9])?$/.test(v)) return 'المعرّف يجب أن يحتوي على أحرف وأرقام صغيرة وشرطات فقط';
    if (v.length > 63) return 'المعرّف طويل جداً';
    return null;
  });

  readonly periodError = computed(() => {
    const starts = this.startsAtUtc();
    const ends = this.computedEndsAt();
    if (!starts || !ends) return null;
    const s = toUtcIso(starts, false);
    const e = toUtcIso(ends, true);
    if (s === null || e === null) return null;
    if (new Date(e).getTime() <= new Date(s).getTime()) return 'تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية';
    return null;
  });

  readonly passwordHint = computed(() => {
    const v = this.adminPassword();
    if (!v) return '6 أحرف على الأقل — يُفضّل مزيج أحرف وأرقام';
    if (v.length < 6) return 'ضعيفة — أكمل 6 أحرف على الأقل';
    if (v.length < 10) return 'متوسطة — زد الطول لقوة أفضل';
    return 'قوية — ممتاز';
  });

  passwordHintClass(): string {
    const v = this.adminPassword();
    if (!v) return 'text-gray-500';
    if (v.length < 6) return 'text-error font-medium';
    if (v.length < 10) return 'text-amber-600 font-medium';
    return 'text-emerald-600 font-medium';
  }

  stepperClass(index: number): string {
    if (index < this.currentStep()) return 'bg-emerald-500 text-white';
    if (index === this.currentStep()) return 'bg-gold text-white shadow-sm ring-2 ring-gold/30';
    return 'bg-gray-200 text-gray-500';
  }

  chamberBorder(index: number): string {
    return index === this.currentStep() ? 'border-gold/30 ring-1 ring-gold/20' : 'border-gray-200';
  }

  chamberHeader(index: number): string {
    return index === this.currentStep() ? 'from-gold-container/50 to-amber-50/60' : 'from-gray-50 to-white';
  }

  touchStep(index: number): void {
    if (index > this.currentStep()) this.currentStep.set(index);
  }

  readonly invalid = computed(() => {
    if (!this.name().trim()) return true;
    if (!this.key().trim() || this.keyError() !== null) return true;
    if (!this.timeZoneId().trim()) return true;
    if (!this.adminFirstName().trim() || !this.adminLastName().trim()) return true;
    if (!this.adminEmail().trim() || !this.adminPassword().trim() || this.adminPassword().trim().length < 6) return true;
    if (!this.subscriptionPlanId().trim()) return true;
    if (!this.startsAtUtc() || !this.computedEndsAt()) return true;
    if (this.periodError() !== null) return true;
    return false;
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        this.reset();
        this.startsAtUtc.set(todayLocalDate());
        this.store.clearSaveError();
        this.currentStep.set(0);
        if (this.store.plans() === null && !this.store.plansLoading()) {
          void this.store.loadPlans();
        }
      }
    });
  }

  errorMessage(error: ApiError): string {
    return firstValidationMessage(error) ?? error.detail ?? error.title ?? 'تعذّر الحفظ';
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  async onSubmit(): Promise<void> {
    if (this.invalid()) return;
    const startsIso = toUtcIso(this.startsAtUtc(), false);
    const endsIso = toUtcIso(this.computedEndsAt(), true);
    if (startsIso === null || endsIso === null) return;

    const ok = await this.store.provisionTenant({
      name: this.name().trim(),
      key: this.key().trim().toLowerCase(),
      timeZoneId: this.timeZoneId().trim(),
      adminFirstName: this.adminFirstName().trim(),
      adminLastName: this.adminLastName().trim(),
      adminEmail: this.adminEmail().trim(),
      adminPassword: this.adminPassword(),
      adminPhoneNumber: this.adminPhoneNumber().trim() || null,
      adminWhatsappNumber: this.adminWhatsappNumber().trim() || null,
      subscriptionPlanId: this.subscriptionPlanId().trim(),
      billingCycle: Number(this.computedBillingCycle()),
      startsAtUtc: startsIso,
      endsAtUtc: endsIso,
    });

    if (ok) {
      this.reset();
      this.saved.emit();
      this.onDismiss();
    }
  }

  private reset(): void {
    this.name.set('');
    this.key.set('');
    this.timeZoneId.set('Asia/Amman');
    this.adminFirstName.set('');
    this.adminLastName.set('');
    this.adminEmail.set('');
    this.adminPassword.set('');
    this.adminPhoneNumber.set('');
    this.adminWhatsappNumber.set('');
    this.subscriptionPlanId.set('');
    this.billingCycle.set(0);
    this.startsAtUtc.set(todayLocalDate());
    this.endsAtUtc.set('');
    this.currentStep.set(0);
  }
}
