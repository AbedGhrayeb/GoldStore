using Application.Abstractions.Messaging;

namespace Application.Categories.GetAll;

public sealed record GetCategoriesQuery : IQuery<List<CategoryResponse>>;
