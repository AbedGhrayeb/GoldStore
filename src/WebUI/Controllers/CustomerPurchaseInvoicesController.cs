using Application.Abstractions.Messaging;
using Application.Features.CustomerPurchaseInvoices.Create;
using Application.Features.CustomerPurchaseInvoices.GetNextNumber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using WebUI.Models;
using WebUI.Models.CustomerPurchaseInvoice;

namespace WebUI.Controllers;

[Authorize]
public class CustomerPurchaseInvoicesController(
    IQueryHandler<GetNextCustomerPurchaseInvoiceNumberQuery, string> getNextNumberHandler,
    ICommandHandler<CreateCustomerPurchaseInvoiceCommand, Guid> createHandler) : BaseController
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetNextNumber(CancellationToken cancellationToken)
    {
        Result<string> result = await getNextNumberHandler.Handle(
            new GetNextCustomerPurchaseInvoiceNumberQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(new { invoiceNumber = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create([FromBody] CreateCustomerPurchaseInvoiceModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "مشتريات الذهب"));
        }

        var command = new CreateCustomerPurchaseInvoiceCommand(
            model.SellerName,
            model.SellerPhone,
            model.SellerIdNumber,
            model.SellerYearOfBirth,
            model.SellerAddress,
            model.BuyerName,
            model.Date,
            model.Currency,

            model.TotalAmount,
            model.AmountPaid,
            model.PaymentMethod,
            model.AccountId,
            model.SellerAccountNumber,
            model.Notes,
            model.Items.Select(i => new CustomerPurchaseInvoiceItemDto(
                i.CategoryId,
                i.Karat,
                i.WeightInGrams,
                i.PricePerGram)).ToList());

        Result<Guid> result = await createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.Error.Description, "مشتريات الذهب"));
        }

        return Json(ToastResult.SuccessResult("تم إصدار فاتورة شراء الذهب بنجاح", "مشتريات الذهب", "", "refreshCustomerPurchases"));
    }
}
