using CoddLoom.Table.Base;
using System;
using System.Data;

namespace CoddLoom.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbPrimaryKeyAttribute : DbPrimaryKeyBaseAttribute, INormalColumn
{
    public DbType Type { get; set; }
}