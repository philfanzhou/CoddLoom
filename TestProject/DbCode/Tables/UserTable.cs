using System.Data;
using QuantumZhou.Infrastructure.Data.Database.Table;

namespace TestProject.DbCode.Tables
{
    internal static class UserTable
    {
        [DbTableName]
        internal const string TableName = "User";

        [DbPrimaryKey(Type = DbType.String)]
        internal const string Id = "id";

        [DbColumn(Type = DbType.String, AllowEmpty = false)]
        internal const string UnionId = "unionId";
    }
}
