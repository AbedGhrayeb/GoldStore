/** Shared account-list filters for every form with financial accounts. */

export interface AccountFilterOption {
  id?: string | null;
  currency?: string | null;
  accountType?: string | null;
  isActive?: boolean | null;
}

/** Active accounts matching one currency (exact code match). */
export function filterAccountsByCurrency<T extends AccountFilterOption>(
  accounts: readonly T[] | null | undefined,
  currency: string | null | undefined,
): T[] {
  return (accounts ?? []).filter(
    (account) =>
      account.isActive !== false &&
      (currency == null || currency === '' || account.currency === currency),
  );
}

/** Account type implied by the invoice payment method: 2 = تحويل بنكي → Bank, else نقدي → Cash. */
export function accountTypeForPaymentMethod(
  paymentMethod: string | number | null | undefined,
): string {
  return String(paymentMethod ?? '') === '2' ? 'Bank' : 'Cash';
}

/** Active accounts matching the selected currency AND the payment-method account type. */
export function filterAccountsByPaymentMethod<T extends AccountFilterOption>(
  accounts: readonly T[] | null | undefined,
  currency: string | null | undefined,
  paymentMethod: string | number | null | undefined,
): T[] {
  const expectedType = accountTypeForPaymentMethod(paymentMethod);
  return (accounts ?? []).filter(
    (account) =>
      account.isActive !== false &&
      (currency == null || currency === '' || account.currency === currency) &&
      (account.accountType == null ||
        account.accountType === '' ||
        account.accountType === expectedType),
  );
}
