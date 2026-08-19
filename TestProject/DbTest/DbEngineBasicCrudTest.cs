using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using TestProject.DbCode;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;
using TestProject.DbTest;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine基础CRUD操作测试类
    /// 测试DbEngine的基本增删改查功能（使用Entity操作）
    /// </summary>
    [TestClass]
    public class DbEngineBasicCrudTest : TestBase
    {

        /// <summary>
        /// 创建测试用户实体
        /// </summary>
        private static User CreateTestUser(string id, string unionId, int intData, string specialString)
        {
            return new User
            {
                Id = id,
                UnionId = unionId,
                RegistrationDate = DateTime.Now,
                IntData = intData,
                Data = new byte[] { 1, 2, 3, 4, 5 },
                DoubleData = 123.45,
                DecimalData = 123.456m,
                ShortData = (short)123,
                BoolData = true,
                SpecialString = specialString
            };
        }

        /// <summary>
        /// 测试单条记录插入（使用Entity）
        /// </summary>
        [TestMethod]
        public void Insert_SingleRecord_Should_Succeed()
        {
            // 准备测试实体
            var entity = CreateTestUser("1", "TestUser", 123, "SingleTest");

            // 执行插入
            var affected = DbEngine.Insert(entity);

            // 验证结果
            Assert.AreEqual(1, affected, "应该插入1条记录");

            // 验证数据是否正确插入
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "TestUser");
            var count = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(1, count, "应该查询到1条记录");
        }

        /// <summary>
        /// 测试批量记录插入（使用Entity）
        /// </summary>
        [TestMethod]
        public void Insert_BatchRecords_Should_Succeed()
        {
            // 准备批量测试实体
            var entities = new List<User>();
            for (int i = 1; i <= 3; i++)
            {
                entities.Add(CreateTestUser(i.ToString(), $"BatchUser{i}", i * 100, $"Batch{i}"));
            }

            // 执行批量插入
            var affected = DbEngine.Insert(entities, 2);

            // 验证结果
            Assert.AreEqual(3, affected, "应该插入3条记录");

            // 验证数据是否正确插入
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "BatchUser%", WhereOperator.Like);
            var count = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(3, count, "应该查询到3条记录");
        }

        /// <summary>
        /// 测试记录更新（使用Entity）
        /// </summary>
        [TestMethod]
        public void Update_Record_Should_Succeed()
        {
            // 先插入测试数据
            var originalEntity = CreateTestUser("1", "OriginalUser", 100, "Original");
            DbEngine.Insert(originalEntity);

            // 准备更新实体
            var updatedEntity = CreateTestUser("1", "UpdatedUser", 200, "Updated");
            updatedEntity.RegistrationDate = DateTime.Now.AddDays(1);

            // 执行更新
            var affected = DbEngine.Update(updatedEntity);

            // 验证结果
            Assert.AreEqual(1, affected, "应该更新1条记录");

            // 验证数据是否正确更新
            var retrievedEntity = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(retrievedEntity, "应该查询到更新后的实体");
            Assert.AreEqual("UpdatedUser", retrievedEntity.UnionId, "UnionId应该已更新");
            Assert.AreEqual(200, retrievedEntity.IntData, "IntData应该已更新");
            Assert.AreEqual("Updated", retrievedEntity.SpecialString, "SpecialString应该已更新");
        }

        /// <summary>
        /// 测试记录删除（使用Entity）
        /// </summary>
        [TestMethod]
        public void Delete_Record_Should_Succeed()
        {
            // 先插入测试数据
            var entity = CreateTestUser("1", "ToBeDeleted", 300, "DeleteTest");
            DbEngine.Insert(entity);

            // 验证数据已插入
            var beforeWhere = new WhereConditions();
            beforeWhere.Add(UserTable.UnionId, "ToBeDeleted");
            var beforeCount = DbEngine.Count(UserTable.TableName, beforeWhere);
            Assert.AreEqual(1, beforeCount, "删除前应该有1条记录");

            // 执行删除
            var affected = DbEngine.Delete<User>("1");

            // 验证结果
            Assert.AreEqual(1, affected, "应该删除1条记录");

            // 验证数据已删除
            var afterCount = DbEngine.Count(UserTable.TableName, beforeWhere);
            Assert.AreEqual(0, afterCount, "删除后应该没有记录");
       }

        /// <summary>
        /// 测试记录查询（使用Entity）
        /// </summary>
        [TestMethod]
        public void Select_Records_Should_Succeed()
        {
            // 先插入测试数据
            var entity1 = CreateTestUser("1", "SelectUser1", 100, "Select1");
            var entity2 = CreateTestUser("2", "SelectUser2", 200, "Select2");
            DbEngine.Insert(entity1);
            DbEngine.Insert(entity2);

            // 执行查询
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "SelectUser%", WhereOperator.Like);
            var orderBy = new OrderByCondition(UserTable.UnionId, false); // ASC
            var columns = new ColumnParam();
            columns.AddSelect(UserTable.UnionId);
            columns.AddSelect(UserTable.IntData);

            var results = DbEngine.Select<User>(where, orderBy, columns);

            // 验证结果
            Assert.IsNotNull(results, "查询结果不应为null");
            Assert.AreEqual(2, results.Count, "应该查询到2条记录");
            Assert.AreEqual("SelectUser1", results[0].UnionId);
            Assert.AreEqual(100, results[0].IntData);
            Assert.AreEqual("SelectUser2", results[1].UnionId);
            Assert.AreEqual(200, results[1].IntData);
        }

        /// <summary>
        /// 测试通过ID查询（使用Entity）
        /// </summary>
        [TestMethod]
        public void SelectById_Record_Should_Succeed()
        {
            // 先插入测试数据
            var entity = CreateTestUser("1", "SelectByIdUser", 400, "SelectById");
            DbEngine.Insert(entity);

            // 执行通过ID查询
            var result = DbEngine.SelectById<User>("1");

            // 验证结果
            Assert.IsNotNull(result, "应该查询到实体");
            Assert.AreEqual("SelectByIdUser", result.UnionId, "UnionId应该匹配");
            Assert.AreEqual(400, result.IntData, "IntData应该匹配");
       }

        /// <summary>
        /// 测试记录计数（使用Entity）
        /// </summary>
        [TestMethod]
        public void Count_Records_Should_Succeed()
        {
            // 先插入测试数据
            var entity1 = CreateTestUser("1", "CountUser1", 100, "Count1");
            var entity2 = CreateTestUser("2", "CountUser2", 200, "Count2");
            var entity3 = CreateTestUser("3", "OtherUser", 300, "Other");
            DbEngine.Insert(entity1);
            DbEngine.Insert(entity2);
            DbEngine.Insert(entity3);

            // 测试总计数
            var totalCount = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(3, totalCount, "总记录数应该是3");

            // 测试条件计数
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "CountUser%", WhereOperator.Like);
            var filteredCount = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(2, filteredCount, "符合条件的记录数应该是2");
       }

        /// <summary>
        /// 测试记录存在性检查（使用Entity）
        /// </summary>
        [TestMethod]
        public void Exist_Record_Should_Succeed()
        {
            // 先插入测试数据
            var entity = CreateTestUser("1", "ExistUser", 500, "ExistTest");
            DbEngine.Insert(entity);

            // 测试存在性检查
            var existsWhere = new WhereConditions();
            existsWhere.Add(UserTable.UnionId, "ExistUser");
            var exists = DbEngine.Exist(UserTable.TableName, existsWhere);

            // 验证结果
            Assert.IsTrue(exists, "记录应该存在");

            // 测试不存在的记录
            var notExistsWhere = new WhereConditions();
            notExistsWhere.Add(UserTable.UnionId, "NonExistentUser");
            var notExists = DbEngine.Exist(UserTable.TableName, notExistsWhere);
            Assert.IsFalse(notExists, "记录不应该存在");
        }

        /// <summary>
        /// 测试表删除（使用Entity）
        /// </summary>
        [TestMethod]
        public void Drop_Table_Should_Succeed()
        {
            // 先插入测试数据
            var entity = CreateTestUser("1", "DropUser", 600, "DropTest");
            DbEngine.Insert(entity);

            // 验证表存在
            var beforeCount = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(1, beforeCount, "删除表前应该有1条记录");

            // 执行删除表
            DbEngine.Drop(UserTable.TableName);

            // 验证表已删除（尝试查询应该抛出异常）
            try
            {
                DbEngine.Count(UserTable.TableName, new WhereConditions());
                Assert.Fail("删除表后查询应该抛出异常");
            }
            catch (Exception ex)
            {
                // 预期会抛出异常，因为表已被删除
                Assert.IsTrue(ex.Message.Contains("no such table") || ex.Message.Contains("table"), 
                    $"应该抛出表不存在的异常，但实际异常是: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试复杂查询条件（使用Entity）
        /// </summary>
        [TestMethod]
        public void Select_WithComplexConditions_Should_Succeed()
        {
            // 先插入测试数据
            var entity1 = CreateTestUser("1", "ComplexUser1", 100, "Complex1");
            var entity2 = CreateTestUser("2", "ComplexUser2", 200, "Complex2");
            var entity3 = CreateTestUser("3", "OtherUser", 300, "Other");
            DbEngine.Insert(entity1);
            DbEngine.Insert(entity2);
            DbEngine.Insert(entity3);

            // 测试复杂查询条件
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "ComplexUser%", WhereOperator.Like);
            where.Add(UserTable.IntData, 150, WhereOperator.GreaterThan);
            var orderBy = new OrderByCondition(UserTable.IntData, true); // DESC

            var results = DbEngine.Select<User>(where, orderBy);

            // 验证结果
            Assert.IsNotNull(results, "查询结果不应为null");
            Assert.AreEqual(1, results.Count, "应该查询到1条记录");
            Assert.AreEqual("ComplexUser2", results[0].UnionId);
            Assert.AreEqual(200, results[0].IntData);
        }

        
    }
}