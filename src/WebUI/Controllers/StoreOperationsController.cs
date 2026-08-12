using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.GetDetail;
using Application.Features.StoreOperations.GetEmployees;
using Application.Features.StoreOperations.GetKpis;
using Application.Features.StoreOperations.GetPaged;
using Application.Features.StoreOperations.GetTodayEmployeeStats;
using Application.Features.StoreOperations.Shared;
using Domain.Tenants;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;

namespace WebUI.Controllers;

[Authorize]
[RequireFeature(Features.Reports)]
public class StoreOperationsController(
    IQueryHandler<GetStoreOperationsQuery, PagedStoreOperationsResponse> getPagedHandler,
    IQueryHandler<GetStoreOperationsKpisQuery, StoreOperationsKpiResponse> getKpisHandler,
    IQueryHandler<GetStoreEmployeesQuery, List<EmployeeResponse>> getEmployeesHandler,
    IQueryHandler<GetStoreOperationDetailQuery, StoreOperationDetailResponse> getDetailHandler,
    IQueryHandler<GetTodayEmployeeStatsQuery, List<EmployeeDayStatsResponse>> getTodayEmployeeStatsHandler) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? operationType = null,
        Guid? employeeId = null,
        Guid? accountId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedStoreOperationsResponse> result = await getPagedHandler.Handle(
            new GetStoreOperationsQuery(page, pageSize, fromDate, toDate, operationType, employeeId, accountId, search),
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
        Result<StoreOperationsKpiResponse> result = await getKpisHandler.Handle(
            new GetStoreOperationsKpisQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetTodayEmployeeStats(CancellationToken cancellationToken)
    {
        Result<List<EmployeeDayStatsResponse>> result = await getTodayEmployeeStatsHandler.Handle(
            new GetTodayEmployeeStatsQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetEmployees(CancellationToken cancellationToken)
    {
        Result<List<EmployeeResponse>> result = await getEmployeesHandler.Handle(
            new GetStoreEmployeesQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetDetail(Guid id, string operationType, CancellationToken cancellationToken)
    {
        Result<StoreOperationDetailResponse> result = await getDetailHandler.Handle(
            new GetStoreOperationDetailQuery(id, operationType), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(result.Value);
    }
}
