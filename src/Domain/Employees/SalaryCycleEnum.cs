namespace Domain.Employees;

public enum SalaryCycleEnum
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}
public static class SalaryCycleEnumExtensions
{
    public static string ToFriendlyString(this SalaryCycleEnum salaryCycle)
    {
        return salaryCycle switch
        {
            SalaryCycleEnum.Daily => "يومي",
            SalaryCycleEnum.Weekly => "اسبوعي",
            SalaryCycleEnum.Monthly => "شهري",
            _ => throw new ArgumentOutOfRangeException(nameof(salaryCycle), salaryCycle, null)
        };
    }
}
