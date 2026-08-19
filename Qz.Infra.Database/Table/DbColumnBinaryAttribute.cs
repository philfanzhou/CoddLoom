using Qz.Infra.Database.Table.Base;
using System;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbColumnBinaryAttribute : DbColumnBaseAttribute
{
    public int Length { get; set; } = 500;
}