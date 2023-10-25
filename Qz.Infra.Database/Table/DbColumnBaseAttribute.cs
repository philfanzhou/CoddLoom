using System;
using System.Data;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class DbColumnBaseAttribute : Attribute
{
    public DbType Type { get; set; }

    public int Length { get; set; } = 50;

    public int PointLength { get; set; }

    public bool FixedLength { get; set; }

    public bool AllowUnicode { get; set; }

    public string Name { get; internal set; }
}