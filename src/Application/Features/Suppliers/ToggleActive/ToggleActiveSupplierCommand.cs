// <copyright file="ToggleActiveSupplierCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Suppliers.ToggleActive;

public sealed record ToggleActiveSupplierCommand(Guid Id) : ICommand<Updated>;
