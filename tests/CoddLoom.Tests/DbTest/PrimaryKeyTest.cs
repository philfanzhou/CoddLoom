using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom;
using CoddLoom.Condition;
using CoddLoom.Entity;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoddLoom.Tests.DbTest
{
    [TestClass]
    public class PrimaryKeyTest : TestBase
    {
        [TestMethod]
        public void EntityWithoutPrimaryKeyAttribute_Should_Work_WhenTableDefineHasPrimaryKey()
        {
                var engine = new PrimaryKeyTestDbEngine(Executor);

                // Test an entity without a PrimaryKey attribute whose TableDefine declares a primary key.
                var entity = new TestEntityWithoutPrimaryKey
                {
                    Id = 1,
                    Name = "TestEntity",
                    CreatedDate = DateTime.Now
                };

                // This should not throw because TableDefine already declares a primary key.
                var affected = DbEngine.Insert(entity);
                Assert.AreEqual(1, affected);

                // Verify that the record can be queried by primary key.
                using var con = Executor.GetConnection();
                con.Open();

                // ById should resolve the primary key without throwing a "does not have a primary key" exception.
                // A successful call confirms that the primary key was resolved.
                var where = WhereConditions.ById<TestEntityWithoutPrimaryKey>(1, out var tableName);
                Assert.AreEqual(TestEntityWithoutPrimaryKeyTable.TableName, tableName);
                
                var result = DbEngine.Select<TestEntityWithoutPrimaryKey>(where, null, con).FirstOrDefault();
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Id);
                Assert.AreEqual("TestEntity", result.Name);
        }

        /// <summary>
        /// Verifies that ById throws when the entity's TableDefine does not declare a primary key.
        /// ById uses EntityMap to find the primary-key definition and throws when none exists.
        /// </summary>
        [TestMethod]
        public void EntityWithoutPrimaryKey_Should_ThrowException_WhenTableDefineHasNoPrimaryKey()
        {
                var engine = new PrimaryKeyTestDbEngine(Executor);

                // Test entity: TestEntityWithoutPrimaryKey2.
                // Corresponding TableDefine: TestEntityWithoutPrimaryKeyTable2, which has no primary key.
                var entity = new TestEntityWithoutPrimaryKey2
                {
                    Name = "TestEntity",
                    CreatedDate = DateTime.Now
                };

                // Insertion should succeed because TableDefine declares the columns, even without a primary key.
                var affected = DbEngine.Insert(entity);
                Assert.AreEqual(1, affected);

                // Verify that ById throws because TableDefine does not declare a primary key.
                try
                {
                    WhereConditions.ById<TestEntityWithoutPrimaryKey2>(1, out var tableName);
                    Assert.Fail("Should throw exception when TableDefine has no primary key defined");
                }
                catch (ArgumentException ex)
                {
                    // Verify that the exception message mentions the primary key.
                    Assert.IsTrue(ex.Message.Contains("primary key") || ex.Message.Contains("PrimaryKey"), 
                        $"Exception message should contain 'primary key' information. Actual message: {ex.Message}");
                }
        }

        

        private static void AssertDateTime(DateTime time1, DateTime time2)
        {
            Assert.AreEqual(time1.Year, time2.Year);
            Assert.AreEqual(time1.Month, time2.Month);
            Assert.AreEqual(time1.Day, time2.Day);
            Assert.AreEqual(time1.Hour, time2.Hour);
            Assert.AreEqual(time1.Minute, time2.Minute);
            Assert.AreEqual(time1.Second, time2.Second);
            //Assert.AreEqual(time1.Millisecond, time2.Millisecond);
        }
    }

    #region Test DbEngine

    public class PrimaryKeyTestDbEngine : DbEngine
    {
        public PrimaryKeyTestDbEngine(DbExecutor executor)
            : base(executor, new List<TableDefine>
            {
                new(typeof(TestEntityWithoutPrimaryKeyTable)),
                new(typeof(TestEntityWithoutPrimaryKeyTable2))
            })
        {
        }
    }

    #endregion

    #region Test Entities and Tables

    internal static class TestEntityWithoutPrimaryKeyTable
    {
        [DbTableName]
        internal const string TableName = "TestEntityWithoutPrimaryKey";

        [DbPrimaryKey(Type = DbType.Int32)]
        internal const string Id = "id";

        [DbColumnString(AllowEmpty = false)]
        internal const string Name = "name";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string CreatedDate = "createdDate";
    }

    internal static class TestEntityWithoutPrimaryKeyTable2
    {
        [DbTableName]
        internal const string TableName = "TestEntityWithoutPrimaryKey2";

        // This table deliberately has no primary key.
        [DbColumnString(AllowEmpty = false)]
        internal const string Name = "name";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string CreatedDate = "createdDate";
    }

    [MapTable(Name = TestEntityWithoutPrimaryKeyTable.TableName)]
    public class TestEntityWithoutPrimaryKey
    {
        // PrimaryKey = true is omitted because TableDefine already declares the primary key.
        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable.Id)]
        public int Id { get; set; }

        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable.Name)]
        public string Name { get; set; }

        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable.CreatedDate)]
        public DateTime CreatedDate { get; set; }
    }

    [MapTable(Name = TestEntityWithoutPrimaryKeyTable2.TableName)]
    public class TestEntityWithoutPrimaryKey2
    {
        // PrimaryKey = true is omitted and TableDefine does not declare a primary key.
        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable2.Name)]
        public string Name { get; set; }

        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable2.CreatedDate)]
        public DateTime CreatedDate { get; set; }
    }

    #endregion
}
