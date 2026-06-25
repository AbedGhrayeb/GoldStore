using Application.Abstractions.Messaging;
using Application.Features.GoldPrices.GetCurrent;
using Application.Features.Inventory.GoldLedger;
using Application.Features.Inventory.GoldLedger.GetKpis;
using Application.Features.Inventory.GoldLedger.GetPaged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace WebUI.Controllers;

[Authorize]
public class InventoryController(
    IQueryHandler<GetGoldPricesQuery, GoldPricesResponse> getGoldPricesHandler,
    IQueryHandler<GetInventoryKpisQuery, InventoryKpiResponse> getKpisHandler,
    IQueryHandler<GetGoldLedgerQuery, PagedGoldLedgerResponse> getLedgerHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<JsonResult> GetGoldPrices(CancellationToken cancellationToken)
    {
        Result<GoldPricesResponse> result = await getGoldPricesHandler.Handle(new GetGoldPricesQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<JsonResult> GetKpis(CancellationToken cancellationToken)
    {
        Result<InventoryKpiResponse> result = await getKpisHandler.Handle(new GetInventoryKpisQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetLedger(
        int page = 1,
        int pageSize = 20,
        int? karat = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? referenceType = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedGoldLedgerResponse> result = await getLedgerHandler.Handle(
            new GetGoldLedgerQuery(page, pageSize, karat, fromDate, toDate, referenceType),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }
}