using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TestProject.DbCode;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;
using TestProject.DbTest;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine扩展方法测试类
    /// 测试DbEngine的泛型CRUD操作（Insert<T>、Update<T>、Delete<T>、Select<T>等）
    /// </summary>
    [TestClass]
    public class DbEngineExtensionTest : TestBase
    {
        

        /// <summary>
        /// 测试泛型插入操作
        /// </summary>
        [TestMethod]
        public void Insert_GenericEntity_Should_Succeed()
        {

                // 准备测试实体
                var entity = new User
                {
                    Id = "1", // 设置主键ID
                    UnionId = "GenericUser",
                    RegistrationDate = DateTime.Now,
                    IntData = 100,
                    Data = new byte[] { 1, 2, 3, 4, 5 },
                    DoubleData = 100.5,
                    DecimalData = 100.123m,
                    ShortData = (short)100,
                    BoolData = true,
                    SpecialString = "GenericTest"
                };

                // 执行泛型插入
                var affected = DbEngine.Insert(entity);

                // 验证结果
                Assert.AreEqual(1, affected, "应该插入1条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "GenericUser");
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(1, count, "应该查询到1条记录");
        }

        /// <summary>
        /// 测试泛型批量插入操作
        /// </summary>
        [TestMethod]
        public void Insert_GenericBatchEntities_Should_Succeed()
        {

                // 准备批量测试实体
                var entities = new List<User>();
                for (int i = 1; i <= 3; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(), // 设置主键ID
                        UnionId = $"BatchGenericUser{i}",
                        RegistrationDate = DateTime.Now,
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"BatchGeneric{i}"
                    });
                }

                // 执行泛型批量插入
                var affected = DbEngine.Insert(entities, 2);

                // 验证结果
                Assert.AreEqual(3, affected, "应该插入3条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "BatchGenericUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(3, count, "应该查询到3条记录");
        }

        /// <summary>
        /// 测试泛型更新操作
        /// </summary>
        [TestMethod]
        public void Update_GenericEntity_Should_Succeed()
        {

                // 先插入测试数据
                var originalEntity = new User
                {
                    Id = "1", // 设置主键ID
                    UnionId = "OriginalGenericUser",
                    RegistrationDate = DateTime.Now,
                    IntData = 200,
                    Data = new byte[] { 10, 20, 30 },
                    DoubleData = 200.5,
                    DecimalData = 200.123m,
                    ShortData = (short)200,
                    BoolData = true,
                    SpecialString = "OriginalGeneric"
                };
                DbEngine.Insert(originalEntity);

                // 准备更新实体（保持相同的主键ID）
                var updatedEntity = new User
                {
                    Id = "1", // 保持相同的主键ID
                    UnionId = "UpdatedGenericUser",
                    RegistrationDate = DateTime.Now.AddDays(1),
                    IntData = 300,
                    Data = new byte[] { 30, 40, 50 },
                    DoubleData = 300.5,
                    DecimalData = 300.123m,
                    ShortData = (short)300,
                    BoolData = false,
                    SpecialString = "UpdatedGeneric"
                };

                // 执行泛型更新
                var affected = DbEngine.Update(updatedEntity);

                // 验证结果
                Assert.AreEqual(1, affected, "应该更新1条记录");

                // 验证数据是否正确更新
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "UpdatedGenericUser");
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(1, count, "应该查询到1条更新后的记录");
        }

        /// <summary>
        /// 测试泛型删除操作
        /// </summary>
        [TestMethod]
        public void Delete_GenericEntity_Should_Succeed()
        {

                // 先插入测试数据
                var entity = new User
                {
                    Id = "1", // 设置主键ID
                    UnionId = "ToBeDeletedGeneric",
                    RegistrationDate = DateTime.Now,
                    IntData = 400,
                    Data = new byte[] { 40, 50, 60 },
                    DoubleData = 400.5,
                    DecimalData = 400.123m,
                    ShortData = (short)400,
                    BoolData = true,
                    SpecialString = "DeleteGeneric"
                };
                DbEngine.Insert(entity);

                // 验证数据已插入
                var beforeWhere = new WhereConditions();
                beforeWhere.Add(UserTable.UnionId, "ToBeDeletedGeneric");
                var beforeCount = DbEngine.Count(UserTable.TableName, beforeWhere);
                Assert.AreEqual(1, beforeCount, "删除前应该有1条记录");

                // 执行泛型删除（通过主键ID删除）
                var affected = DbEngine.Delete<User>("1"); // 使用字符串主键ID

                // 验证结果
                Assert.AreEqual(1, affected, "应该删除1条记录");

                // 验证数据已删除
                var afterCount = DbEngine.Count(UserTable.TableName, beforeWhere);
                Assert.AreEqual(0, afterCount, "删除后应该没有记录");
        }

        /// <summary>
        /// 测试泛型查询操作
        /// </summary>
        [TestMethod]
        public void Select_GenericEntities_Should_Succeed()
        {

                // 先插入测试数据
                var entity1 = new User
                {
                    Id = "1", // 设置主键ID
                    UnionId = "SelectUser1",
                    RegistrationDate = DateTime.Now,
                    IntData = 1,
                    Data = new byte[] { 1, 1, 1 },
                    DoubleData = 1.1,
                    DecimalData = 1.11m,
                    ShortData = (short)1,
                    BoolData = true,
                    SpecialString = "Select1"
                };
                DbEngine.Insert(entity1);

                var entity2 = new User
                {
                    Id = "2", // 设置主键ID
                    UnionId = "SelectUser2",
                    RegistrationDate = DateTime.Now,
                    IntData = 2,
                    Data = new byte[] { 2, 2, 2 },
                    DoubleData = 2.2,
                    DecimalData = 2.22m,
                    ShortData = (short)2,
                    BoolData = false,
                    SpecialString = "Select2"
                };
                DbEngine.Insert(entity2);

                // 执行泛型查询
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "SelectUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.UnionId, false); // false = ASC
                var results = DbEngine.Select<User>(where, orderBy);

                // 验证结果
                Assert.AreEqual(2, results.Count, "应该查询到2条记录");
                Assert.AreEqual("SelectUser1", results[0].UnionId, "第一条记录应该是SelectUser1");
                Assert.AreEqual("SelectUser2", results[1].UnionId, "第二条记录应该是SelectUser2");
        }

        /// <summary>
        /// 测试通过ID查询单个实体
        /// </summary>
        [TestMethod]
        public void SelectById_GenericEntity_Should_Succeed()
        {

                // 先插入测试数据
                var entity = new User
                {
                    Id = "1", // 设置主键ID
                    UnionId = "SelectByIdUser",
                    RegistrationDate = DateTime.Now,
                    IntData = 500,
                    Data = new byte[] { 50, 60, 70 },
                    DoubleData = 500.5,
                    DecimalData = 500.123m,
                    ShortData = (short)500,
                    BoolData = true,
                    SpecialString = "SelectById"
                };
                DbEngine.Insert(entity);

                // 执行通过ID查询
                var result = DbEngine.SelectById<User>("1"); // 使用字符串主键ID

                // 验证结果
                Assert.IsNotNull(result, "应该查询到实体");
                Assert.AreEqual("SelectByIdUser", result.UnionId, "UnionId应该匹配");
                Assert.AreEqual(500, result.IntData, "IntData应该匹配");
        }

        /// <summary>
        /// 测试泛型存在性检查
        /// </summary>
        [TestMethod]
        public void Exist_GenericEntity_Should_Succeed()
        {

                // 先插入测试数据
                var entity = new User
                {
                    Id = "1", // 设置主键ID
                    UnionId = "ExistGenericUser",
                    RegistrationDate = DateTime.Now,
                    IntData = 600,
                    Data = new byte[] { 60, 70, 80 },
                    DoubleData = 600.5,
                    DecimalData = 600.123m,
                    ShortData = (short)600,
                    BoolData = false,
                    SpecialString = "ExistGeneric"
                };
                DbEngine.Insert(entity);

                // 测试存在性检查
                var existsWhere = new WhereConditions();
                existsWhere.Add(UserTable.UnionId, "ExistGenericUser");
                var exists = DbEngine.Exist(UserTable.TableName, existsWhere);

                // 验证结果
                Assert.IsTrue(exists, "应该存在记录");

                // 测试不存在的记录
                var notExistsWhere = new WhereConditions();
                notExistsWhere.Add(UserTable.UnionId, "NonExistentUser");
                var notExists = DbEngine.Exist(UserTable.TableName, notExistsWhere);

                // 验证结果
                Assert.IsFalse(notExists, "不应该存在记录");
        }
    }
}
