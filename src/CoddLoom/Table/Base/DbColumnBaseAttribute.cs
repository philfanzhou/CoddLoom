using System;

namespace CoddLoom.Table.Base;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class DbColumnBaseAttribute : DbBaseAttribute
{
    public bool AllowEmpty { get; set; }
}