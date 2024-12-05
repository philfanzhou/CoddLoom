using Qz.Infra.Database.Table.Base;
using System;
using System.Data;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbPrimaryKeyAttribute : DbPrimaryKeyBaseAttribute, INormalColumn
{
    public DbType Type { get; set; }
}