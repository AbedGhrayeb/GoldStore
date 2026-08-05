using Application.Abstractions.Messaging;
using Application.Suppliers;
using Application.Suppliers.Create;
using Application.Suppliers.GetAll;
using Application.Suppliers.GetById;
using Application.Suppliers.ToggleActive;
using Application.Suppliers.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.Supplier;

namespace WebUI.Controllers;

[Authorize]
public class SuppliersController(
    IQueryHandler<GetSuppliersQuery, List<SupplierResponse>> getSuppliersHandler,
    IQueryHandler<GetSupplierByIdQuery, SupplierDetailResponse> getSupplierByIdHandler,
    ICommandHandler<CreateSupplierCommand, Guid> createSupplierHandler,
    ICommandHandler<UpdateSupplierCommand, Updated> updateSupplierHandler,
    ICommandHandler<ToggleActiveSupplierCommand, Updated> toggleActiveHandler) : BaseController
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Result<List<SupplierResponse>> result = await getSuppliersHandler.Handle(new GetSuppliersQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound();
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> AddOrUpdate(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return PartialView("Partials/_CreateModal", new CreateSupplierModel());
        }

        Result<SupplierDetailResponse> result = await getSupplierByIdHandler.Handle(new GetSupplierByIdQuery(id.Value), cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموردين"));
        }

        SupplierDetailResponse detail = result.Value;
        var model = new EditSupplierModel
        {
            Id = detail.Id,
            Name = detail.Name,
            PrimaryPhone = detail.PrimaryPhone,
            SecondaryPhone = detail.SecondaryPhone,
            BankAccountNumber = detail.BankAccountNumber ?? string.Empty,
            Notes = detail.Notes,
            IsActive = detail.IsActive
        };

        return PartialView("Partials/_EditModal", model);
    }

    [HttpGet]
    public async Task<JsonResult> GetById(Guid id, CancellationToken cancellationToken)
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
            id = detail.Id,
            name = detail.Name,
            primaryPhone = detail.PrimaryPhone,
            secondaryPhone = detail.SecondaryPhone,
            bankAccountNumber = detail.BankAccountNumber,
            notes = detail.Notes,
            isActive = detail.IsActive,
            createdAt = detail.CreatedAt,
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
            }),
            financialBalancesByCurrency = detail.FinancialBalancesByCurrency.Select(c => new
            {
                currency = c.Currency,
                balance = c.Balance
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddOrUpdateAjax(CreateSupplierModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "إدارة الموردين"));
        }

        Result<Guid> result = await createSupplierHandler.Handle(
            new CreateSupplierCommand(model.Name, model.PrimaryPhone, model.SecondaryPhone, model.BankAccountNumber, model.Notes),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموردين"));
        }

        return Json(ToastResult.SuccessResult("تم إضافة المورد بنجاح", "إدارة الموردين", "", "refreshSupplierTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditSupplierModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "إدارة الموردين"));
        }

        Result<Updated> result = await updateSupplierHandler.Handle(
            new UpdateSupplierCommand(model.Id, model.Name, model.PrimaryPhone, model.SecondaryPhone, model.BankAccountNumber, model.Notes, model.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموردين"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث المورد بنجاح", "إدارة الموردين", "", "refreshSupplierTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        Result<Updated> result = await toggleActiveHandler.Handle(new ToggleActiveSupplierCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموردين"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث حالة المورد بنجاح", "إدارة الموردين", "", "refreshSupplierTable"));
    }
}
