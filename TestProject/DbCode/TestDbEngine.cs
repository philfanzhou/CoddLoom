using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
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
            var sql = Executor.SqlBuilder.Select(TenantTable.TableName);
            var tenants = Executor.Select(conn,
                sql, record => record[TenantTable.Id].ToString());
            return tenants.ToList();
        }

        public void DeleteTenant(IDbConnection conn, string tenant)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                return;
            }

            var whereParamItem = new WhereParamsItem(TenantTable.Id, tenant);
            var sql = Executor.SqlBuilder.Delete(TenantTable.TableName, new WhereConditions(whereParamItem));
            Executor.Execute(conn, sql, null, new WhereParams(whereParamItem));
        }

        public void DeleteUser(string unionId)
        {
            var whereParams = new WhereParams(PasswordUserTable.UnionId, unionId);
            Executor.Transaction(transaction =>
            {
                Delete(new SqlBuilderDeleteParam(PasswordUserTable.TableName, whereParams), transaction);
                Delete(new SqlBuilderDeleteParam(UserTable.TableName, whereParams), transaction);
            });
        }

        #endregion
    }
}
