using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace WebUI.Controllers;

public abstract class BaseController : Controller
{
    protected IActionResult Match(Result result)
    {
        return result.IsSuccess ? NoContent()
            : Problem(title: result.Error.Code,
            detail: result.Error.Description);
    }
    protected IActionResult Match<T>(
     Result<T> result,
     Func<T, IActionResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess(result.Value!)
            : Problem(
                title: result.Error.Code,
                detail: result.Error.Description);
    }
}
