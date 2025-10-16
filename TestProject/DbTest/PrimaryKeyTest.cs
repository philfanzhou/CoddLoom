using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Entity;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TestProject.DbCode;
using TestProject.DbTest;

namespace TestProject.DbTest
{
    [TestClass]
    public class PrimaryKeyTest : TestBase
    {
        [TestMethod]
        public void EntityWithoutPrimaryKeyAttribute_Should_Work_WhenTableDefineHasPrimaryKey()
        {
                var engine = new PrimaryKeyTestDbEngine(Executor);

                // 测试Entity没有指定PrimaryKey属性，但TableDefine中定义了主键的情况
                var entity = new TestEntityWithoutPrimaryKey
                {
                    Id = 1,
                    Name = "TestEntity",
                    CreatedDate = DateTime.Now
                };

                // 这应该不会抛出异常，因为TableDefine中已经定义了主键
                var affected = DbEngine.Insert(entity);
                Assert.AreEqual(1, affected);

                // 验证可以通过主键查询
                using var con = Executor.GetConnection();
                con.Open();

                // 使用ById方法测试主键查询，这应该不会抛出"does not have a primary key"异常
                // 如果我们的修改正确，这里应该能够成功获取主键
                var where = WhereConditions.ById<TestEntityWithoutPrimaryKey>(1, out var tableName);
                Assert.AreEqual(TestEntityWithoutPrimaryKeyTable.TableName, tableName);
                
                var result = DbEngine.Select<TestEntityWithoutPrimaryKey>(where, null, con).FirstOrDefault();
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Id);
                Assert.AreEqual("TestEntity", result.Name);
        }

        /// <summary>
        /// 测试当Entity对应的TableDefine中没有定义主键时，调用ById方法应该抛出异常
        /// 底层逻辑：ById方法通过EntityMap查找TableDefine中的主键定义，如果TableDefine没有定义主键，则抛出异常
        /// </summary>
        [TestMethod]
        public void EntityWithoutPrimaryKey_Should_ThrowException_WhenTableDefineHasNoPrimaryKey()
        {
                var engine = new PrimaryKeyTestDbEngine(Executor);

                // 测试Entity：TestEntityWithoutPrimaryKey2
                // 对应的TableDefine：TestEntityWithoutPrimaryKeyTable2（注意：这个Table没有定义主键）
                var entity = new TestEntityWithoutPrimaryKey2
                {
                    Name = "TestEntity",
                    CreatedDate = DateTime.Now
                };

                // 插入应该成功，因为TableDefine定义了列结构（即使没有主键）
                var affected = DbEngine.Insert(entity);
                Assert.AreEqual(1, affected);

                // 验证使用ById方法会抛出异常，因为TableDefine中没有定义主键
                try
                {
                    WhereConditions.ById<TestEntityWithoutPrimaryKey2>(1, out var tableName);
                    Assert.Fail("Should throw exception when TableDefine has no primary key defined");
                }
                catch (ArgumentException ex)
                {
                    // 验证异常消息包含"primary key"相关信息
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

        // 注意：这个Table没有定义主键
        [DbColumnString(AllowEmpty = false)]
        internal const string Name = "name";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string CreatedDate = "createdDate";
    }

    [MapTable(Name = TestEntityWithoutPrimaryKeyTable.TableName)]
    public class TestEntityWithoutPrimaryKey
    {
        // 注意：这里没有指定PrimaryKey = true，但TableDefine中已经定义了主键
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
        // 注意：这里没有指定PrimaryKey = true，且TableDefine中也没有定义主键
        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable2.Name)]
        public string Name { get; set; }

        [MapColumn(Name = TestEntityWithoutPrimaryKeyTable2.CreatedDate)]
        public DateTime CreatedDate { get; set; }
    }

    #endregion
}
