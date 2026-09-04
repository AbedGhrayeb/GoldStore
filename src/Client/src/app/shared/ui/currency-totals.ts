import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { formatCurrency } from '../format/formatters';

export interface CurrencyTotalLine {
  currency: string;
  text: string;
}

/**
 * Renders a per-currency totals breakdown (e.g. KPI currency sums) with data-mono numerals.
 * Used by the dashboard store-operations KPIs and reused by debts / supplier financial KPIs.
 */
@Component({
  selector: 'app-currency-totals',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (line of lines(); track line.currency) {
      <p class="mt-1 text-lg font-semibold text-gray-900" data-mono>{{ line.text }}</p>
    } @empty {
      <p class="mt-1 text-lg font-semibold text-gray-400" data-mono>{{ empty() }}</p>
    }
  `,
})
export class CurrencyTotals {
  readonly totals = input<readonly { currency: string; symbol: string; amount: number | string }[]>(
    [],
  );
  readonly empty = input('0.000');

  readonly lines = computed<CurrencyTotalLine[]>(() =>
    this.totals().map((total) => ({
      currency: total.currency,
      text: formatCurrency(Number(total.amount ?? 0), total.currency),
    })),
  );
}
