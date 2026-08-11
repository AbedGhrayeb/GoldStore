using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Employees;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.PaySalary;

internal sealed class PaySalaryCommandHandler(IApplicationDbContext context)
    : ICommandHandler<PaySalaryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PaySalaryCommand command, CancellationToken cancellationToken)
    {
        Employee? employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return SalaryPaymentErrors.EmployeeNotFound(command.EmployeeId);
        }

        if (!employee.IsActive)
        {
            return SalaryPaymentErrors.EmployeeInactive;
        }

        if (employee.SalaryCycle == SalaryCycleEnum.Daily)
        {
            return SalaryPaymentErrors.DailyCycleNotSupported;
        }

        if (employee.Salary is not > 0m)
        {
            return SalaryPaymentErrors.SalaryNotSet;
        }

        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);
        if (account is null)
        {
            return SalaryPaymentErrors.AccountNotFound(command.AccountId);
        }

        if (!account.IsActive)
        {
            return SalaryPaymentErrors.AccountInactive;
        }

        if (account.Currency != employee.Currency)
        {
            return SalaryPaymentErrors.AccountCurrencyMismatch;
        }

        DateOnly scheduledDate = SalaryPayment.GetScheduledDate(employee.SalaryCycle, command.PaymentDate);

        List<SalaryPayment> periodPayments = await context.SalaryPayments
            .Where(p => p.EmployeeId == command.EmployeeId && p.ScheduledDate == scheduledDate)
            .ToListAsync(cancellationToken);

        decimal salaryAmount;
        decimal discountAmount;
        if (periodPayments.Count > 0)
        {
            salaryAmount = periodPayments[0].SalaryAmount;
            discountAmount = periodPayments[0].DiscountAmount;
        }
        else
        {
            int dayDifference = Math.Abs(command.PaymentDate.DayNumber - scheduledDate.DayNumber);
            salaryAmount = employee.Salary.Value;
            discountAmount = SalaryPayment.CalculateDiscountAmount(salaryAmount, dayDifference);
        }

        decimal periodNet = salaryAmount - discountAmount;
        decimal paidSoFar = periodPayments.Sum(p => p.Amount);
        decimal remaining = periodNet - paidSoFar;

        if (remaining <= 0)
        {
            return SalaryPaymentErrors.PeriodFullyPaid;
        }

        if (command.Amount > remaining)
        {
            return SalaryPaymentErrors.AmountExceedsRemaining;
        }

        decimal availableBalance = await context.GetAccountBalanceAsync(account.Id, cancellationToken);

        if (command.Amount > availableBalance)
        {
            return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
        }

        Result<SalaryPayment> salaryPaymentResult = SalaryPayment.Create(
            employee.Id,
            account.Id,
            account.Currency,
            salaryAmount,
            discountAmount,
            command.Amount,
            command.PaymentDate,
            scheduledDate,
            command.Notes);

        if (salaryPaymentResult.IsError)
        {
            return salaryPaymentResult.Errors;
        }

        context.SalaryPayments.Add(salaryPaymentResult.Value);

        Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(
            account.Id,
            account.Currency,
            salaryPaymentResult.Value.Amount,
            FinancialTransactionType.Outflow,
            FinancialReferenceType.SalaryPayment,
            salaryPaymentResult.Value.Id,
            command.Notes);

        if (financialTransactionResult.IsError)
        {
            return financialTransactionResult.Errors;
        }

        context.FinancialTransactions.Add(financialTransactionResult.Value);
        await context.SaveChangesAsync(cancellationToken);

        return salaryPaymentResult.Value.Id;
    }
}
