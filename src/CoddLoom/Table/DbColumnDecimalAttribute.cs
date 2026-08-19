using Qz.Infra.Database.Table.Base;
using System;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbColumnDecimalAttribute : DbColumnBaseAttribute
{
    public int Length { get; set; } = 18;

    public int PointLength { get; set; } = 2;
}