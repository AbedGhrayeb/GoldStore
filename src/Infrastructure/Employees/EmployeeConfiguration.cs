using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Employees;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(category => category.FirstName).HasMaxLength(20);
        builder.Property(category => category.LastName).HasMaxLength(20);
        builder.Property(category => category.Title).HasMaxLength(50);
        builder.Property(i => i.Salary).HasPrecision(18, 3);

    }
}
