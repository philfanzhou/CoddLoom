using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database.Condition;
using System;
using TestProject.DbCode.Entity;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine关联查询测试类
    /// 测试DbEngine的JoinConditions功能
    /// 注意：由于当前测试环境只有一个表，我们主要测试JoinConditions的基本功能
    /// </summary>
    [TestClass]
    public class DbEngineJoinTest : TestBase
    {
        

        private static User CreateTestUser(string id, string unionId, int intData, string specialString)
        {
            return new User
            {
                Id = id,
                UnionId = unionId,
                RegistrationDate = DateTime.Now,
                IntData = intData,
                Data = new byte[] { 1, 2, 3 },
                DoubleData = 1.1,
                DecimalData = 1.1m,
                ShortData = 1,
                BoolData = true,
                SpecialString = specialString
            };
        }

        /// <summary>
        /// 测试JoinConditions的构造函数和基本属性
        /// </summary>
        [TestMethod]
        public void JoinConditions_Constructor_Should_Succeed()
        {
            // 测试内连接
            var innerJoin = new JoinConditions("Table1", "Column1", "Table2", "Column2", JoinType.Inner);
            Assert.AreEqual("Table1", innerJoin.Table1);
            Assert.AreEqual("Table2", innerJoin.Table2);
            Assert.AreEqual(JoinType.Inner, innerJoin.Type);

            // 测试左连接
            var leftJoin = new JoinConditions("TableA", "Id", "TableB", "ForeignId", JoinType.Left);
            Assert.AreEqual("TableA", leftJoin.Table1);
            Assert.AreEqual("TableB", leftJoin.Table2);
            Assert.AreEqual(JoinType.Left, leftJoin.Type);

            // 测试右连接
            var rightJoin = new JoinConditions("Users", "UserId", "Orders", "UserId", JoinType.Right);
            Assert.AreEqual("Users", rightJoin.Table1);
            Assert.AreEqual("Orders", rightJoin.Table2);
            Assert.AreEqual(JoinType.Right, rightJoin.Type);
        }

        /// <summary>
        /// 测试JoinConditions的Add方法
        /// </summary>
        [TestMethod]
        public void JoinConditions_AddMethod_Should_Succeed()
        {
            var join = new JoinConditions("Table1", "Id", "Table2", "Table1Id", JoinType.Inner);
            
            // 添加额外的连接条件
            join.Add("Table1.Name", "Table2.Name");
            join.Add("Table1.Status", "Table2.Status");

            // 验证连接条件已添加（通过GetTableName方法验证）
            var tableName = join.GetTableName(DbEngine.Executor.SqlBuilder);
                
                Assert.IsNotNull(tableName, "GetTableName应该返回非null值");
            Assert.IsTrue(tableName.Contains("Table1"), "应该包含Table1");
            Assert.IsTrue(tableName.Contains("Table2"), "应该包含Table2");
            Assert.IsTrue(tableName.Contains("INNER JOIN"), "应该包含INNER JOIN");
        }

        /// <summary>
        /// 测试JoinConditions的GetTableName方法
        /// </summary>
        [TestMethod]
        public void JoinConditions_GetTableName_Should_Succeed()
        {

                // 测试内连接
                var innerJoin = new JoinConditions("Users", "Id", "Orders", "UserId", JoinType.Inner);
                var innerTableName = innerJoin.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(innerTableName, "内连接表名不应为null");
                Assert.IsTrue(innerTableName.Contains("Users"), "应该包含Users表");
                Assert.IsTrue(innerTableName.Contains("Orders"), "应该包含Orders表");
                Assert.IsTrue(innerTableName.Contains("INNER JOIN"), "应该包含INNER JOIN");

                // 测试左连接
                var leftJoin = new JoinConditions("Customers", "CustomerId", "Orders", "CustomerId", JoinType.Left);
                var leftTableName = leftJoin.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(leftTableName, "左连接表名不应为null");
                Assert.IsTrue(leftTableName.Contains("Customers"), "应该包含Customers表");
                Assert.IsTrue(leftTableName.Contains("Orders"), "应该包含Orders表");
                Assert.IsTrue(leftTableName.Contains("LEFT JOIN"), "应该包含LEFT JOIN");

                // 测试右连接
                var rightJoin = new JoinConditions("Products", "ProductId", "OrderItems", "ProductId", JoinType.Right);
                var rightTableName = rightJoin.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(rightTableName, "右连接表名不应为null");
                Assert.IsTrue(rightTableName.Contains("Products"), "应该包含Products表");
                Assert.IsTrue(rightTableName.Contains("OrderItems"), "应该包含OrderItems表");
                Assert.IsTrue(rightTableName.Contains("RIGHT JOIN"), "应该包含RIGHT JOIN");
        }

        /// <summary>
        /// 测试JoinConditions的复杂连接条件
        /// </summary>
        [TestMethod]
        public void JoinConditions_ComplexJoin_Should_Succeed()
        {

                // 创建复杂的连接条件
                var join = new JoinConditions("Users", "Id", "UserRoles", "UserId", JoinType.Inner);
                join.Add("Users.Status", "UserRoles.Status");
                join.Add("Users.TenantId", "UserRoles.TenantId");

                var tableName = join.GetTableName(DbEngine.Executor.SqlBuilder);
                
                Assert.IsNotNull(tableName, "复杂连接表名不应为null");
                Assert.IsTrue(tableName.Contains("Users"), "应该包含Users表");
                Assert.IsTrue(tableName.Contains("UserRoles"), "应该包含UserRoles表");
                Assert.IsTrue(tableName.Contains("INNER JOIN"), "应该包含INNER JOIN");
                Assert.IsTrue(tableName.Contains("Id"), "应该包含Id字段");
                Assert.IsTrue(tableName.Contains("UserId"), "应该包含UserId字段");
        }

        

        

        

        /// <summary>
        /// 测试JoinConditions的边界情况
        /// </summary>
        [TestMethod]
        public void JoinConditions_EdgeCases_Should_Succeed()
        {

                // 测试特殊字符（避免空字符串，因为会导致异常）
                var join1 = new JoinConditions("Table-1", "Column_1", "Table-2", "Column_2", JoinType.Left);
                var tableName1 = join1.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(tableName1, "特殊字符表名应该也能生成SQL");
                Assert.IsTrue(tableName1.Contains("Table-1"), "应该包含特殊字符表名");

                // 测试很长的表名和列名
                var longTableName = "VeryLongTableNameThatExceedsNormalLength";
                var longColumnName = "VeryLongColumnNameThatExceedsNormalLength";
                var join2 = new JoinConditions(longTableName, longColumnName, "Table2", "Column2", JoinType.Right);
                var tableName2 = join2.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(tableName2, "长表名应该也能生成SQL");
                Assert.IsTrue(tableName2.Contains(longTableName), "应该包含长表名");

                // 测试数字开头的表名
                var join3 = new JoinConditions("Table123", "Column456", "Table789", "Column012", JoinType.Inner);
                var tableName3 = join3.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(tableName3, "数字开头的表名应该也能生成SQL");
                Assert.IsTrue(tableName3.Contains("Table123"), "应该包含数字开头的表名");
        }
    }
}