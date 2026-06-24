using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Login;

public sealed record LoginUserCommand(string Email, string Password,bool? RememberMe) : ICommand;
