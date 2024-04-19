using Qz.Infra.Database.Table;
using System.Data;

namespace TestProject.DbCode.Tables
{
    internal static class PasswordUserTable
    {
        [DbTableName]
        internal const string TableName = "PasswordUser";

        [DbPrimaryKeyString]
        internal const string UnionId = "unionId";

        [DbColumnString]
        internal const string Password = "password";
    }
}
