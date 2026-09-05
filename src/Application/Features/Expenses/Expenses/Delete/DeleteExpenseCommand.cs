// <copyright file="DeleteExpenseCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.Delete;

public sealed record DeleteExpenseCommand(Guid Id) : ICommand<Deleted>;
