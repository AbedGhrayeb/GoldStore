using Application.Abstractions.Messaging;
using Application.Employees;
using Application.Employees.Create;
using Application.Employees.GetAll;
using Application.Employees.GetById;
using Application.Employees.GetSalaryPeriodSummary;
using Application.Employees.GetUnlinkedUsers;
using Application.Employees.PaySalary;
using Application.Employees.ToggleActive;
using Application.Employees.Update;
using Application.Finance.Accounts;
using Application.Finance.Accounts.GetWithBalance;
using Domain.Employees;
using Domain.Tenants;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using WebUI.Models;
using WebUI.Models.Employee;
namespace WebUI.Controllers;
[Authorize]
[RequireFeature(Features.Hr)]
public class EmployeesController(
    IQueryHandler<GetEmployeesQuery, List<EmployeeResponse>> getEmployeesHandler,
    IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse> getEmployeeByIdHandler,
    IQueryHandler<GetUnlinkedUsersQuery, List<EmployeeUserOptionResponse>> getUnlinkedUsersHandler,
    IQueryHandler<GetAccountsWithBalancesQuery, List<AccountWithBalanceResponse>> getAccountsWithBalancesHandler,
    IQueryHandler<GetSalaryPeriodSummaryQuery, SalaryPeriodSummaryResponse> getSalaryPeriodSummaryHandler,
    ICommandHandler<CreateEmployeeCommand, Guid> createEmployeeHandler,
    ICommandHandler<UpdateEmployeeCommand, Updated> updateEmployeeHandler,
    ICommandHandler<ToggleActiveEmployeeCommand, Updated> toggleActiveHandler,
    ICommandHandler<PaySalaryCommand, Guid> paySalaryHandler) : Controller
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
            Currency = employee.Currency,
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
                model.Currency!.Value,
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
                model.Currency!.Value,
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

    [HttpGet]
    public async Task<IActionResult> PaySalary(Guid id, CancellationToken cancellationToken)
    {
        Result<EmployeeResponse> employeeResult = await getEmployeeByIdHandler.Handle(new GetEmployeeByIdQuery(id), cancellationToken);
        if (!employeeResult.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(employeeResult.TopError.Description, "إدارة الموظفين"));
        }

        EmployeeResponse employee = employeeResult.Value;
        if (employee.SalaryCycle == SalaryCycleEnum.Daily)
        {
            return Json(ToastResult.ErrorResult("دفع الراتب اليومي غير مدعوم", "إدارة الموظفين"));
        }

        if (!employee.IsActive)
        {
            return Json(ToastResult.ErrorResult("لا يمكن دفع راتب لموظف متوقف", "إدارة الموظفين"));
        }

        Result<List<AccountWithBalanceResponse>> accountsResult = await getAccountsWithBalancesHandler.Handle(
            new GetAccountsWithBalancesQuery(AccountType: null, ActiveOnly: true, Currency: employee.Currency), cancellationToken);

        DateOnly scheduledDate = SalaryPayment.GetNextScheduledDate(employee.SalaryCycle, DateOnly.FromDateTime(DateTime.Now));

        Result<SalaryPeriodSummaryResponse> summaryResult = await getSalaryPeriodSummaryHandler.Handle(
            new GetSalaryPeriodSummaryQuery(employee.Id, scheduledDate), cancellationToken);

        var model = new PaySalaryModel
        {
            EmployeeId = employee.Id,
            PaymentDate = scheduledDate,
            Amount = summaryResult.IsSuccess && summaryResult.Value.Remaining > 0
                ? summaryResult.Value.Remaining
                : null
        };

        ViewBag.Employee = employee;
        ViewBag.ScheduledDate = scheduledDate;
        ViewBag.Accounts = accountsResult.IsSuccess ? accountsResult.Value : [];
        ViewBag.Remaining = summaryResult.IsSuccess ? summaryResult.Value.Remaining : 0m;
        ViewBag.AlreadyPaid = summaryResult.IsSuccess ? summaryResult.Value.AlreadyPaid : 0m;
        ViewBag.NetAmount = summaryResult.IsSuccess ? summaryResult.Value.NetAmount : 0m;
        ViewBag.DiscountAmount = summaryResult.IsSuccess ? summaryResult.Value.DiscountAmount : 0m;

        return PartialView("Partials/_PaySalaryModal", model);
    }

    [HttpGet]
    public async Task<JsonResult> GetPayPeriodSummary(Guid employeeId, DateOnly paymentDate, CancellationToken cancellationToken)
    {
        Result<SalaryPeriodSummaryResponse> result = await getSalaryPeriodSummaryHandler.Handle(
            new GetSalaryPeriodSummaryQuery(employeeId, paymentDate), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.TopError.Description });
        }

        return Json(new { success = true, summary = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PaySalaryAjax(PaySalaryModel model, CancellationToken cancellationToken)
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

        Result<Guid> result = await paySalaryHandler.Handle(
            new PaySalaryCommand(model.EmployeeId, model.AccountId!.Value, model.Amount!.Value, model.PaymentDate!.Value, model.Notes),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(ToastResult.ErrorResult(result.TopError.Description, "إدارة الموظفين"));
        }

        return Json(ToastResult.SuccessResult("تم دفع الراتب بنجاح", "إدارة الموظفين", "", "refreshEmployeeTable"));
    }
}
