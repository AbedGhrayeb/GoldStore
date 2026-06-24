using SharedKernel;

namespace Application.Common.Errors;

public static class ApplicationErrors
{
    public static Error EmptyUsersList =>
    Error.Failure(
           "ApplicationErrors.Users.NotFoundAnyUsers",
           $"No users found.");
    public static Error LoginFailed =>
    Error.Failure(
           "ApplicationErrors.Users.LoginFailed",
           $"Email or password is incorrect.");
}
