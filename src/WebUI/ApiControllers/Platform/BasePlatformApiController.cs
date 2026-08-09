using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SharedKernel.Result;

namespace WebUI.ApiControllers.Platform;

/// <summary>
/// Base for platform (catalog) endpoints. Not tenant-scoped and not tenant-user authenticated —
/// protected by platform-admin bearer auth (PlatformApiPolicy).
/// </summary>
[ApiController]
[Authorize(Policy = "PlatformApiPolicy")]
public abstract class BasePlatformApiController : ControllerBase
{
    protected ActionResult Problem(List<Error> errors)
    {
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        return Problem(errors[0]);
    }

    private ObjectResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(statusCode: statusCode, title: error.Description);
    }

    private ActionResult ValidationProblem(List<Error> errors)
    {
        var modelStateDictionary = new ModelStateDictionary();

        errors.ForEach(error => modelStateDictionary.AddModelError(error.Code, error.Description));

        return ValidationProblem(modelStateDictionary);
    }
}
