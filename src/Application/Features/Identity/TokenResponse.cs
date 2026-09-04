namespace Application.Features.Identity;

public record TokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresOnUtc { get; set; }
}
