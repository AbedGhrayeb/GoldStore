using SharedKernel.Result;

namespace Domain.Employee;

public static class EmployeeErrors
{
    public static Error IdRequired => Error.Validation(
    "Employee.Id.Required",
    $"The Employee Id is Required");
    public static Error FirstNameRequired => Error.Validation(
        "Employee.FirstName.Required",
        $"The Employee FirstName is Required");
    public static Error LastNameRequired => Error.Validation(
        "Employee.LastName.Required",
        $"The Employee LastName is Required");
    public static Error NotFound(Guid employeeId) => Error.NotFound(
        "Employee.NotFound",
        $"The Employee with the Id = '{employeeId}' was not found");
}
