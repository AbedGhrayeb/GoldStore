// <copyright file="DeliveryLineDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.SupplierDeliveries.Create;

public sealed record DeliveryLineDto(
    int Karat,
    decimal WeightInGrams);
