using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Users.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models.Account;


namespace WebUI.Controllers;

public class AccountController(ICommandHandler<LoginUserCommand, Guid> loginCommandHandler, IAuthSessionManager authSessionManager) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginModel { ReturnUrl = returnUrl });
    }
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, CancellationToken cancellationToken)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;
        Result<Guid> result = await loginCommandHandler.Handle(new LoginUserCommand(model.Username, model.Password, model.RememberMe), cancellationToken);


        if (!result.IsSuccess)
        {
            ModelState.AddModelError(result.TopError.Code, result.TopError.Description);
            return View(model);
        }
        await authSessionManager.SignInAsync(model.Username, model.RememberMe.GetValueOrDefault(false), cancellationToken);
        return result.IsSuccess ? RedirectToLocal(model.ReturnUrl) : View(model);
    }
    // ── POST /Account/Logout ──────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authSessionManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        // 1. Check if the URL is locally safe using the brand new .NET 10 static helper
        if (!string.IsNullOrEmpty(returnUrl) && RedirectHttpResult.IsLocalUrl(returnUrl))
        {
            // 2. Use the standard MVC LocalRedirect result to issue a safe 302
            return LocalRedirect(returnUrl);
        }

        // 3. Fallback safely to a default local route if the validation faILS
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

}
