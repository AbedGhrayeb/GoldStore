// <copyright file="GetCategoriesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Categories.GetAll;

public sealed record GetCategoriesQuery : IQuery<List<CategoryResponse>>;
