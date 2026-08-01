using Application.Abstractions.Messaging;
using Application.SupplierDeliveries.Create;
using Application.Suppliers;
using Application.Suppliers.GetAll;
using Application.Suppliers.GetById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.SupplierDelivery;

namespace WebUI.Controllers;

[Authorize]
public class SupplierDeliveriesController(
    IQueryHandler<GetSuppliersQuery, List<SupplierResponse>> getSuppliersHandler,
    IQueryHandler<GetSupplierByIdQuery, SupplierDetailResponse> getSupplierByIdHandler,
    ICommandHandler<CreateSupplierDeliveryCommand, string> createDeliveryHandler)
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
            manufacturingBalance = detail.ManufacturingBalance
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateDeliveryAjax([FromBody] CreateSupplierDeliveryModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "توريد المورد"));
        }

         var command = new CreateSupplierDeliveryCommand(
            model.SupplierId,
            model.Lines.Select(l => new DeliveryLineDto(l.Karat, l.WeightInGrams)).ToList(),
            model.ManufacturingFeePerGram,
            model.ManufacturingFeeCurrency,
            model.Notes);

        Result<string> result = await createDeliveryHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "توريد المورد"));
        }

        return Json(ToastResult.SuccessResult("تم تسجيل التوريد بنجاح", "توريد المورد", "", "refreshDeliveryPage"));
    }
}
