using Domain.Catalog;
using Domain.Finance;
using Domain.Inventory;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }
    DbSet<FinancialTransaction> FinancialTransactions { get; }
    DbSet<FinancialAccount> FinancialAccounts { get; }
    DbSet<Category> Categories { get; }
    DbSet<GoldLedgerEntry> GoldLedgerEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
