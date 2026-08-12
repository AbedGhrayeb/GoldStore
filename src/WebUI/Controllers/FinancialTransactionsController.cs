using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Finance.Transactions;
using Application.Finance.Transactions.GetPaged;
using Domain.Common;
using Domain.Tenants;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models.Finance;
namespace WebUI.Controllers;
[Authorize]
[RequireFeature(Features.Finance)]
public class FinancialTransactionsController(
    IQueryHandler<GetPagedTransactionsQuery, PaginatedList<RecentTransactionResponse>> getPagedTransactionsHandler) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetPagedTransactions([FromQuery] TransactionFilterModel filter, CancellationToken cancellationToken)
    {
        Result<PaginatedList<RecentTransactionResponse>> result = await getPagedTransactionsHandler.Handle(
            new GetPagedTransactionsQuery(
                Page: filter.Page,
                PageSize: filter.PageSize,
                AccountName: filter.AccountName,
                FromDate: filter.FromDate,
                ToDate: filter.ToDate,
                Currency: filter.Currency,
                AccountType: filter.AccountType),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new
        {
            success = true,
            result.Value.TotalCount,
            result.Value.PageNumber,
            result.Value.PageSize,
            result.Value.TotalPages,
            Items = result.Value.Items!.Select(t => new
            {
                t.Id,
                Date = t.Date.ToString("yyyy-MM-dd"),
                t.Description,
                t.AccountName,
                t.Amount,
                t.Currency,
                t.TransactionType,
                t.ReferenceType,
                CurrencySymbol = GetCurrencySymbol(t.Currency)
            })
        });
    }

    [HttpGet]
    public JsonResult GetCurrencies()
    {
        var currencies = SupportedValues.Currencies
            .Select(c => new
            {
                Value = c.ToString(),
                Label = c switch
                {
                    Currency.JOD => "دينار أردني (د.إ)",
                    Currency.USD => "دولار أمريكي ($)",
                    Currency.ILS => "شيكل إسرائيلي (₪)",
                    _ => c.ToString()
                }
            })
            .ToList();
        return Json(currencies);
    }

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "JOD" => "د.إ",
        "USD" => "$",
        "ILS" => "₪",
        _ => currency
    };
}
