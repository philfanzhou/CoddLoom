using CoddLoom.Table.Base;
using System;

namespace CoddLoom.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbColumnDecimalAttribute : DbColumnBaseAttribute
{
    public int Length { get; set; } = 18;

    public int PointLength { get; set; } = 2;
}