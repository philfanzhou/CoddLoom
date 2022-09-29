using QuantumZhou.Infrastructure.Data.Database;
using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TestProject.DbCode.Tables;

namespace TestProject.DbCode
{
    public class TestDbEngine : DbEngine
    {
        public TestDbEngine(DbExecutor executor)
            : base(executor, new List<TableDefine>
            {
                new(typeof(PasswordUserTable)),
                new(typeof(TenantTable)),
                new(typeof(UserRoleTable)),
                new(typeof(UserTable))
            })
        {
        }

        #region Tenant

        public IEnumerable<string> GetAllTenant(IDbConnection conn)
        {
            var sql = SqlBuilder.Select(TenantTable.TableName);
            var tenants = Executor.Select(conn, 
                record => record[TenantTable.Id].ToString(),
                sql);
            return tenants.ToList();
        }

        public void DeleteTenant(IDbConnection conn, string tenant)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                return;
            }

            var whereParamItem = new WhereParamsItem(TenantTable.Id, tenant);
            var sql = SqlBuilder.Delete(TenantTable.TableName, new WhereConditions(whereParamItem));
            Executor.Delete(conn, sql, new WhereParams(whereParamItem));
        }

        public void DeleteUser(string unionId)
        {
            var whereParams = new WhereParams(PasswordUserTable.UnionId, unionId);
            Transaction(conn =>
            {
                Delete(new SqlBuilderDeleteParam(PasswordUserTable.TableName, whereParams), conn);
                Delete(new SqlBuilderDeleteParam(UserTable.TableName, whereParams), conn);
            });
        }

        #endregion
    }
}
