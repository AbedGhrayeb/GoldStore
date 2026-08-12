using SharedKernel.Result;

namespace Application.Abstractions.Subscriptions;

/// <summary>
/// Central subscription quota gate (plan Phase 4 item 7). Handlers and endpoints ask the
/// gate whether the current tenant may add more countable resources instead of duplicating
/// count logic. Plan limits that are null mean unlimited, so the gate is a no-op for plans
/// that define no limits.
/// </summary>
public interface ISubscriptionGate
{
    /// <summary>
    /// Ensures the current tenant may add <paramref name="additionalUsers"/> active users
    /// without exceeding its plan's active-user limit.
    /// </summary>
    Task<Result<Success>> EnsureCanAddUsersAsync(int additionalUsers, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures the current tenant may post <paramref name="additionalInvoices"/> sales
    /// invoices in the current billing period without exceeding its plan limit.
    /// </summary>
    Task<Result<Success>> EnsureCanPostInvoicesAsync(int additionalInvoices, CancellationToken cancellationToken);
}
