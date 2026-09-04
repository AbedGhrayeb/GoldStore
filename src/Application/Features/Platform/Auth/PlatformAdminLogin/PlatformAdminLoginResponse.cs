namespace Application.Features.Platform.Auth.PlatformAdminLogin;

public sealed record PlatformAdminLoginResponse(string AccessToken, DateTime ExpiresOnUtc);
