using Application.Abstractions.Messaging;

namespace Application.Features.GoldPrices.GetCurrent;

public sealed record GetGoldPricesQuery : IQuery<GoldPricesResponse>;