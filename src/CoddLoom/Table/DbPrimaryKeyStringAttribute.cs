using CoddLoom.Table.Base;
using System;

namespace CoddLoom.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbPrimaryKeyStringAttribute : DbPrimaryKeyBaseAttribute, IStringColumn
{
    public int Length { get; set; } = 50;

    public bool FixedLength { get; set; }

    public bool AllowUnicode { get; set; }
}