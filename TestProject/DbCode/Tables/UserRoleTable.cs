using Qz.Infra.Database.Table;
using System.Data;

namespace TestProject.DbCode.Tables
{
    internal static class UserRoleTable
    {
        [DbTableName]
        internal const string TableName = "UserRole";

        [DbColumnString]
        internal const string UserId = "id";

        [DbColumnString]
        internal const string TenantId = "tenantId";

        [DbColumn(Type = DbType.Int32)]
        internal const string Role = "role";
    }
}
