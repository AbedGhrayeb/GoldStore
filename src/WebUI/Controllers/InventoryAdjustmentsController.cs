using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Features.Inventory.Adjustments;
using Application.Features.Inventory.Adjustments.Create;
using Application.Features.Inventory.Adjustments.GetKpis;
using Application.Features.Inventory.Adjustments.GetPaged;
using Domain.Tenants;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.InventoryAdjustment;

namespace WebUI.Controllers;

[Authorize]
[RequireFeature(Features.Inventory)]
public class InventoryAdjustmentsController(
    IQueryHandler<GetInventoryAdjustmentsQuery, PaginatedList<InventoryAdjustmentResponse>> getPagedHandler,
    IQueryHandler<GetInventoryAdjustmentKpisQuery, InventoryAdjustmentKpiResponse> getKpisHandler,
    ICommandHandler<CreateInventoryAdjustmentCommand, Guid> createHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? adjustmentType = null,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedList<InventoryAdjustmentResponse>> result = await getPagedHandler.Handle(
            new GetInventoryAdjustmentsQuery(page, pageSize, fromDate, toDate, adjustmentType),
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
        Result<InventoryAdjustmentKpiResponse> result = await getKpisHandler.Handle(
            new GetInventoryAdjustmentKpisQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([FromBody] CreateInventoryAdjustmentModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "تسويات المخزون"));
        }

        var command = new CreateInventoryAdjustmentCommand(
            model.AdjustmentType,
            model.Karat,
            model.WeightInGrams,
            model.Reason,
            model.Notes,
            model.Date);

        Result<Guid> result = await createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "تسويات المخزون"));
        }

        return Json(ToastResult.SuccessResult("تم تسجيل التسوية بنجاح", "تسويات المخزون", "", "refreshAdjustments"));
    }
}
