using Application.Abstractions.Messaging;

namespace Application.Suppliers.GetById;

public sealed record GetSupplierByIdQuery(Guid Id) : IQuery<SupplierDetailResponse>;