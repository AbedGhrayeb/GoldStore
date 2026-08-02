using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.Common.Models;
using Application.Features.SalesInvoices;
using Application.Features.SalesInvoices.Create;
using Application.Features.SalesInvoices.GetKpis;
using Application.Features.SalesInvoices.GetNextNumber;
using Application.Features.SalesInvoices.GetPaged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.SalesInvoice;

namespace WebUI.Controllers;

[Authorize]
public class SalesInvoicesController(
    IQueryHandler<GetNextInvoiceNumberQuery, string> getNextNumberHandler,
    IQueryHandler<GetSalesInvoicesQuery, PaginatedList<SalesInvoiceResponse>> getPagedHandler,
    IQueryHandler<GetSalesInvoiceKpisQuery, SalesInvoiceKpiResponse> getKpisHandler,
    ICommandHandler<CreateSalesInvoiceCommand, Guid> createHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetNextNumber(CancellationToken cancellationToken)
    {
        Result<string> result = await getNextNumberHandler.Handle(
            new GetNextInvoiceNumberQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new { invoiceNumber = result.Value });
    }

    [HttpGet]
    public async Task<JsonResult> GetKpis(CancellationToken cancellationToken)
    {
        Result<SalesInvoiceKpiResponse> result = await getKpisHandler.Handle(
            new GetSalesInvoiceKpisQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetPaged(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        Result<PaginatedList<SalesInvoiceResponse>> result = await getPagedHandler.Handle(
            new GetSalesInvoicesQuery(page, pageSize, fromDate, toDate, search, status),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([FromBody] CreateSalesInvoiceModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "المبيعات"));
        }

        var command = new CreateSalesInvoiceCommand(
            model.CustomerName,
            model.CustomerPhone,
            model.Date,
            model.Currency,
            model.Items.Select(i => new SalesInvoiceItemDto(
                i.CategoryId,
                i.Karat,
                i.WeightInGrams,
                i.PricePerGram)).ToList(),
            model.TotalAmount,
            model.AmountPaid,
            model.PaymentMethod,
            model.AccountId,
            model.BuyerAccountNumber,
            model.EmplyeeId,
            model.PaymentLegs?.Select(l => new PaymentLegDto(
                l.AccountId,
                l.Currency,
                l.Amount,
                l.ExchangeRate)).ToList(),
            model.Notes);

        Result<Guid> result = await createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "المبيعات"));
        }

        return Json(ToastResult.SuccessResult("تم إصدار الفاتورة بنجاح", "المبيعات", "", "refreshSales"));
    }
}
