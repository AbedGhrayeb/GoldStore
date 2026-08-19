import { Pipe, type PipeTransform } from '@angular/core';

import { formatCurrency, formatDate, formatWeight, type DateMode } from './formatters';

@Pipe({ name: 'appCurrency', standalone: true })
export class AppCurrencyPipe implements PipeTransform {
  transform(value: number | string | null | undefined, currency = 'JOD'): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }
    return formatCurrency(Number(value), currency);
  }
}

@Pipe({ name: 'appWeight', standalone: true })
export class AppWeightPipe implements PipeTransform {
  transform(value: number | string | null | undefined, decimalPlaces = 3): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }
    return formatWeight(Number(value), decimalPlaces);
  }
}

@Pipe({ name: 'appDate', standalone: true })
export class AppDatePipe implements PipeTransform {
  transform(value: Date | string | number | null | undefined, mode: DateMode = 'date'): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }
    return formatDate(value, mode);
  }
}
