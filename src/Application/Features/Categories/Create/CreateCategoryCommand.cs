// <copyright file="CreateCategoryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Categories.Create;

public sealed record CreateCategoryCommand(string Name, string? Description, Guid? ParentCategoryId, bool IsActive, decimal WeightInGrams, int Karat)
    : ICommand<Guid>;
