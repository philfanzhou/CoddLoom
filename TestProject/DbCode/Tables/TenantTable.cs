using System.Data;
using QuantumZhou.Infrastructure.Data.Database.Table;

namespace TestProject.DbCode.Tables
{
    internal static class TenantTable
    {
        [DbTableName]
        internal const string TableName = "Tenant";

        [DbPrimaryKey(Type = DbType.String)]
        internal const string Id = "id";
    }
}