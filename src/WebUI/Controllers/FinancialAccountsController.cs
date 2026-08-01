using Application.Abstractions.Messaging;
using Application.Finance.Accounts;
using Application.Finance.Accounts.Create;
using Application.Finance.Accounts.GetBalance;
using Application.Finance.Accounts.GetWithBalance;
using Application.Finance.Accounts.SetBalance;
using Application.Finance.Transactions;
using Application.Finance.Transactions.GetRecent;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.Finance;

namespace WebUI.Controllers;

[Authorize]
public class FinancialAccountsController(
    IQueryHandler<GetAccountsWithBalancesQuery, List<AccountWithBalanceResponse>> getAccountsWithBalancesHandler,
    IQueryHandler<GetRecentTransactionsQuery, List<RecentTransactionResponse>> getRecentTransactionsHandler,
    ICommandHandler<CreateFinancialAccountCommand, Guid> createFinancialAccountHandler,
    IQueryHandler<GetAccountBalanceQuery, AccountBalanceResponse> getAccountBalanceHandler,
    ICommandHandler<SetAccountBalanceCommand, Updated> setAccountBalanceHandler) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetCashAccounts(CancellationToken cancellationToken)
    {
        Result<List<AccountWithBalanceResponse>> result = await getAccountsWithBalancesHandler.Handle(
            new GetAccountsWithBalancesQuery(AccountType: "Cash", ActiveOnly: true), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value.Select(a => new
        {
            a.Id,
            a.Name,
            a.Currency,
            a.AccountType,
            a.AccountNumber,
            a.Balance,
            a.LastChangeAmount,
            a.LastChangeDirection,
            CurrencySymbol = GetCurrencySymbol(a.Currency)
        }));
    }

    [HttpGet]
    public async Task<JsonResult> GetBankAccounts(CancellationToken cancellationToken)
    {
        Result<List<AccountWithBalanceResponse>> result = await getAccountsWithBalancesHandler.Handle(
            new GetAccountsWithBalancesQuery(AccountType: "Bank", ActiveOnly: true), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value.Select(a => new
        {
            a.Id,
            a.Name,
            a.Currency,
            a.AccountType,
            a.AccountNumber,
            a.Balance,
            a.LastChangeAmount,
            a.LastChangeDirection,
            CurrencySymbol = GetCurrencySymbol(a.Currency)
        }));
    }

    [HttpGet]
    public async Task<JsonResult> GetAll(CancellationToken cancellationToken)
    {
        Result<List<AccountWithBalanceResponse>> result = await getAccountsWithBalancesHandler.Handle(
            new GetAccountsWithBalancesQuery(AccountType: null, ActiveOnly: true), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new
        {
            items = result.Value.Select(a => new
            {
                a.Id,
                a.Name,
                a.AccountType,
                a.Currency
            })
        });
    }

    [HttpGet]
    public async Task<JsonResult> GetRecentTransactions(CancellationToken cancellationToken)
    {
        Result<List<RecentTransactionResponse>> result = await getRecentTransactionsHandler.Handle(
            new GetRecentTransactionsQuery(20), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value.Select(t => new
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
        }));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateBankAccount([FromBody] CreateBankAccountModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "الحسابات المالية"));
        }

        var command = new CreateFinancialAccountCommand(
            model.Name,
            model.Currency,
            model.AccountNumber,
            model.Notes,
            model.OpeningBalance);

        Result<Guid> result = await createFinancialAccountHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "الحسابات المالية"));
        }

        return Json(ToastResult.SuccessResult("تم إنشاء الحساب البنكي بنجاح", "الحسابات المالية", "", "refreshAccountsPage"));
    }

    [HttpGet]
    public async Task<JsonResult> GetAccountBalance(Guid id, CancellationToken cancellationToken)
    {
        Result<AccountBalanceResponse> result = await getAccountBalanceHandler.Handle(
            new GetAccountBalanceQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new
        {
            success = true,
            result.Value.Id,
            result.Value.Name,
            result.Value.Currency,
            result.Value.AccountType,
            result.Value.CurrentBalance,
            CurrencySymbol = GetCurrencySymbol(result.Value.Currency)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SetBalanceAjax([FromBody] SetBalanceModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "الحسابات المالية"));
        }

        var command = new SetAccountBalanceCommand(model.AccountId, model.TargetBalance, model.Notes);

        Result<Updated> result = await setAccountBalanceHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "الحسابات المالية"));
        }

        return Json(ToastResult.SuccessResult("تم ضبط الرصيد بنجاح", "الحسابات المالية", "", "refreshAccountsPage"));
    }

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "JOD" => "د.إ",
        "USD" => "$",
        "ILS" => "₪",
        _ => currency
    };
}
