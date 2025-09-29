using Qz.Infra.Database.Table;
using System.Data;

namespace TestProject.DbCode.Tables;

internal static class BatchRecordTable
{
    [DbTableName]
    internal const string TableName = "BatchRecord";

    [DbPrimaryKey(Type = DbType.Int32)]
    internal const string Id = "Id";

    [DbColumnString]
    internal const string Name = "Name";
}
