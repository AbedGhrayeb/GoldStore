namespace Application.Abstractions.Authentication;

/// <summary>
/// Signs host administrators in and out on the dedicated host authentication scheme
/// (plan Phase 4 item 6). Host identities are <c>PlatformUser</c>s that carry no tenant
/// and are authorized only through host-only endpoints.
/// </summary>
public interface IPlatformAuthSessionManager
{
    Task SignInAsync(string email, bool rememberMe, CancellationToken cancellationToken);

    Task SignOutAsync();
}
