using CoddLoom.Table.Base;
using System;

namespace CoddLoom.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbColumnBinaryAttribute : DbColumnBaseAttribute
{
    public int Length { get; set; } = 500;
}