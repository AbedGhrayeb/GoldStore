using Application.Abstractions.Messaging;
using Application.Features.Identity.Dtos;

namespace Application.Features.Identity.GetUserInfo;

public sealed record GetUserInfoByIdQuery() : IQuery<UserDto>;
