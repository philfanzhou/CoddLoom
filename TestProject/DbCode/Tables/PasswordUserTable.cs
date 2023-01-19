using Qz.Infra.Database.Table;
using System.Data;

namespace TestProject.DbCode.Tables
{
    internal static class PasswordUserTable
    {
        [DbTableName]
        internal const string TableName = "PasswordUser";

        [DbPrimaryKey(Type = DbType.String)]
        internal const string UnionId = "unionId";

        [DbColumn(Type = DbType.String)]
        internal const string Password = "password";
    }
}
