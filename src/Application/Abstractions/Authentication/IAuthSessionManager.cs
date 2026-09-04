using Domain.Users;

namespace Application.Abstractions.Authentication;

public interface IAuthSessionManager
{
    Task SignInAsync(string username, bool rememberMe, CancellationToken cancellationToken);
    Task SignOutAsync();
}
