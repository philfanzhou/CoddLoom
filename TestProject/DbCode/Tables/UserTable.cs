using Qz.Infra.Database.Table;
using System.Data;

namespace TestProject.DbCode.Tables
{
    internal static class UserTable
    {
        [DbTableName]
        internal const string TableName = "UserTable";

        [DbPrimaryKey(Type = DbType.Int32)]
        internal const string Id = "id";

        [DbColumn(Type = DbType.String, AllowEmpty = false)]
        internal const string UnionId = "unionId";
    }
}
