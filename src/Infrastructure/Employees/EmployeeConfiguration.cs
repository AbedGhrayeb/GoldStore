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
        builder.Property(category => category.Role).HasConversion<string>().IsRequired();
        builder.Property(i => i.Salary).HasPrecision(18, 3);
        builder.Property(i => i.Currency).HasConversion<string>().HasMaxLength(3).IsRequired();

        builder.Property(i => i.SalaryCycle).HasConversion<int>().IsRequired();
        builder.Property(i => i.UserId).IsRequired(false);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}
