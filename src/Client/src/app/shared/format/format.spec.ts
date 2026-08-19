import { FormControl } from '@angular/forms';

import {
  AppCurrencyPipe,
  AppDatePipe,
  AppWeightPipe,
  calculateEquivalent21KWeight,
  decimalValidator,
  formatCurrency,
  formatDate,
  formatWeight,
  roundHalfToEven,
  weightValidator,
} from '.';

describe('gold weight formatters', () => {
  it('leaves 21K weight unchanged', () => {
    expect(calculateEquivalent21KWeight(10, 21)).toBe(10);
  });

  it('converts 24K by ×1000/875', () => {
    expect(calculateEquivalent21KWeight(875, 24)).toBe(1000);
    expect(calculateEquivalent21KWeight(10, 24)).toBe(11.429);
  });

  it('converts 18K by ×700/875 (store convention, not ×18/21)', () => {
    expect(calculateEquivalent21KWeight(10, 18)).toBe(8);
    expect(calculateEquivalent21KWeight(8.75, 18)).toBe(7);
  });

  it('rounds half-to-even to 3 dp like decimal.Round(…, 3)', () => {
    expect(roundHalfToEven(12.3455, 3)).toBe(12.346);
    expect(roundHalfToEven(12.3445, 3)).toBe(12.344);
  });

  it('formats weight with 3 dp and latin digits', () => {
    expect(formatWeight(12.5)).toBe('12.500');
    expect(formatWeight(3.2555, 2)).toBe('3.26');
  });
});

describe('currency formatters', () => {
  const normalize = (value: string): string => value.replace(/[\u200f\u200e\u202c\u00a0]/g, '');

  it('formats JOD with 3 decimal places', () => {
    expect(normalize(formatCurrency(12.5, 'JOD'))).toBe('12.500د.أ.');
  });

  it('formats USD and ILS with 2 decimal places', () => {
    expect(normalize(formatCurrency(99.9, 'USD'))).toBe('99.90US$');
    expect(normalize(formatCurrency(99.9, 'ILS'))).toBe('99.90₪');
  });
});

describe('date formatters', () => {
  const normalize = (value: string): string => value.replace(/[\u200f\u200e\u202c\u00a0]/g, '');

  it('formats a date in ar-JO style with latin digits', () => {
    expect(normalize(formatDate(new Date(2026, 7, 19)))).toBe('19/08/2026');
  });

  it('formats date-time and time modes', () => {
    const date = new Date(2026, 7, 19, 14, 5);
    expect(normalize(formatDate(date, 'datetime'))).toContain('19/08/2026');
    expect(normalize(formatDate(date, 'time'))).toMatch(/02:05|14:05/);
  });

  it('accepts ISO strings', () => {
    const normalize = (value: string): string => value.replace(/[\u200f\u200e\u202c\u00a0]/g, '');
    expect(normalize(formatDate('2026-08-19T00:00:00', 'date'))).toBe('19/08/2026');
  });
});

describe('decimal validators', () => {
  it('accepts empty values (requiredness is handled by Validators.required)', () => {
    expect(decimalValidator()(new FormControl(''))).toBeNull();
    expect(decimalValidator()(new FormControl(null))).toBeNull();
  });

  it('rejects non-numeric input', () => {
    expect(decimalValidator()(new FormControl('abc'))).toEqual({
      decimal: expect.objectContaining({ message: expect.any(String) }),
    });
  });

  it('rejects values below the minimum', () => {
    const errors = decimalValidator({ min: 0 })(new FormControl(-1));
    expect(errors?.['decimal']).toBeDefined();
  });

  it('rejects more decimals than allowed', () => {
    const errors = decimalValidator({ maxDecimalPlaces: 3 })(new FormControl(1.2345));
    expect(errors?.['decimal']).toBeDefined();
    expect(decimalValidator({ maxDecimalPlaces: 3 })(new FormControl(1.234))).toBeNull();
  });

  it('accepts strings and numbers', () => {
    expect(decimalValidator({ maxDecimalPlaces: 2 })(new FormControl('3.25'))).toBeNull();
    expect(decimalValidator({ maxDecimalPlaces: 2 })(new FormControl(3.25))).toBeNull();
  });
});

describe('weight validator', () => {
  it('rejects negative weights and >3 dp by default', () => {
    expect(weightValidator()(new FormControl(-0.5))).not.toBeNull();
    expect(weightValidator()(new FormControl(0.1234))).not.toBeNull();
    expect(weightValidator()(new FormControl(12.345))).toBeNull();
  });
});

describe('format pipes', () => {
  const currencyPipe = new AppCurrencyPipe();
  const weightPipe = new AppWeightPipe();
  const datePipe = new AppDatePipe();

  it('appCurrency formats and blanks empty input', () => {
    const normalize = (value: string): string => value.replace(/[\u200f\u200e\u202c\u00a0]/g, '');
    expect(normalize(currencyPipe.transform(12.5))).toBe('12.500د.أ.');
    expect(currencyPipe.transform(null)).toBe('');
  });

  it('appWeight formats with default 3 dp', () => {
    expect(weightPipe.transform(5)).toBe('5.000');
    expect(weightPipe.transform('')).toBe('');
  });

  it('appDate formats with the requested mode', () => {
    const normalize = (value: string): string => value.replace(/[\u200f\u200e\u202c\u00a0]/g, '');
    expect(normalize(datePipe.transform('2026-08-19T00:00:00'))).toBe('19/08/2026');
    expect(datePipe.transform(undefined)).toBe('');
  });
});
