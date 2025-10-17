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
    public class DbTest : TestBase
    {

        /// <summary>
        /// 集成测试：复杂数据类型和关联查询
        /// 测试各种数据类型的插入、查询和关联操作
        /// </summary>
        [TestMethod]
        public void IntegrationTest_ComplexDataTypesAndJoins_Should_Succeed()
        {
                using var con = DbEngine.Executor.GetConnection();
                con.Open();

                // 测试复杂数据类型
                TestComplexDataTypes(con);

                // 测试关联查询
                TestJoinOperations(con);

                con.Close();
        }

        /// <summary>
        /// 测试复杂数据类型
        /// </summary>
        private void TestComplexDataTypes(IDbConnection con)
        {
            const int count = 10;
            var specialString = "test'fdadf_OLOGY£¨KUNSHAN£©ELECTR";
            var flag = false;
            var now = DateTime.Now;

            for (var i = 0; i < count; i++)
            {
                var user = new User
                {
                    Id = i.ToString(),
                    UnionId = (i * 5).ToString(),
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
                DbEngine.Insert(user, con);
            }

            // 验证数据插入
            var allUser = DbEngine.Select<User>(null, null);
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
        /// 注意：由于PasswordUser和PasswordUserTable已被删除，此方法已简化
        /// </summary>
        private void TestJoinOperations(IDbConnection con)
        {
            // 简化的关联查询测试，只使用UserTable
            var users = DbEngine.Select<User>(null, null, con);
            Assert.IsTrue(users.Count == 10, "应该返回10条记录");
        }

        /// <summary>
        /// 测试排序功能
        /// </summary>
        [TestMethod]
        public void Test_OrderByFunctionality_Should_Succeed()
        {
                using var con = DbEngine.Executor.GetConnection();
                con.Open();

                // 插入测试数据
                InsertTestData(con);

                // 测试默认排序（升序）
                var firstUser = DbEngine.First<User>(null, null, con);
                Assert.IsTrue(firstUser.Id.Trim() == "0", "默认排序应该返回ID为0的记录");

                // 测试降序排序
                firstUser = DbEngine.First<User>(null, new OrderByCondition(UserTable.Id, true), con);
                Assert.IsTrue(firstUser.Id.Trim() == "99", "降序排序应该返回ID为99的记录");

                con.Close();
        }

        /// <summary>
        /// 插入测试数据
        /// </summary>
        private void InsertTestData(IDbConnection con)
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
                DbEngine.Insert(user, con);
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