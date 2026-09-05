// <copyright file="GetGoldPricesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.GoldPrices.GetCurrent;

public sealed record GetGoldPricesQuery : IQuery<GoldPricesResponse>;
