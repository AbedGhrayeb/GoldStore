import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export interface DecimalConstraints {
  min?: number;
  max?: number;
  maxDecimalPlaces?: number;
}

function parse(value: unknown): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }
  const number = typeof value === 'number' ? value : Number(String(value).trim());
  return Number.isFinite(number) ? number : NaN;
}

function decimalPlacesOf(value: number): number {
  const text = String(value);
  const dot = text.indexOf('.');
  if (dot === -1) {
    return 0;
  }
  return text.length - dot - 1;
}

/** Validates numbers/decimal strings; empty values are valid (pair with `Validators.required`). */
export function decimalValidator(constraints: DecimalConstraints = {}): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = parse(control.value);
    if (value === null) {
      return null;
    }
    if (Number.isNaN(value)) {
      return { decimal: { message: 'يجب إدخال رقم صحيح أو عشري.' } };
    }
    if (constraints.min !== undefined && value < constraints.min) {
      return { decimal: { message: `القيمة يجب أن تكون أكبر من أو تساوي ${constraints.min}.` } };
    }
    if (constraints.max !== undefined && value > constraints.max) {
      return { decimal: { message: `القيمة يجب أن تكون أصغر من أو تساوي ${constraints.max}.` } };
    }
    if (
      constraints.maxDecimalPlaces !== undefined &&
      decimalPlacesOf(value) > constraints.maxDecimalPlaces
    ) {
      return {
        decimal: { message: `لا يُسمح بأكثر من ${constraints.maxDecimalPlaces} خانات عشرية.` },
      };
    }
    return null;
  };
}

/** Weight in grams: non-negative, up to 3 decimal places by default. */
export function weightValidator(maxDecimalPlaces = 3): ValidatorFn {
  return decimalValidator({ min: 0, maxDecimalPlaces });
}
