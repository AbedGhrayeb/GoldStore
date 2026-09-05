// <copyright file="GetTenantReconciliationQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Tenants.Reconciliation;

/// <summary>
/// Host-only reconciliation query (plan Phase 6 items 4 and 7). Validates tenant
/// ownership of every tenant-owned table and recomputes gold, financial, debt, and
/// supplier balances from the raw ledger entries so an operator can compare state
/// before and after a data migration. When <see cref="TenantId"/> is null, all
/// tenants are reconciled.
/// </summary>
public sealed record GetTenantReconciliationQuery(Guid? TenantId) : IQuery<TenantReconciliationResponse>;
