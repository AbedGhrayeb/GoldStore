// <copyright file="GetSupplierByIdQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Suppliers.GetById;

public sealed record GetSupplierByIdQuery(Guid Id) : IQuery<SupplierDetailResponse>;
