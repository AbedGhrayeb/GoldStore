using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.GetDetail;
using Application.Features.StoreOperations.GetEmployees;
using Application.Features.StoreOperations.GetKpis;
using Application.Features.StoreOperations.GetPaged;
using Application.Features.StoreOperations.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace WebUI.Controllers;

[Authorize]
public class StoreOperationsController(
    IQueryHandler<GetStoreOperationsQuery, PagedStoreOperationsResponse> getPagedHandler,
    IQueryHandler<GetStoreOperationsKpisQuery, StoreOperationsKpiResponse> getKpisHandler,
    IQueryHandler<GetStoreEmployeesQuery, List<string>> getEmployeesHandler,
    IQueryHandler<GetStoreOperationDetailQuery, StoreOperationDetailResponse> getDetailHandler) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<JsonResult> GetPaged(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? operationType = null,
        string? employeeName = null,
        Guid? accountId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        Result<PagedStoreOperationsResponse> result = await getPagedHandler.Handle(
            new GetStoreOperationsQuery(page, pageSize, fromDate, toDate, operationType, employeeName, accountId, search),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
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
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }

    [HttpGet]
    public async Task<JsonResult> GetEmployees(CancellationToken cancellationToken)
    {
        Result<List<string>> result = await getEmployeesHandler.Handle(
            new GetStoreEmployeesQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error.Description });
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
            return Json(new { success = false, error = result.Error.Description });
        }

        return Json(result.Value);
    }
}
