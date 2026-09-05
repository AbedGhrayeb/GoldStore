// <copyright file="DeleteCategoryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Categories.Delete;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand<Deleted>;
