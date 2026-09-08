// <copyright file="GetSupplierBalancesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Suppliers;

namespace Application.Suppliers.GetBalances;

public sealed record GetSupplierBalancesQuery(Guid Id) : IQuery<SupplierBalancesResponse>;
