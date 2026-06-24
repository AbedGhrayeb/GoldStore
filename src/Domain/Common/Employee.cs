using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Common;

public enum Employee
{
    Tareq=1,
    Ramzi=2,
    Yazan=3
}
public static class EmployeeExtention
{
    public static string GetEmployeeName(int employee) => employee switch
    {
    1 =>"طارق",
    2 => "رمزي",
    3 => "يزن",
    _ => "" 
};
}
