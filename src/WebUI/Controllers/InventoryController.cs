using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Features.GoldPrices.GetCurrent;
using Application.Features.Inventory.GoldLedger;
using Application.Features.Inventory.GoldLedger.GetKpis;
using Application.Features.Inventory.GoldLedger.GetPaged;
using Application.Features.Inventory.GoldLedger.GetTrend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.Controllers;

[Authorize]
public class InventoryController(
    IQueryHandler<GetGoldPricesQuery, GoldPricesResponse> getGoldPricesHandler,
    IQueryHandler<GetInventoryKpisQuery, InventoryKpiResponse> getKpisHandler,
    IQueryHandler<GetGoldLedgerQuery, PaginatedList<GoldLedgerEntryResponse>> getLedgerHandler,
    IQueryHandler<GetGoldLedgerTrendQuery, List<GoldTrendPoint>> getTrendHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<JsonResult> GetGoldPrices(CancellationToken cancellationToken)
    {
        Result<GoldPricesResponse> result = await getGoldPricesHandler.Handle(new GetGoldPricesQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
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
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetTrend(int days = 7, CancellationToken cancellationToken = default)
    {
        Result<List<GoldTrendPoint>> result = await getTrendHandler.Handle(
            new GetGoldLedgerTrendQuery(days),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
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
        Result<PaginatedList<GoldLedgerEntryResponse>> result = await getLedgerHandler.Handle(
            new GetGoldLedgerQuery(page, pageSize, karat, fromDate, toDate, referenceType),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }
}
