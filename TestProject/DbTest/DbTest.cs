using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.SqlServer;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Qz.Infra.Database.SQLite;
using TestProject.DbCode;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;
using System;

namespace TestProject.DbTest
{
    [TestClass]
    public class DbTest
    {
        [TestMethod]
        public void Run()
        {
            //var executor = new MySqlExecutor("192.168.50.85", "test", "root", "`12qweasd");
            var executor = new SQLiteExecutor(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "User.db");
            /*var executor =
                new SqlServerExecutor(
                    "Data Source=192.168.53.21;Database=TestDb;User ID=myuser;Password=qwe123!@;Connect Timeout=30");*/

            var dbEngine = new TestDbEngine(executor);
            DbContainer.Add(dbEngine);

            dbEngine = DbContainer.Get<TestDbEngine>();
            using var con = dbEngine.Executor.GetConnection();

            con.Open();

            InsertUpdateDelete(dbEngine, con);

            const int count = 100;
            InsertTwoTables(count, dbEngine, con);

            TestOrderBy(dbEngine, con);

            PageSelectTest(dbEngine, con);

            var pwsUserJoin = new JoinConditions(UserTable.TableName, UserTable.UnionId,
                PasswordUserTable.TableName, PasswordUserTable.UnionId);
            var sqlBuilderParam = new SqlBuilderSelectJoinParam(dbEngine.Executor.SqlBuilder, pwsUserJoin);
            var allPwdUser = dbEngine.Select<PasswordUser>(sqlBuilderParam, con).ToList();
            Assert.IsTrue(allPwdUser.Count() == count);

            DeleteTwoTables(allPwdUser, dbEngine, con, sqlBuilderParam);

            con.Close();
        }

        private static void InsertUpdateDelete(TestDbEngine dbEngine, IDbConnection con)
        {
            var tenantId = "TestTenant";
            var newTenant = new Tenant {Id = tenantId};
            dbEngine.Insert(newTenant, con);

            var allTenant = dbEngine.GetAllTenant(con).ToList();
            Assert.AreEqual(tenantId, allTenant[0].Trim());

            var anotherTenant = "another";
            var input = new InputValues();
            input.Add(TenantTable.Id, anotherTenant);
            var whereParams = new WhereParams(TenantTable.Id, tenantId);
            dbEngine.Update(new SqlBuilderUpdateParam(TenantTable.TableName, input, whereParams), con);
            allTenant = dbEngine.GetAllTenant(con).ToList();
            Assert.AreEqual(anotherTenant, allTenant[0].Trim());

            dbEngine.DeleteTenant(con, anotherTenant);
            allTenant = dbEngine.GetAllTenant(con).ToList();
            Assert.IsTrue(allTenant.Count == 0);
        }

        private static void InsertTwoTables(int count, TestDbEngine dbEngine, IDbConnection con)
        {
            for (var i = 0; i < count; i++)
            {
                var pwdUser = new PasswordUser
                {
                    Id = i.ToString(),
                    UnionId = (i * 5).ToString(),
                    Password = i.ToString(),
                    RegistrationDate = DateTime.Now
                };
                dbEngine.Insert(pwdUser, con);
                dbEngine.Insert<User>(pwdUser, con);
            }

            var allUser = dbEngine.Select<User>(new SqlBuilderSelectParam<User>(), con);
            Assert.IsTrue(allUser.Count() == count);
        }

        private static void TestOrderBy(TestDbEngine dbEngine, IDbConnection con)
        {
            var firstUser = dbEngine.First<User>(new SqlBuilderSelectParam<User>(), con);
            Assert.IsTrue(firstUser.Id.Trim() == "0");

            firstUser = dbEngine.First<User>(new SqlBuilderSelectParam<User>(new OrderByCondition(UserTable.Id, true)), con);
            Assert.IsTrue(firstUser.Id.Trim() == "99");
        }

        private static void PageSelectTest(TestDbEngine dbEngine, IDbConnection con)
        {
            var firstPage = dbEngine.PageSelect<User>(new PageParam { PageSize = 10, PageNumber = 1 }, 
                new SqlBuilderSelectParam<User>(new OrderByCondition(UserTable.Id)), out var _, out var _, con);
            Assert.IsTrue(firstPage.Count == 10);
            Assert.IsTrue(firstPage[0].Id.Trim() == "0");

            var secondPage = dbEngine.PageSelect<User>(new PageParam { PageSize = 10, PageNumber = 2 }, 
                new SqlBuilderSelectParam<User>(new OrderByCondition(UserTable.Id)), out var _, out var _, con);
            Assert.IsTrue(secondPage.Count == 10);
            Assert.IsTrue(secondPage[0].Id.Trim() == "10");
        }

        private static void DeleteTwoTables(List<PasswordUser> allPwdUser, TestDbEngine dbEngine, IDbConnection con,
            SqlBuilderSelectJoinParam sqlBuilderParam)
        {
            foreach (var user in allPwdUser)
            {
                Assert.IsTrue(user.Id == user.Password.Trim());
                dbEngine.DeleteUser(user.UnionId);
            }

            Assert.IsTrue(!dbEngine.Select<User>(new SqlBuilderSelectParam<User>(), con).Any());
            Assert.IsTrue(!dbEngine.Select<PasswordUser>(sqlBuilderParam, con).Any());
        }
    }
}
