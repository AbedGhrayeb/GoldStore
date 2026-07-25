using SharedKernel;
using SharedKernel.Result;

namespace Domain.Users;

public static class UserErrors
{
    public static Error IdRequired => Error.Validation(
        "Users.Id.Required",
        $"The user Id is Required");
    public static Error EmailRequired => Error.Validation(
        "User.Email.Required",
        $"The user Email is Required");
    public static Error PasswordRequired => Error.Validation(
        "User.Password.Required",
        $"The user Password is Required");
    public static Error FirstNameRequired => Error.Validation(
        "User.FirstName.Required",
        $"The user FirstName is Required");
    public static Error LastNameRequired => Error.Validation(
        "User.LastName.Required",
        $"The user LastName is Required");
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "The user with the specified email was not found");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "The provided email is not unique");
}
