using Application.Abstractions.Messaging;
using Application.Finance.Accounts;
using Application.Finance.Accounts.GetAll;
using Application.SupplierPayments.Manufacturing.Create;
using Application.SupplierPayments.ScrapGold.Create;
using Application.Suppliers;
using Application.Suppliers.GetAll;
using Application.Suppliers.GetById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.SupplierDelivery;
using WebUI.Models.SupplierPayment;

namespace WebUI.Controllers;

[Authorize]
public class SupplierPaymentsController(
    IQueryHandler<GetSuppliersQuery, List<SupplierResponse>> getSuppliersHandler,
    IQueryHandler<GetSupplierByIdQuery, SupplierDetailResponse> getSupplierByIdHandler,
    IQueryHandler<GetFinancialAccountsQuery, List<FinancialAccountResponse>> getFinancialAccountsHandler,
    ICommandHandler<CreateSupplierScrapGoldPaymentCommand, Guid> createScrapGoldPaymentHandler,
    ICommandHandler<CreateSupplierManufacturingPaymentCommand, Guid> createManufacturingPaymentHandler)
    : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetSuppliers(CancellationToken cancellationToken)
    {
        Result<List<SupplierResponse>> result = await getSuppliersHandler.Handle(new GetSuppliersQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        var activeSuppliers = result.Value
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name, s.PrimaryPhone, s.GoldBalance, s.ManufacturingBalance })
            .ToList();

        return Json(activeSuppliers);
    }

    [HttpGet]
    public async Task<JsonResult> GetSupplierBalances(Guid id, CancellationToken cancellationToken)
    {
        Result<SupplierDetailResponse> result = await getSupplierByIdHandler.Handle(new GetSupplierByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        SupplierDetailResponse detail = result.Value;
        return Json(new
        {
            success = true,
            name = detail.Name,
            primaryPhone = detail.PrimaryPhone,
            goldBalance = detail.GoldBalance,
            manufacturingBalance = detail.ManufacturingBalance,
            recentTransactions = detail.RecentTransactions.Select(t => new
            {
                id = t.Id,
                description = t.Description,
                date = t.Date,
                type = t.Type,
                amount = t.Amount,
                unit = t.Unit,
                direction = t.Direction
            })
        });
    }

    [HttpGet]
    public JsonResult GetKarats()
    {
        var karats = Domain.Common.SupportedValues.Karats
            .Select(k => new KaratOption { Value = (int)k, Label = $"{(int)k} قيراط" })
            .ToList();
        return Json(karats);
    }

    [HttpGet]
    public JsonResult GetCurrencies()
    {
        var currencies = Domain.Common.SupportedValues.Currencies
            .Select(c => new CurrencyOption
            {
                Value = c.ToString(),
                Label = c switch
                {
                    Domain.Common.Currency.JOD => "دينار أردني (د.إ)",
                    Domain.Common.Currency.USD => "دولار أمريكي ($)",
                    Domain.Common.Currency.ILS => "شيكل إسرائيلي (₪)",
                    _ => c.ToString()
                }
            })
            .ToList();
        return Json(currencies);
    }

    [HttpGet]
    public async Task<JsonResult> GetFinancialAccounts(CancellationToken cancellationToken)
    {
        Result<List<FinancialAccountResponse>> result = await getFinancialAccountsHandler.Handle(new GetFinancialAccountsQuery(true), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        var accounts = result.Value.Select(a => new
        {
            a.Id,
            a.Name,
            a.Currency,
            a.AccountType,
            DisplayLabel = a.AccountNumber != null ? $"{a.Name} ({a.AccountNumber})" : a.Name
        }).ToList();

        return Json(accounts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateScrapGoldPayment([FromBody] CreateScrapGoldPaymentModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "دفعات المورد"));
        }

        var command = new CreateSupplierScrapGoldPaymentCommand(
            model.SupplierId,
            model.Karat,
            model.WeightInGrams,
            model.Notes);

        Result<Guid> result = await createScrapGoldPaymentHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "دفعات المورد"));
        }

        return Json(ToastResult.SuccessResult("تم تسجيل دفعة الذهب بنجاح", "دفعات المورد", "", "refreshPaymentPage"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateManufacturingPayment([FromBody] CreateManufacturingPaymentModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "دفعات المورد"));
        }

        var command = new CreateSupplierManufacturingPaymentCommand(
            model.SupplierId,
            model.AccountId,
            model.Amount,
            model.Currency,
            model.Notes);

        Result<Guid> result = await createManufacturingPaymentHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "دفعات المورد"));
        }

        return Json(ToastResult.SuccessResult("تم تسجيل دفعة الأجور بنجاح", "دفعات المورد", "", "refreshPaymentPage"));
    }
}
