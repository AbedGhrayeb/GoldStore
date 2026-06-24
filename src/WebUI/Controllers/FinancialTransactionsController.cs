using Application.Abstractions.Messaging;
using Application.Finance.Transactions;
using Application.Finance.Transactions.GetPaged;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using WebUI.Models.Finance;

namespace WebUI.Controllers;

[Authorize]
public class FinancialTransactionsController(
    IQueryHandler<GetPagedTransactionsQuery, PagedTransactionResponse> getPagedTransactionsHandler) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetPagedTransactions([FromQuery] TransactionFilterModel filter, CancellationToken cancellationToken)
    {
        Result<PagedTransactionResponse> result = await getPagedTransactionsHandler.Handle(
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
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(new
        {
            success = true,
            result.Value.TotalCount,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalPages,
            Items = result.Value.Items.Select(t => new
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
            .Select(c => new { Value = c.ToString(), Label = c switch
            {
                Currency.Jod => "دينار أردني (د.إ)",
                Currency.Usd => "دولار أمريكي ($)",
                Currency.Ils => "شيكل إسرائيلي (₪)",
                _ => c.ToString()
            }})
            .ToList();
        return Json(currencies);
    }

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "Jod" => "د.إ",
        "Usd" => "$",
        "Ils" => "₪",
        _ => currency
    };
}