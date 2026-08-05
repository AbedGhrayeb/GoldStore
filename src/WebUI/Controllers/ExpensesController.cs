using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Features.Expenses.ExpenseCategories;
using Application.Features.Expenses.ExpenseCategories.Create;
using Application.Features.Expenses.ExpenseCategories.Delete;
using Application.Features.Expenses.ExpenseCategories.GetAll;
using Application.Features.Expenses.ExpenseCategories.Update;
using Application.Features.Expenses.Expenses;
using Application.Features.Expenses.Expenses.Create;
using Application.Features.Expenses.Expenses.Delete;
using Application.Features.Expenses.Expenses.GetKpis;
using Application.Features.Expenses.Expenses.GetPaged;
using Application.Features.Expenses.Expenses.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.Expense;

namespace WebUI.Controllers;

[Authorize]
public class ExpensesController(
    IQueryHandler<GetExpensesQuery, PaginatedList<ExpenseResponse>> getExpensesHandler,
    IQueryHandler<GetExpenseKpisQuery, ExpenseKpiResponse> getKpisHandler,
    ICommandHandler<CreateExpenseCommand, Guid> createExpenseHandler,
    ICommandHandler<UpdateExpenseCommand, Updated> updateExpenseHandler,
    ICommandHandler<DeleteExpenseCommand, Deleted> deleteExpenseHandler,
    IQueryHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryResponse>> getCategoriesHandler,
    ICommandHandler<CreateExpenseCategoryCommand, Guid> createCategoryHandler,
    ICommandHandler<UpdateExpenseCategoryCommand, Updated> updateCategoryHandler,
    ICommandHandler<DeleteExpenseCategoryCommand, Deleted> deleteCategoryHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(int page = 1, int pageSize = 20, string? accountName = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        Result<PaginatedList<ExpenseResponse>> result = await getExpensesHandler.Handle(
            new GetExpensesQuery(page, pageSize, accountName, fromDate, toDate, categoryId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetKpis(CancellationToken cancellationToken)
    {
        Result<ExpenseKpiResponse> result = await getKpisHandler.Handle(new GetExpenseKpisQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetCategories(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        Result<List<ExpenseCategoryResponse>> result = await getCategoriesHandler.Handle(
            new GetExpenseCategoriesQuery(activeOnly), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([FromBody] CreateExpenseModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "المصروفات"));
        }

        var command = new CreateExpenseCommand(
            DateOnly.FromDateTime(model.ExpenseDate),
            model.CategoryId,
            model.Description,
            model.Amount,
            model.AccountId);

        Result<Guid> result = await createExpenseHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "المصروفات"));
        }

        return Json(ToastResult.SuccessResult("تم إضافة المصروف بنجاح", "المصروفات", "", "refreshExpenseTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Update([FromBody] EditExpenseModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "المصروفات"));
        }

        var command = new UpdateExpenseCommand(
            model.Id,
            DateOnly.FromDateTime(model.ExpenseDate),
            model.CategoryId,
            model.Description,
            model.Amount,
            model.AccountId);

        Result<Updated> result = await updateExpenseHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "المصروفات"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث المصروف بنجاح", "المصروفات", "", "refreshExpenseTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        Result<Deleted> result = await deleteExpenseHandler.Handle(new DeleteExpenseCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "المصروفات"));
        }

        return Json(ToastResult.SuccessResult("تم حذف المصروف بنجاح", "المصروفات", "", "refreshExpenseTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateCategory([FromBody] ExpenseCategoryModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model?.Name))
        {
            return Json(ToastResult.ErrorResult("اسم التصنيف مطلوب", "تصنيفات المصروفات"));
        }

        Result<Guid> result = await createCategoryHandler.Handle(new CreateExpenseCategoryCommand(model.Name), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "تصنيفات المصروفات"));
        }

        return Json(ToastResult.SuccessResult("تم إضافة التصنيف بنجاح", "تصنيفات المصروفات", "", "refreshCategories"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UpdateCategory([FromBody] UpdateExpenseCategoryModel model, CancellationToken cancellationToken)
    {
        if (model?.Id == null || string.IsNullOrWhiteSpace(model.Name))
        {
            return Json(ToastResult.ErrorResult("البيانات غير صالحة", "تصنيفات المصروفات"));
        }

        Result<Updated> result = await updateCategoryHandler.Handle(new UpdateExpenseCategoryCommand(model.Id, model.Name), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "تصنيفات المصروفات"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث التصنيف بنجاح", "تصنيفات المصروفات", "", "refreshCategories"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        Result<Deleted> result = await deleteCategoryHandler.Handle(new DeleteExpenseCategoryCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "تصنيفات المصروفات"));
        }

        return Json(ToastResult.SuccessResult("تم حذف التصنيف بنجاح", "تصنيفات المصروفات", "", "refreshCategories"));
    }
}
