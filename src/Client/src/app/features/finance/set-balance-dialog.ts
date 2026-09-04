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

import type { ApiError } from '../../core/http/api-error';
import { formatCurrency } from '../../shared/format/formatters';
import { Button, Dialog } from '../../shared/ui';
import type { AccountWithBalanceResponse } from './finance-api.service';
import { FinanceStore } from './finance-store';

/**
 * P3.10 — adjust an account's balance to a new target value. The server records a
 * ManualAdjustment ledger entry for the difference (in or out) rather than overwriting
 * anything, keeping the "balances are ledger sums" invariant intact.
 */
@Component({
  selector: 'app-set-balance-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog],
  template: `
    <app-dialog
      [open]="open()"
      title="تعديل رصيد الحساب"
      subtitle="{{ account()?.name ?? '' }}"
      icon="wallet"
      (openChange)="onDismiss()"
    >
      <div class="space-y-5">
        <div class="rounded-input bg-gold-container/40 px-4 py-3">
          <div class="flex items-center justify-between text-sm text-gray-700">
            <span>الرصيد الحالي</span>
            <span class="font-semibold" data-mono>{{ currentBalanceText() }}</span>
          </div>
          @if (difference() !== null) {
            <div class="mt-1 flex items-center justify-between text-sm">
              <span>الفرق</span>
              <span
                class="font-semibold"
                [class.text-emerald-700]="(difference() ?? 0) > 0"
                [class.text-red-700]="(difference() ?? 0) < 0"
                data-mono
              >
                {{ differenceText() }}
              </span>
            </div>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="target-balance"
            >الرصيد الجديد <span class="text-error">*</span></label
          >
          <input
            id="target-balance"
            type="number"
            dir="ltr"
            step="0.001"
            min="0"
            [value]="targetBalance()"
            (input)="targetBalance.set($any($event.target).value)"
            placeholder="0.000"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-left text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          />
          @if (invalidTarget()) {
            <p class="mt-1 text-xs text-error">أدخل رصيداً صحيحاً (0 أو أكثر).</p>
          }
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700" for="balance-notes"
            >سبب التعديل</label
          >
          <textarea
            id="balance-notes"
            rows="2"
            [value]="notes()"
            (input)="notes.set($any($event.target).value)"
            placeholder="مثال: تسوية جرد نقدي"
            class="w-full rounded-input border border-gray-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
          ></textarea>
        </div>

        @if (saveError(); as error) {
          <p class="rounded-input bg-error/10 px-3 py-2 text-sm text-red-700" role="alert">
            {{ error.detail ?? error.title ?? 'تعذّر الحفظ' }}
          </p>
        }

        <div class="flex justify-end gap-3 pt-1">
          <app-button
            variant="secondary"
            type="button"
            [disabled]="saving()"
            (clicked)="onDismiss()"
          >
            إلغاء
          </app-button>
          <app-button type="button" [loading]="saving()" [disabled]="invalidTarget()" (clicked)="onSubmit()">
            حفظ الرصيد
          </app-button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class SetBalanceDialog {
  private readonly store = inject(FinanceStore);

  readonly open = input(false);
  readonly account = input<AccountWithBalanceResponse | null>(null);
  readonly saved = output<void>();
  readonly openChange = output<boolean>();

  readonly saving = this.store.saving;
  readonly saveError = this.store.saveError;

  readonly targetBalance = signal('');
  readonly notes = signal('');

  readonly currentBalanceText = computed(() =>
    formatCurrency(Number(this.account()?.balance ?? 0), this.account()?.currency ?? 'JOD'),
  );

  readonly invalidTarget = computed(() => {
    const value = Number(this.targetBalance());
    return !Number.isFinite(value) || value < 0;
  });

  readonly difference = computed(() => {
    const value = Number(this.targetBalance());
    if (!Number.isFinite(value)) {
      return null;
    }
    return value - Number(this.account()?.balance ?? 0);
  });

  readonly differenceText = computed(() => {
    const difference = this.difference();
    if (difference === null || Math.abs(difference) < 0.0005) {
      return 'بدون تغيير';
    }
    const sign = difference > 0 ? '+' : '−';
    return `${sign}${Math.abs(difference).toFixed(3)} ${this.account()?.currency ?? ''}`;
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        this.targetBalance.set(String(this.account()?.balance ?? 0));
        this.notes.set('');
        this.store.clearSaveError();
      }
    });
  }

  onDismiss(): void {
    this.openChange.emit(false);
  }

  onSubmit(): void {
    const account = this.account();
    if (!account?.id || this.invalidTarget()) {
      return;
    }
    void (async () => {
      const ok = await this.store.setBalance(account.id!, {
        targetBalance: Number(this.targetBalance()),
        notes: this.notes().trim() || null,
      });
      if (ok) {
        this.saved.emit();
        this.onDismiss();
      }
    })();
  }
}
