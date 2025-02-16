using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
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
                //new(typeof(CodeFirstEntity)),
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
            var tenants = Executor.Execute(
                sql, record => record[TenantTable.Id].ToString(), null, conn);
            return tenants.ToList();
        }

        public void DeleteTenant(IDbConnection conn, string tenant)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                return;
            }

            var where = new WhereConditions();
            where.Add(TenantTable.Id, tenant);
            var sql = Executor.SqlBuilder.Delete(TenantTable.TableName, where);
            Executor.Execute(sql, where.Parameters, null, conn);
        }

        public void DeleteUser(string unionId)
        {
            var where = new WhereConditions();
            where.Add(PasswordUserTable.UnionId, unionId);
            Executor.Transaction(tran =>
            {
                Delete(PasswordUserTable.TableName, where, null, tran);
                Delete(UserTable.TableName, where, null, tran);
            });
        }

        #endregion
    }
}
