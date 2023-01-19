using System.Data;
using Qz.Infra.Database.Table;

namespace TestProject.DbCode.Tables
{
    internal static class UserRoleTable
    {
        [DbTableName]
        internal const string TableName = "UserRole";

        [DbColumn(Type = DbType.String)]
        internal const string UserId = "userId";

        [DbColumn(Type = DbType.String)]
        internal const string TenantId = "tenantId";

        [DbColumn(Type = DbType.Int32)]
        internal const string Role = "role";
    }
}
