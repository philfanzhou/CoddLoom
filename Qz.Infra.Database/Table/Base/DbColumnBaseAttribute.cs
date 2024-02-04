using System;

namespace Qz.Infra.Database.Table.Base;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class DbColumnBaseAttribute : DbBaseAttribute
{
    public bool AllowEmpty { get; set; }
}