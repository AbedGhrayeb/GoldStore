using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace WebUI.Infrastructure;

public static class CustomResults
{
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }

        return Results.Problem(
            title: GetTitle(result.Error),
            detail: GetDetail(result.Error),
            type: GetType(result.Error.Type),
            statusCode: GetStatusCode(result.Error.Type),
            extensions: GetErrors(result));

        static string GetTitle(Error error) =>
            error.Type switch
            {
                ErrorType.Validation => error.Code,
                ErrorType.Problem => error.Code,
                ErrorType.NotFound => error.Code,
                ErrorType.Conflict => error.Code,
                _ => "Server failure"
            };

        static string GetDetail(Error error) =>
            error.Type switch
            {
                ErrorType.Validation => error.Description,
                ErrorType.Problem => error.Description,
                ErrorType.NotFound => error.Description,
                ErrorType.Conflict => error.Description,
                _ => "An unexpected error occurred"
            };

        static string GetType(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };

        static int GetStatusCode(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

        static Dictionary<string, object?>? GetErrors(Result result)
        {
            if (result.Error is not ValidationError validationError)
            {
                return null;
            }

            return new Dictionary<string, object?>
            {
                { "errors", validationError.Errors }
            };
        }
    }

    // ---------------------------
    // Non-generic Result
    // ---------------------------
    public static IActionResult MatchToActionResult(
        this Result result,
        Func<IActionResult> onSuccess,
        Func<Result, IActionResult> onFailure)
    {
        return result.IsSuccess
            ? onSuccess()
            : onFailure(result);
    }

    // ---------------------------
    // Generic Result<T>
    // ---------------------------
    public static IActionResult MatchToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult> onSuccess,
        Func<Result<T>, IActionResult> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result);
    }

    // ---------------------------
    // Convenience helpers (recommended)
    // ---------------------------
    public static IActionResult ToActionResult(this Result result)
    {
        return result.MatchToActionResult(
            onSuccess: () => new OkResult(),
            onFailure: r => new BadRequestObjectResult(r.Error)
        );
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.MatchToActionResult(
            onSuccess: value => new OkObjectResult(value),
            onFailure: r => new BadRequestObjectResult(r.Error)
        );
    }
}

