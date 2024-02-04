using Qz.Infra.Database.Table.Base;
using System;

namespace Qz.Infra.Database.Table;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DbPrimaryKeyStringAttribute : DbPrimaryKeyBaseAttribute, IStringColumn
{
    public int Length { get; set; } = 50;

    public bool FixedLength { get; set; }

    public bool AllowUnicode { get; set; }
}