using SharedKernel;

namespace Infrastructure.Time;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now.ToLocalTime();
}
