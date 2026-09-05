// <copyright file="EmployeeRespone.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.StoreOperations.GetEmployees;

public sealed record EmployeeResponse(Guid Id, string Name)
{
}
