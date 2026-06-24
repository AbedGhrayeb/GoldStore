namespace WebUI.Models.User;

public sealed record UserModel
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string DispalyName { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }


}
