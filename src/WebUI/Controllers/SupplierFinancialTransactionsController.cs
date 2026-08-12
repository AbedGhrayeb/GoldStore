using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Application.Features.SupplierFinancialTransactions.Create;
using Application.Features.SupplierFinancialTransactions.GetKpis;
using Application.Features.SupplierFinancialTransactions.GetPaged;
using Application.Features.SupplierFinancialTransactions.GetPayments;
using Application.Features.SupplierFinancialTransactions.Payments;
using Application.Finance.Accounts;
using Application.Finance.Accounts.GetAll;
using Domain.Tenants;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.SupplierFinancialTransaction;

namespace WebUI.Controllers;

[Authorize]
[RequireFeature(Features.Suppliers)]
public class SupplierFinancialTransactionsController(
    IQueryHandler<GetPagedSupplierFinancialTransactionsQuery, PagedSupplierFinancialTransactionResponse> getPagedHandler,
    IQueryHandler<GetSupplierFinancialKpisQuery, SupplierFinancialKpiResponse> getKpisHandler,
    IQueryHandler<GetFinancialAccountsQuery, List<FinancialAccountResponse>> getFinancialAccountsHandler,
    IQueryHandler<GetPaymentsByTransactionIdQuery, List<SupplierFinancialPaymentResponse>> getPaymentsHandler,
    ICommandHandler<CreateSupplierFinancialTransactionCommand, Guid> createHandler,
    ICommandHandler<CreateSupplierFinancialPaymentCommand, Guid> paymentHandler) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(
        Guid? supplierId = null,
        int? direction = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Result<PagedSupplierFinancialTransactionResponse> result = await getPagedHandler.Handle(
            new GetPagedSupplierFinancialTransactionsQuery(
                Page: page,
                PageSize: pageSize,
                SupplierId: supplierId,
                Direction: direction,
                Search: search),
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
        Result<SupplierFinancialKpiResponse> result = await getKpisHandler.Handle(
            new GetSupplierFinancialKpisQuery(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetAccounts(CancellationToken cancellationToken)
    {
        Result<List<FinancialAccountResponse>> result = await getFinancialAccountsHandler.Handle(
            new GetFinancialAccountsQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new { success = true, accounts = result.Value });
    }

    [HttpGet]
    public async Task<JsonResult> GetPayments(Guid transactionId, CancellationToken cancellationToken)
    {
        Result<List<SupplierFinancialPaymentResponse>> result = await getPaymentsHandler.Handle(
            new GetPaymentsByTransactionIdQuery(transactionId), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new { success = true, payments = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([FromBody] CreateSupplierFinancialTransactionModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "المعاملات المالية"));
        }

        var command = new CreateSupplierFinancialTransactionCommand(
            model.SupplierId,
            model.Direction,
            model.Amount,
            model.Currency,
            model.AccountId,
            model.Date,
            model.Notes,
            model.PaymentLegs?.Select(l => new PaymentLegDto(
                l.AccountId,
                l.Currency,
                l.Amount,
                l.ExchangeRate)).ToList());

        Result<Guid> result = await createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "المعاملات المالية"));
        }

        return Json(ToastResult.SuccessResult("تم إنشاء المعاملة المالية بنجاح", "المعاملات المالية", "", "refreshSupplierFinancialTransactions"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreatePayment([FromBody] CreateSupplierFinancialPaymentModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "المعاملات المالية"));
        }

        var command = new CreateSupplierFinancialPaymentCommand(
            model.TransactionId,
            model.AccountId,
            model.Amount,
            model.Date,
            model.Notes,
            model.PaymentLegs?.Select(l => new PaymentLegDto(
                l.AccountId,
                l.Currency,
                l.Amount,
                l.ExchangeRate)).ToList());

        Result<Guid> result = await paymentHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "المعاملات المالية"));
        }

        return Json(ToastResult.SuccessResult("تم تسجيل الدفعة بنجاح", "المعاملات المالية", "", "refreshSupplierFinancialTransactions"));
    }
}
