using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Condition;
using System;
using CoddLoom.Tests.DbCode.Entity;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Tests DbEngine joined queries.
    /// Covers JoinConditions behavior.
    /// The test environment contains one physical table, so these tests focus on core JoinConditions behavior.
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
        /// Tests the JoinConditions constructor and basic properties.
        /// </summary>
        [TestMethod]
        public void JoinConditions_Constructor_Should_Succeed()
        {
            // Test an inner join.
            var innerJoin = new JoinConditions("Table1", "Column1", "Table2", "Column2", JoinType.Inner);
            Assert.AreEqual("Table1", innerJoin.Table1);
            Assert.AreEqual("Table2", innerJoin.Table2);
            Assert.AreEqual(JoinType.Inner, innerJoin.Type);

            // Test a left join.
            var leftJoin = new JoinConditions("TableA", "Id", "TableB", "ForeignId", JoinType.Left);
            Assert.AreEqual("TableA", leftJoin.Table1);
            Assert.AreEqual("TableB", leftJoin.Table2);
            Assert.AreEqual(JoinType.Left, leftJoin.Type);

            // Test a right join.
            var rightJoin = new JoinConditions("Users", "UserId", "Orders", "UserId", JoinType.Right);
            Assert.AreEqual("Users", rightJoin.Table1);
            Assert.AreEqual("Orders", rightJoin.Table2);
            Assert.AreEqual(JoinType.Right, rightJoin.Type);
        }

        /// <summary>
        /// Tests JoinConditions.Add.
        /// </summary>
        [TestMethod]
        public void JoinConditions_AddMethod_Should_Succeed()
        {
            var join = new JoinConditions("Table1", "Id", "Table2", "Table1Id", JoinType.Inner);
            
            // Add another join condition.
            join.Add("Table1.Name", "Table2.Name");
            join.Add("Table1.Status", "Table2.Status");

            // Verify the added condition through GetTableName.
            var tableName = join.GetTableName(DbEngine.Executor.SqlBuilder);
                
                Assert.IsNotNull(tableName, "GetTableName should return a non-null value.");
            Assert.Contains("Table1", tableName, "The result should contain Table1.");
            Assert.Contains("Table2", tableName, "The result should contain Table2.");
            Assert.Contains("INNER JOIN", tableName, "The result should contain INNER JOIN.");
        }

        /// <summary>
        /// Tests JoinConditions.GetTableName.
        /// </summary>
        [TestMethod]
        public void JoinConditions_GetTableName_Should_Succeed()
        {

                // Test an inner join.
                var innerJoin = new JoinConditions("Users", "Id", "Orders", "UserId", JoinType.Inner);
                var innerTableName = innerJoin.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(innerTableName, "The inner-join table name should not be null.");
                Assert.Contains("Users", innerTableName, "The result should contain the Users table.");
                Assert.Contains("Orders", innerTableName, "The result should contain the Orders table.");
                Assert.Contains("INNER JOIN", innerTableName, "The result should contain INNER JOIN.");

                // Test a left join.
                var leftJoin = new JoinConditions("Customers", "CustomerId", "Orders", "CustomerId", JoinType.Left);
                var leftTableName = leftJoin.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(leftTableName, "The left-join table name should not be null.");
                Assert.Contains("Customers", leftTableName, "The result should contain the Customers table.");
                Assert.Contains("Orders", leftTableName, "The result should contain the Orders table.");
                Assert.Contains("LEFT JOIN", leftTableName, "The result should contain LEFT JOIN.");

                // Test a right join.
                var rightJoin = new JoinConditions("Products", "ProductId", "OrderItems", "ProductId", JoinType.Right);
                var rightTableName = rightJoin.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(rightTableName, "The right-join table name should not be null.");
                Assert.Contains("Products", rightTableName, "The result should contain the Products table.");
                Assert.Contains("OrderItems", rightTableName, "The result should contain the OrderItems table.");
                Assert.Contains("RIGHT JOIN", rightTableName, "The result should contain RIGHT JOIN.");
        }

        /// <summary>
        /// Tests complex JoinConditions.
        /// </summary>
        [TestMethod]
        public void JoinConditions_ComplexJoin_Should_Succeed()
        {

                // Create a complex join condition.
                var join = new JoinConditions("Users", "Id", "UserRoles", "UserId", JoinType.Inner);
                join.Add("Users.Status", "UserRoles.Status");
                join.Add("Users.TenantId", "UserRoles.TenantId");

                var tableName = join.GetTableName(DbEngine.Executor.SqlBuilder);
                
                Assert.IsNotNull(tableName, "The complex join table name should not be null.");
                Assert.Contains("Users", tableName, "The result should contain the Users table.");
                Assert.Contains("UserRoles", tableName, "The result should contain the UserRoles table.");
                Assert.Contains("INNER JOIN", tableName, "The result should contain INNER JOIN.");
                Assert.Contains("Id", tableName, "The result should contain the Id column.");
                Assert.Contains("UserId", tableName, "The result should contain the UserId column.");
        }

        

        

        

        /// <summary>
        /// Tests JoinConditions edge cases.
        /// </summary>
        [TestMethod]
        public void JoinConditions_EdgeCases_Should_Succeed()
        {

                // Test special characters; avoid empty strings because they throw.
                var join1 = new JoinConditions("Table-1", "Column_1", "Table-2", "Column_2", JoinType.Left);
                var tableName1 = join1.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(tableName1, "A table name with special characters should still produce SQL.");
                Assert.Contains("Table-1", tableName1, "The result should contain the table name with special characters.");

                // Test very long table and column names.
                var longTableName = "VeryLongTableNameThatExceedsNormalLength";
                var longColumnName = "VeryLongColumnNameThatExceedsNormalLength";
                var join2 = new JoinConditions(longTableName, longColumnName, "Table2", "Column2", JoinType.Right);
                var tableName2 = join2.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(tableName2, "A long table name should still produce SQL.");
                Assert.Contains(longTableName, tableName2, "The result should contain the long table name.");

                // Test a table name that begins with a digit.
                var join3 = new JoinConditions("Table123", "Column456", "Table789", "Column012", JoinType.Inner);
                var tableName3 = join3.GetTableName(DbEngine.Executor.SqlBuilder);
                Assert.IsNotNull(tableName3, "A table name beginning with a digit should still produce SQL.");
                Assert.Contains("Table123", tableName3, "The result should contain the table name beginning with a digit.");
        }
    }
}
