using Application.Abstractions.Messaging;

namespace Application.Suppliers.GetAll;

public sealed record GetSuppliersQuery : IQuery<List<SupplierResponse>>;
