using Application.Abstractions.Messaging;
using Application.Features.Finance.Debts;
using Application.Features.Finance.Debts.Create;
using Application.Features.Finance.Debts.GetKpis;
using Application.Features.Finance.Debts.GetPaged;
using Application.Features.Finance.Debts.Payments;
using Application.Features.Finance.Debts.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using WebUI.Models;
using WebUI.Models.Debt;

namespace WebUI.Controllers;

[Authorize]
public class DebtsController(
    IQueryHandler<GetDebtsQuery, PagedDebtResponse> getPagedHandler,
    IQueryHandler<GetDebtKpisQuery, DebtKpiResponse> getKpisHandler,
    ICommandHandler<CreateDebtCommand, Guid> createHandler,
    ICommandHandler<UpdateDebtCommand, Guid> updateHandler,
    ICommandHandler<CreatePaymentCommand, Guid> paymentHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(
        int page = 1,
        int pageSize = 20,
        string? direction = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedDebtResponse> result = await getPagedHandler.Handle(
            new GetDebtsQuery(page, pageSize, direction, search),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetKpis(CancellationToken cancellationToken)
    {
        Result<DebtKpiResponse> result = await getKpisHandler.Handle(
            new GetDebtKpisQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([FromBody] CreateDebtModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "الذمم"));
        }

        var command = new CreateDebtCommand(
            model.Name,
            model.Phone,
            model.Direction,
            model.Currency,
            model.AccountId,
            model.Amount,
            model.Notes,
            model.Date);

        Result<Guid> result = await createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.Error.Description, "الذمم"));
        }

        return Json(ToastResult.SuccessResult("تم إنشاء الدين بنجاح", "الذمم", "", "refreshDebtsAndAccounts"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Update([FromBody] UpdateDebtModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "الذمم"));
        }

        var command = new UpdateDebtCommand(
            model.Id,
            model.Name,
            model.Phone,
            model.NewAmount,
            model.NewAccountId,
            model.Notes);

        Result<Guid> result = await updateHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.Error.Description, "الذمم"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث الدين بنجاح", "الذمم", "", "refreshDebtsAndAccounts"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreatePayment([FromBody] CreatePaymentModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "الذمم"));
        }

        var command = new CreatePaymentCommand(
            model.DebtId,
            model.AccountId,
            model.Amount,
            model.Date,
            model.Notes);

        Result<Guid> result = await paymentHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.Error.Description, "الذمم"));
        }

        return Json(ToastResult.SuccessResult("تم تسجيل الدفعة بنجاح", "الذمم", "", "refreshDebtsAndAccounts"));
    }
}
