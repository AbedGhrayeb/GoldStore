using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Employees;
using Application.Employees.GetSalaryPayments;
using Domain.Tenants;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.Controllers;

[Authorize]
[RequireFeature(Features.Hr)]
public class SalaryPaymentsController(
    IQueryHandler<GetSalaryPaymentsQuery, PaginatedList<SalaryPaymentResponse>> getSalaryPaymentsHandler) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(int page = 1, int pageSize = 20, string? employeeName = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        Result<PaginatedList<SalaryPaymentResponse>> result = await getSalaryPaymentsHandler.Handle(
            new GetSalaryPaymentsQuery(page, pageSize, employeeName, fromDate, toDate),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }
}
