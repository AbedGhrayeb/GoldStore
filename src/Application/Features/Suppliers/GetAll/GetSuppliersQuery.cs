// <copyright file="GetSuppliersQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Suppliers.GetAll;

public sealed record GetSuppliersQuery : IQuery<List<SupplierResponse>>;
