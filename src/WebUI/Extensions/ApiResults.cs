using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.Extensions;

/// <summary>
/// Maps application <see cref="Result"/> values to RFC 9457 <c>ProblemDetails</c>
/// responses for the minimal API layer (plan Phase 7a). Every endpoint maps through
/// these helpers so validation, cross-tenant 404s, conflicts, and server failures all
/// produce the same <c>application/problem+json</c> shape the MVC exception handler and
/// tenant middleware already produce. Response schemas are declared per endpoint with
/// <c>Produces</c> metadata.
/// </summary>
public static class ApiResults
{
    public static IResult Ok<TValue>(Result<TValue> result)
        => result.IsSuccess ? TypedResults.Ok(result.Value) : ToProblem(result.Errors);

    public static IResult Created<TValue>(string uri, Result<TValue> result)
        => result.IsSuccess ? TypedResults.Created(uri, result.Value) : ToProblem(result.Errors);

    public static IResult From<TValue>(Result<TValue> result)
        => result.IsSuccess ? TypedResults.Ok(result.Value) : ToProblem(result.Errors);

    public static IResult From(Result result)
        => result.IsSuccess ? TypedResults.NoContent() : ToProblem(result.Error);

    /// <summary>
    /// Maps any failed result to <c>401 Unauthorized</c> with RFC 9457 ProblemDetails.
    /// Used by the authentication endpoints, whose failures (bad credentials, lockout,
    /// disabled account, disqualified tenant, invalid refresh token) are Unauthorized
    /// semantics regardless of the error's stored type.
    /// </summary>
    public static IResult UnauthorizedFrom<TValue>(Result<TValue> result)
    {
        Error error = result.TopError;

        return TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = error.Code
            });
    }

    private static IResult ToProblem(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return TypedResults.Problem();
        }

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return TypedResults.ValidationProblem(ToModelState(errors));
        }

        return ToProblem(errors[0]);
    }

    private static IResult ToProblem(Error? error)
    {
        if (error is null)
        {
            return TypedResults.Problem();
        }

        return TypedResults.Problem(
            statusCode: StatusFor(error.Type),
            title: TitleFor(error.Type),
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = error.Code
            });
    }

    private static Dictionary<string, string[]> ToModelState(List<Error> errors)
    {
        var modelState = new Dictionary<string, string[]>();

        foreach (Error error in errors)
        {
            string key = string.IsNullOrWhiteSpace(error.Code) ? string.Empty : error.Code;
            if (modelState.TryGetValue(key, out string[]? existing))
            {
                modelState[key] = [.. existing, error.Description];
            }
            else
            {
                modelState[key] = [error.Description];
            }
        }

        return modelState;
    }

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation Error",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Server Error",
    };
}
