using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Domain.Employees;

namespace WebUI.Models.User;

public sealed class CreateUserModel
{
    [Required, MaxLength(100), MinLength(3)]
    public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100), MinLength(3)]
    public string LastName { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(20)]
    [PasswordPropertyText]
    public string Password { get; set; } = string.Empty;
    public RoleEnum Role { get; set; } = RoleEnum.None;
}

