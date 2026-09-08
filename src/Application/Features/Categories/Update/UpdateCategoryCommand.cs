// <copyright file="UpdateCategoryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Categories.Update;

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Description, Guid? ParentCategoryId, bool IsActive, decimal WeightInGrams, int Karat)
    : ICommand<Updated>;
