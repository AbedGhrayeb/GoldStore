using SharedKernel.Result;

namespace WebUI.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetaILS(this Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");
        }

        (int statusCode, string? title) = result.Error!.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Error"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: result.Error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = result.Error.Code
            });
    }
}
