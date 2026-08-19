export type Karat = 18 | 21 | 24;

const AR_LOCALE = 'ar-JO-u-nu-latn';

export function isKarat(value: number): value is Karat {
  return value === 18 || value === 21 || value === 24;
}

export function roundHalfToEven(value: number, decimalPlaces: number): number {
  const factor = 10 ** decimalPlaces;
  const scaled = value * factor;
  const rounded = Math.round(scaled);
  if (Math.abs(scaled - Math.trunc(scaled)) !== 0.5) {
    return rounded / factor;
  }
  const even = Math.trunc(scaled) % 2 === 0 ? Math.trunc(scaled) : Math.trunc(scaled) + 1;
  return even / factor;
}

/**
 * Mirrors `Domain.Common.GoldWeight.CalculateEquivalent21KWeight`:
 * 21K → unchanged, 24K → weight × 1000 / 875, 18K → weight × 700 / 875 (× 0.8 store
 * convention — deliberately NOT weight × 18 / 21). Rounded half-to-even to 3 dp, matching
 * `decimal.Round(..., 3)` on the server.
 */
export function calculateEquivalent21KWeight(weightInGrams: number, karat: Karat): number {
  const raw =
    karat === 24
      ? (weightInGrams / 875) * 1000
      : karat === 18
        ? (weightInGrams * 700) / 875
        : weightInGrams;
  return roundHalfToEven(raw, 3);
}

const WEIGHT_FORMATTER = (dp: number): Intl.NumberFormat =>
  new Intl.NumberFormat(AR_LOCALE, { minimumFractionDigits: dp, maximumFractionDigits: dp });

const weightFormatters = new Map<number, Intl.NumberFormat>();

function weightFormatter(dp: number): Intl.NumberFormat {
  let formatter = weightFormatters.get(dp);
  if (!formatter) {
    formatter = WEIGHT_FORMATTER(dp);
    weightFormatters.set(dp, formatter);
  }
  return formatter;
}

export function formatWeight(grams: number, decimalPlaces = 3): string {
  return weightFormatter(decimalPlaces).format(grams);
}

const CURRENCY_DP: Readonly<Record<string, number>> = {
  JOD: 3,
  USD: 2,
  ILS: 2,
};

const CURRENCY_FORMATTERS = new Map<string, Intl.NumberFormat>();

export function currencyFormatter(currency: string): Intl.NumberFormat {
  let formatter = CURRENCY_FORMATTERS.get(currency);
  if (!formatter) {
    const dp = CURRENCY_DP[currency] ?? 2;
    formatter = new Intl.NumberFormat(AR_LOCALE, {
      style: 'currency',
      currency,
      minimumFractionDigits: dp,
      maximumFractionDigits: dp,
    });
    CURRENCY_FORMATTERS.set(currency, formatter);
  }
  return formatter;
}

export function formatCurrency(amount: number, currency = 'JOD'): string {
  return currencyFormatter(currency).format(amount);
}

const DATE_FORMATTERS = new Map<string, Intl.DateTimeFormat>();

export type DateMode = 'date' | 'long' | 'datetime' | 'time';

function dateFormatter(mode: DateMode): Intl.DateTimeFormat {
  const key = `ar-JO-u-nu-latn:${mode}`;
  let formatter = DATE_FORMATTERS.get(key);
  if (!formatter) {
    const options: Intl.DateTimeFormatOptions =
      mode === 'long'
        ? { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' }
        : mode === 'datetime'
          ? {
              day: '2-digit',
              month: '2-digit',
              year: 'numeric',
              hour: '2-digit',
              minute: '2-digit',
            }
          : mode === 'time'
            ? { hour: '2-digit', minute: '2-digit' }
            : { day: '2-digit', month: '2-digit', year: 'numeric' };
    formatter = new Intl.DateTimeFormat('ar-JO-u-nu-latn', options);
    DATE_FORMATTERS.set(key, formatter);
  }
  return formatter;
}

export function toDate(value: Date | string | number): Date {
  return value instanceof Date ? value : new Date(value);
}

export function formatDate(value: Date | string | number, mode: DateMode = 'date'): string {
  return dateFormatter(mode).format(toDate(value));
}

export function formatDateTime(value: Date | string | number): string {
  return formatDate(value, 'datetime');
}

export function formatTime(value: Date | string | number): string {
  return formatDate(value, 'time');
}
