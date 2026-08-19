using CoddLoom.Table;
using System.Data;

namespace CoddLoom.Tests.DbCode.Tables;

/// <summary>
/// Initial schema used to verify additive column migration.
/// </summary>
internal static class BasicTestTable
{
    [DbTableName]
    internal const string TableName = "TestColumnMigrationTable";

    [DbPrimaryKey(Type = DbType.String)]
    internal const string Id = "id";

    [DbColumnString(AllowEmpty = false)]
    internal const string Name = "name";

    [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
    public const string CreatedDate = "createdDate";
}
