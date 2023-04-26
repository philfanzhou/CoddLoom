using System;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbPrimaryKeyAttribute : DbColumnBaseAttribute
{
    public bool IsIdentity { get; set; }
}