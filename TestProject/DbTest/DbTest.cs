using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Convert;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TestProject.DbCode;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine集成测试类
    /// 测试复杂的数据类型、关联查询和集成场景
    /// </summary>
    [TestClass]
    public class DbTest
    {

        /// <summary>
        /// 集成测试：复杂数据类型和关联查询
        /// 测试各种数据类型的插入、查询和关联操作
        /// </summary>
        [TestMethod]
        public void IntegrationTest_ComplexDataTypesAndJoins_Should_Succeed()
        {
            var executor = TestExecutorFactory.CreateInMemoryExecutor();
            try
            {
                var dbEngine = new TestDbEngine(executor);
                using var con = dbEngine.Executor.GetConnection();
                con.Open();

                // 测试复杂数据类型
                TestComplexDataTypes(dbEngine, con);

                // 测试关联查询
                TestJoinOperations(dbEngine, con);

                con.Close();
            }
            finally
            {
                TestExecutorFactory.CleanupTestData(executor);
            }
        }

        /// <summary>
        /// 测试复杂数据类型
        /// </summary>
        private static void TestComplexDataTypes(TestDbEngine dbEngine, IDbConnection con)
        {
            const int count = 10;
            var specialString = "test'fdadf_OLOGY£¨KUNSHAN£©ELECTR";
            var flag = false;
            var now = DateTime.Now;

            for (var i = 0; i < count; i++)
            {
                var pwdUser = new PasswordUser
                {
                    Id = i.ToString(),
                    UnionId = (i * 5).ToString(),
                    Password = i.ToString(),
                    RegistrationDate = now,
                    Data = Encoding.UTF8.GetBytes(i.ToString()),
                    DoubleData = i * 0.0001,
                    DecimalData = (decimal)(i * 0.00001),
                    ShortData = (short)i,
                    IntData = i,
                    BoolData = flag,
                    SpecialString = specialString
                };
                flag = !flag;
                dbEngine.Insert(pwdUser, con);
                dbEngine.Insert<User>(pwdUser, con);
            }

            // 验证数据插入
            var allUser = dbEngine.Select<User>(null, null);
            Assert.IsTrue(allUser.Count == count);

            // 验证复杂数据类型
            flag = false;
            for (var i = 0; i < count; i++)
            {
                var user = allUser[i];
                Assert.AreEqual(i.ToString(), user.Id);
                Assert.AreEqual((i * 5).ToString(), user.UnionId);
                AssertDateTime(now, user.RegistrationDate);
                
                // 验证字节数组
                var data = Encoding.UTF8.GetBytes(i.ToString());
                for (var j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], user.Data[j]);
                }
                
                // 验证数值类型
                Assert.IsTrue(i * 0.0001 - user.DoubleData < 0.0001);
                Assert.IsTrue((decimal)(i * 0.00001) - user.DecimalData < (decimal)0.00001);
                Assert.AreEqual((short)i, user.ShortData);
                Assert.AreEqual(i, user.IntData);
                Assert.AreEqual(flag, user.BoolData);
                Assert.AreEqual(specialString, user.SpecialString);
                flag = !flag;
            }
        }

        /// <summary>
        /// 测试关联查询操作
        /// </summary>
        private static void TestJoinOperations(TestDbEngine dbEngine, IDbConnection con)
        {
            // 创建关联查询条件
            var pwsUserJoin = new JoinConditions(UserTable.TableName, UserTable.UnionId,
                PasswordUserTable.TableName, PasswordUserTable.UnionId);
            
            // 执行关联查询
            var allPwdUser = dbEngine.Select(rec => rec.ToEntity<PasswordUser>(), pwsUserJoin, null, null, con).ToList();
            Assert.IsTrue(allPwdUser.Count == 10, "关联查询应该返回10条记录");

            // 验证关联查询结果
            foreach (var user in allPwdUser)
            {
                Assert.IsTrue(user.Id == user.Password.Trim(), "关联查询结果验证失败");
            }

            // 清理测试数据
            foreach (var user in allPwdUser)
            {
                dbEngine.DeleteUser(user.UnionId);
            }

            // 验证清理结果
            Assert.IsTrue(!dbEngine.Select<User>(null, null, con).Any(), "User表应该为空");
            Assert.IsTrue(!dbEngine.Select(rec => rec.ToEntity<PasswordUser>(), pwsUserJoin, null, null, con).Any(), "关联查询结果应该为空");
        }

        /// <summary>
        /// 测试排序功能
        /// </summary>
        [TestMethod]
        public void Test_OrderByFunctionality_Should_Succeed()
        {
            var executor = TestExecutorFactory.CreateInMemoryExecutor();
            try
            {
                var dbEngine = new TestDbEngine(executor);
                using var con = dbEngine.Executor.GetConnection();
                con.Open();

                // 插入测试数据
                InsertTestData(dbEngine, con);

                // 测试默认排序（升序）
                var firstUser = dbEngine.First<User>(null, null, con);
                Assert.IsTrue(firstUser.Id.Trim() == "0", "默认排序应该返回ID为0的记录");

                // 测试降序排序
                firstUser = dbEngine.First<User>(null, new OrderByCondition(UserTable.Id, true), con);
                Assert.IsTrue(firstUser.Id.Trim() == "99", "降序排序应该返回ID为99的记录");

                con.Close();
            }
            finally
            {
                TestExecutorFactory.CleanupTestData(executor);
            }
        }

        /// <summary>
        /// 插入测试数据
        /// </summary>
        private static void InsertTestData(TestDbEngine dbEngine, IDbConnection con)
        {
            for (var i = 0; i < 100; i++)
            {
                var user = new User
                {
                    Id = i.ToString(),
                    UnionId = (i * 5).ToString(),
                    RegistrationDate = DateTime.Now,
                    IntData = i,
                    Data = Encoding.UTF8.GetBytes(i.ToString()),
                    DoubleData = i * 0.0001,
                    DecimalData = (decimal)(i * 0.00001),
                    ShortData = (short)i,
                    BoolData = i % 2 == 0,
                    SpecialString = $"Test_{i}"
                };
                dbEngine.Insert(user, con);
            }
        }

        /// <summary>
        /// 日期时间断言辅助方法
        /// </summary>
        private static void AssertDateTime(DateTime time1, DateTime time2)
        {
            Assert.AreEqual(time1.Year, time2.Year);
            Assert.AreEqual(time1.Month, time2.Month);
            Assert.AreEqual(time1.Day, time2.Day);
            Assert.AreEqual(time1.Hour, time2.Hour);
            Assert.AreEqual(time1.Minute, time2.Minute);
            Assert.AreEqual(time1.Second, time2.Second);
            // 不比较毫秒，因为数据库精度可能不同
        }
    }
}