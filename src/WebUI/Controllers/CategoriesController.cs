using Application.Abstractions.Messaging;
using Application.Categories;
using Application.Categories.Create;
using Application.Categories.GetAll;
using Application.Categories.ToggleActive;
using Application.Categories.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models.Category;

namespace WebUI.Controllers;

[Authorize]
public class CategoriesController(
    IQueryHandler<GetCategoriesQuery, List<CategoryResponse>> getCategoriesHandler,
    ICommandHandler<CreateCategoryCommand, Guid> createCategoryHandler,
    ICommandHandler<UpdateCategoryCommand, Updated> updateCategoryHandler,
    ICommandHandler<ToggleActiveCategoryCommand, Updated> toggleActiveHandler) : BaseController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Result<List<CategoryResponse>> result = await getCategoriesHandler.Handle(new GetCategoriesQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return View(new List<CategoryModel>());
        }

        var models = result.Value.Select(c => new CategoryModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategoryName,
            IsActive = c.IsActive,
        }).ToList();

        return View(models);
    }

    [HttpGet]
    public async Task<JsonResult> GetAll(CancellationToken cancellationToken)
    {
        Result<List<CategoryResponse>> result = await getCategoriesHandler.Handle(new GetCategoriesQuery(), cancellationToken);
        List<CategoryResponse> categories = result.IsSuccess ? result.Value : [];
        return Json(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateCategoryModel model, CancellationToken cancellationToken)
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
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>())
                });
            }
            return RedirectToAction(nameof(Index));
        }

        Result<Guid> result = await createCategoryHandler.Handle(
            new CreateCategoryCommand(model.Name, model.Description, model.ParentCategoryId, model.IsActive),
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
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>())
                });
            }
            return RedirectToAction(nameof(Index));
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] EditCategoryModel model, CancellationToken cancellationToken)
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
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>())
                });
            }
            return RedirectToAction(nameof(Index));
        }

        Result<Updated> result = await updateCategoryHandler.Handle(
            new UpdateCategoryCommand(model.Id, model.Name, model.Description, model.ParentCategoryId, model.IsActive),
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
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>())
                });
            }
            return RedirectToAction(nameof(Index));
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        Result<Updated> result = await toggleActiveHandler.Handle(new ToggleActiveCategoryCommand(id), cancellationToken);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (!result.IsSuccess)
            {
                return Json(new { success = false, error = result.TopError.Description });
            }
            return Json(new { success = true });
        }
        return RedirectToAction(nameof(Index));
    }
}
