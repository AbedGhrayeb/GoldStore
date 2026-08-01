using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Create;
using Application.Users.Delete;
using Application.Users.GetAllUsers;
using Application.Users.GetById;
using Application.Users.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models.User;

namespace WebUI.Controllers;

[Authorize]
public class UsersController(
    IQueryHandler<GetUsersQuery, List<UserResponse>> getUsersHandler,
    IQueryHandler<GetUserByIdQuery, UserResponse> getUserByIdHandler,
    ICommandHandler<CreateUserCommand, Guid> createUserHandler,
    ICommandHandler<UpdateUserCommand, bool> updateUserHandler,
    ICommandHandler<DeleteUserCommand, bool> deleteUserHandler,
    IUserContext userContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Result<List<UserResponse>> result = await getUsersHandler.Handle(new GetUsersQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return View(new List<UserModel>());
        }

        return View(result.Value.Select(x => new UserModel { Id = x.Id, Email = x.Email, FirstName = x.FirstName, LastName = x.LastName, DispalyName = $"{x.FirstName} {x.LastName}" }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await getUserByIdHandler.Handle(
            new GetUserByIdQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        UserResponse user = result.Value;
        return View(new EditUserModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] EditUserModel model, CancellationToken cancellationToken)
    {
        if (model.Id != userContext.UserId)
        {
            return Json(new { success = false, error = "غير مصرح بتعديل بيانات مستخدم آخر" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(new { success = false, errors });
        }

        Result<bool> result = await updateUserHandler.Handle(
            new UpdateUserCommand(model.Id, model.FirstName, model.LastName, model.Password),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new { success = true, redirectUrl = Url.Action(nameof(Profile)) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateUserModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
                });
            }
            return View(model);
        }

        Result<Guid> result = await createUserHandler.Handle(
            new CreateUserCommand(model.Email, model.FirstName, model.LastName, model.Password),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(result.TopError.Code, result.TopError.Description);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
                });
            }
            return View(model);
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] EditUserModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
                });
            }
            return View(model);
        }

        Result<bool> result = await updateUserHandler.Handle(
            new UpdateUserCommand(model.Id, model.FirstName, model.LastName, model.Password),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(result.TopError.Code, result.TopError.Description);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                )
                });
            }
            return View(model);
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUserHandler.Handle(new DeleteUserCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(result.TopError.Code, result.TopError.Description);
        }

        return RedirectToAction(nameof(Index));
    }
}
