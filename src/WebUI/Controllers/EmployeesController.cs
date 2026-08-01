using Application.Abstractions.Messaging;
using Application.Employees;
using Application.Employees.Create;
using Application.Employees.GetAll;
using Application.Employees.GetById;
using Application.Employees.GetUnlinkedUsers;
using Application.Employees.ToggleActive;
using Application.Employees.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.Employee;

namespace WebUI.Controllers;

[Authorize]
public class EmployeesController(
    IQueryHandler<GetEmployeesQuery, List<EmployeeResponse>> getEmployeesHandler,
    IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse> getEmployeeByIdHandler,
    IQueryHandler<GetUnlinkedUsersQuery, List<EmployeeUserOptionResponse>> getUnlinkedUsersHandler,
    ICommandHandler<CreateEmployeeCommand, Guid> createEmployeeHandler,
    ICommandHandler<UpdateEmployeeCommand, Updated> updateEmployeeHandler,
    ICommandHandler<ToggleActiveEmployeeCommand, Updated> toggleActiveHandler) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> List(CancellationToken cancellationToken)
    {
        Result<List<EmployeeResponse>> result = await getEmployeesHandler.Handle(new GetEmployeesQuery(), cancellationToken);

        return Json(result.IsSuccess ? result.Value : []);
    }

    [HttpGet]
    public async Task<IActionResult> AddOrUpdate(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            Result<List<EmployeeUserOptionResponse>> usersResult = await getUnlinkedUsersHandler.Handle(new GetUnlinkedUsersQuery(), cancellationToken);
            ViewBag.UnlinkedUsers = usersResult.IsSuccess ? usersResult.Value : [];

            return PartialView("Partials/_CreateModal", new CreateEmployeeModel());
        }

        Result<EmployeeResponse> result = await getEmployeeByIdHandler.Handle(new GetEmployeeByIdQuery(id.Value), cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموظفين"));
        }

        EmployeeResponse employee = result.Value;
        var model = new EditEmployeeModel
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Role = employee.Role,
            Salary = employee.Salary,
            SalaryCycle = employee.SalaryCycle,
            IsActive = employee.IsActive,
            UserId = employee.UserId,
            UserEmail = employee.UserEmail
        };

        return PartialView("Partials/_EditModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddOrUpdateAjax(CreateEmployeeModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "إدارة الموظفين"));
        }

        Result<Guid> result = await createEmployeeHandler.Handle(
            new CreateEmployeeCommand(
                model.FirstName,
                model.LastName,
                model.Role!.Value,
                model.Salary!.Value,
                model.SalaryCycle!.Value,
                model.ConnectToUser,
                model.ExistingUserId,
                model.NewUserEmail,
                model.NewUserPassword),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموظفين"));
        }

        return Json(ToastResult.SuccessResult("تم إضافة الموظف بنجاح", "إدارة الموظفين", "", "refreshEmployeeTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditEmployeeModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>());
            return Json(ToastResult.ExistResult(
                string.Join(" • ", errors.SelectMany(e => e.Value)),
                "إدارة الموظفين"));
        }

        Result<Updated> result = await updateEmployeeHandler.Handle(
            new UpdateEmployeeCommand(
                model.Id,
                model.FirstName,
                model.LastName,
                model.Role!.Value,
                model.Salary!.Value,
                model.SalaryCycle!.Value,
                model.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموظفين"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث الموظف بنجاح", "إدارة الموظفين", "", "refreshEmployeeTable"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        Result<Updated> result = await toggleActiveHandler.Handle(new ToggleActiveEmployeeCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموظفين"));
        }

        return Json(ToastResult.SuccessResult("تم تحديث حالة الموظف بنجاح", "إدارة الموظفين", "", "refreshEmployeeTable"));
    }
}
