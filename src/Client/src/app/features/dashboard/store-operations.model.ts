import { formatCurrency, formatDateTime, formatWeight } from '../../shared/format/formatters';
import type { BadgeVariant } from '../../shared/ui';
import type {
  CategoryKpi,
  CurrencyTotal,
  EmployeeDayStatsResponse,
  OperationType,
  StoreOperationResponse,
} from './store-operations-api.service';

export const num = (value: number | string | null | undefined): number => Number(value ?? 0);

/** Maps a server status enum name to a UI badge variant (defensive: unknown → neutral). */
export function statusBadgeVariant(status: string | null | undefined): BadgeVariant {
  switch (status?.toLowerCase()) {
    case 'completed':
    case 'paid':
      return 'success';
    case 'partiallypaid':
    case 'partial':
      return 'warning';
    case 'cancelled':
      return 'error';
    default:
      return 'neutral';
  }
}

export function operationTypeVariant(operationType: string | undefined): BadgeVariant {
  return operationType === 'Buy' ? 'gold' : 'success';
}

export function counterpartyLabel(operationType: string | undefined): string {
  return operationType === 'Buy' ? 'البائع' : 'العميل';
}

export function employeeLabel(operationType: string | undefined): string {
  return operationType === 'Buy' ? 'المشتري' : 'البائع';
}

/** Formats a per-currency total list as a single data-mono line (e.g. "1,250.000 د.أ · 450.00 $"). */
export function joinCurrencyTotals(totals: readonly CurrencyTotal[] | undefined): string {
  return (totals ?? [])
    .map((total) => formatCurrency(num(total.amount), total.currency))
    .join(' · ');
}

/** Display model for one operations-table row (pre-formatted so the template stays dumb). */
export interface OperationRow {
  id: string;
  invoiceNumber: string;
  dateText: string;
  typeLabel: string;
  typeVariant: BadgeVariant;
  counterpartyName: string;
  employeeName: string;
  accountName: string;
  totalText: string;
  paidText: string;
  balanceText: string;
  statusLabel: string | null;
  statusVariant: BadgeVariant;
  operationType: OperationType;
  currency: string;
}

export function toOperationRow(operation: StoreOperationResponse): OperationRow {
  const operationType: OperationType = operation.operationType === 'Buy' ? 'Buy' : 'Sale';
  return {
    id: operation.id ?? '',
    invoiceNumber: operation.invoiceNumber ?? '',
    dateText: operation.date ? formatDateTime(operation.date) : '—',
    typeLabel: operation.operationTypeLabel ?? (operationType === 'Buy' ? 'شراء' : 'بيع'),
    typeVariant: operationTypeVariant(operationType),
    counterpartyName: operation.counterpartyName ?? '—',
    employeeName: operation.employeeName ?? '—',
    accountName: operation.accountName ?? '—',
    totalText: formatCurrency(num(operation.totalAmount), operation.currency ?? 'JOD'),
    paidText: formatCurrency(num(operation.amountPaid), operation.currency ?? 'JOD'),
    balanceText: formatCurrency(num(operation.remainingBalance), operation.currency ?? 'JOD'),
    statusLabel: operation.statusLabel ?? null,
    statusVariant: statusBadgeVariant(operation.status),
    operationType,
    currency: operation.currency ?? 'JOD',
  };
}

/** Display model for one today-employee-stats row. */
export interface EmployeeStatRow {
  id: string;
  name: string;
  salesCount: number;
  salesWeight: number;
  salesTotals: string;
  purchasesCount: number;
  purchasesWeight: number;
  purchasesTotals: string;
}

/** Display model for one per-category KPI row (weight = raw row weight in grams). */
export interface CategoryKpiRow {
  id: string;
  name: string;
  weight: number;
  weightText: string;
  count: number;
  totals: string;
}

export function toCategoryKpiRow(entry: CategoryKpi): CategoryKpiRow {
  return {
    id: entry.categoryId ?? entry.categoryName ?? '',
    name: entry.categoryName ?? 'بدون تصنيف',
    weight: num(entry.weightInGrams),
    weightText: formatWeight(num(entry.weightInGrams)),
    count: num(entry.count),
    totals: joinCurrencyTotals(entry.totals),
  };
}

export function toEmployeeStatRow(stat: EmployeeDayStatsResponse): EmployeeStatRow {
  return {
    id: stat.employeeId ?? '',
    name: stat.employeeName ?? '—',
    salesCount: num(stat.salesCount),
    salesWeight: num(stat.salesWeight21K),
    salesTotals: joinCurrencyTotals(stat.salesTotals),
    purchasesCount: num(stat.purchasesCount),
    purchasesWeight: num(stat.purchasesWeight21K),
    purchasesTotals: joinCurrencyTotals(stat.purchasesTotals),
  };
}
