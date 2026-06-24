using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetEmployees;

public sealed record GetStoreEmployeesQuery : IQuery<List<string>>;
