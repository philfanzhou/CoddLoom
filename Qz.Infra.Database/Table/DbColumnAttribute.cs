using System;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbColumnAttribute : DbColumnBaseAttribute
{
    public virtual bool AllowEmpty { get; set; }
}