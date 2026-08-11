using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Employees;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.Create;

internal sealed class CreateEmployeeCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ICurrentTenant currentTenant)
    : ICommandHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        Guid? userId = null;

        if (command.ConnectToUser)
        {
            if (command.ExistingUserId.HasValue)
            {
                bool userExists = await context.Users.AnyAsync(u => u.Id == command.ExistingUserId.Value, cancellationToken);
                if (!userExists)
                {
                    return EmployeeErrors.UserNotFound(command.ExistingUserId.Value);
                }

                bool alreadyLinked = await context.Employees.AnyAsync(e => e.UserId == command.ExistingUserId.Value, cancellationToken);
                if (alreadyLinked)
                {
                    return EmployeeErrors.UserAlreadyLinked(command.ExistingUserId.Value);
                }

                userId = command.ExistingUserId.Value;
            }
            else
            {
                Result<Guid> createUserResult = await CreateUserAsync(command, cancellationToken);
                if (createUserResult.IsError)
                {
                    return createUserResult.Errors;
                }

                userId = createUserResult.Value;
            }
        }

        Result<Employee> employeeResult = Employee.Create(
            command.FirstName,
            command.LastName,
            command.Role,
            command.Salary,
            command.Currency,
            command.SalaryCycle,
            userId);

        if (employeeResult.IsError)
        {
            return employeeResult.Errors;
        }

        context.Employees.Add(employeeResult.Value);
        await context.SaveChangesAsync(cancellationToken);

        return employeeResult.Value.Id;
    }

    private async Task<Result<Guid>> CreateUserAsync(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        // Email addresses are unique system-wide, so the uniqueness check must
        // bypass the tenant query filter.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == command.NewUserEmail, cancellationToken))
        {
            return UserErrors.EmailNotUnique;
        }

        Result<User> userResult = User.Create(
            Guid.CreateVersion7(),
            currentTenant.TenantId,
            command.NewUserEmail!,
            command.FirstName,
            command.LastName,
            passwordHasher.Hash(command.NewUserPassword!));

        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        context.Users.Add(userResult.Value);

        return userResult.Value.Id;
    }
}
