using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine事务操作测试类
    /// 测试DbEngine的事务提交和回滚功能（使用Entity操作）
    /// </summary>
    [TestClass]
    public class DbEngineTransactionTest : TestBase
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
        /// 测试事务成功提交（使用Entity）
        /// </summary>
        [TestMethod]
        public void Transaction_SuccessfulCommit_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user1 = CreateTestUser("1", "TransactionUser1", 100, "Tran1");
            var user2 = CreateTestUser("2", "TransactionUser2", 200, "Tran2");

            Executor.Transaction(tran =>
            {
                DbEngine.Insert(user1, null, tran);
                DbEngine.Insert(user2, null, tran);
            });

            // 验证数据是否已提交
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(2, count, "事务提交后应该有2条记录");

            var retrievedUser1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(retrievedUser1);
            Assert.AreEqual("TransactionUser1", retrievedUser1.UnionId);
        }

        /// <summary>
        /// 测试事务回滚（发生异常）（使用Entity）
        /// </summary>
        [TestMethod]
        public void Transaction_RollbackOnException_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user1 = CreateTestUser("1", "RollbackUser1", 100, "Rollback1");
            var user2 = CreateTestUser("2", "RollbackUser2", 200, "Rollback2");

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                Executor.Transaction(tran =>
                {
                    DbEngine.Insert(user1, null, tran);
                    DbEngine.Insert(user2, null, tran);
                    throw new InvalidOperationException("Intentional exception for rollback test");
                });
            });

            // 验证数据是否已回滚
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(0, count, "事务回滚后应该没有记录");
        }

        /// <summary>
        /// 测试TryTransaction成功执行（使用Entity）
        /// </summary>
        [TestMethod]
        public void TryTransaction_SuccessfulOperation_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user = CreateTestUser("1", "TryUser", 100, "TryTest");

            var result = Executor.TryTransaction(tran =>
            {
                DbEngine.Insert(user, null, tran);
                return true; // 返回一个值表示成功
            });

            Assert.IsTrue(result, "TryTransaction应该成功并返回true");

            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(1, count, "TryTransaction成功后应该有1条记录");
        }

        /// <summary>
        /// 测试TryTransaction异常处理（使用Entity）
        /// </summary>
        [TestMethod]
        public void TryTransaction_ExceptionHandling_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user = CreateTestUser("1", "TryUserFail", 100, "TryTestFail");

            var result = Executor.TryTransaction(tran =>
            {
                DbEngine.Insert(user, null, tran);
                throw new InvalidOperationException("Intentional exception for TryTransaction rollback test");
                return false; // 不会执行到这里
            });

            Assert.IsFalse(result, "TryTransaction发生异常时应该返回默认值false");

            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(0, count, "TryTransaction发生异常后应该没有记录");
        }

        /// <summary>
        /// 测试DbEngine的泛型事务操作（使用Entity）
        /// </summary>
        [TestMethod]
        public void Transaction_GenericOperations_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user1 = CreateTestUser("1", "GenericTranUser1", 100, "GT1");
            var user2 = CreateTestUser("2", "GenericTranUser2", 200, "GT2");

            Executor.Transaction(tran =>
            {
                DbEngine.Insert(user1, null, tran);
                DbEngine.Update(user2, null, tran); // user2不存在，更新0条
            });

            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(1, count, "应该只有user1被插入");

            var retrievedUser1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(retrievedUser1);
            Assert.AreEqual("GenericTranUser1", retrievedUser1.UnionId);

            var retrievedUser2 = DbEngine.SelectById<User>("2");
            Assert.IsNull(retrievedUser2, "user2不应该存在");
        }

        /// <summary>
        /// 测试复杂事务操作（插入、更新、删除）（使用Entity）
        /// </summary>
        [TestMethod]
        public void Transaction_ComplexOperations_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            // 预插入一些数据
            DbEngine.Insert(CreateTestUser("1", "ComplexUser1", 100, "Original1"));
            DbEngine.Insert(CreateTestUser("2", "ComplexUser2", 200, "Original2"));
            DbEngine.Insert(CreateTestUser("3", "ComplexUser3", 300, "Original3"));

            Executor.Transaction(tran =>
            {
                // 1. 插入新记录
                DbEngine.Insert(CreateTestUser("4", "ComplexUser4", 400, "New4"), null, tran);

                // 2. 更新现有记录
                var updateEntity = CreateTestUser("1", "ComplexUser1", 900, "ComplexUpdated");
                DbEngine.Update(updateEntity, null, tran);

                // 3. 删除一条记录
                DbEngine.Delete<User>("2", null, tran);
            });

            // 验证事务提交后的状态
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(3, count, "应该有3条记录 (3 original + 1 new - 1 deleted)");

            var user1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(user1);
            Assert.AreEqual(900, user1.IntData, "User1的IntData应该被更新");
            Assert.AreEqual("ComplexUpdated", user1.SpecialString);

            var user2 = DbEngine.SelectById<User>("2");
            Assert.IsNull(user2, "User2应该已被删除");

            var user3 = DbEngine.SelectById<User>("3");
            Assert.IsNotNull(user3);
            Assert.AreEqual(300, user3.IntData, "User3应该未受影响");

            var user4 = DbEngine.SelectById<User>("4");
            Assert.IsNotNull(user4);
            Assert.AreEqual(400, user4.IntData, "User4应该已被插入");
        }

        


        /// <summary>
        /// 测试事务中的批量操作（使用Entity）
        /// </summary>
        [TestMethod]
        public void Transaction_BatchOperations_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            // 准备批量数据
            var entities = new List<User>();
            for (int i = 1; i <= 5; i++)
            {
                entities.Add(CreateTestUser(i.ToString(), $"BatchTranUser{i}", i * 100, $"Batch{i}"));
            }

            Executor.Transaction(tran =>
            {
                // 在事务中执行批量插入
                DbEngine.Insert(entities, 3, tran);
            });

            // 验证所有记录都被插入
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(5, count, "事务中的批量操作应该插入5条记录");

            // 验证具体记录
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "BatchTranUser%", WhereOperator.Like);
            var results = DbEngine.Select<User>(where, null);
            Assert.AreEqual(5, results.Count, "应该查询到5条批量插入的记录");
        }

        /// <summary>
        /// 测试事务中的混合操作（使用Entity）
        /// </summary>
        [TestMethod]
        public void Transaction_MixedOperations_Should_Succeed()
        {
            // 确保表存在
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            // 预插入一些数据
            DbEngine.Insert(CreateTestUser("1", "MixedUser1", 100, "Original1"));
            DbEngine.Insert(CreateTestUser("2", "MixedUser2", 200, "Original2"));

            Executor.Transaction(tran =>
            {
                // 1. 插入新记录
                DbEngine.Insert(CreateTestUser("3", "MixedUser3", 300, "New3"), null, tran);

                // 2. 更新现有记录
                var updateEntity = CreateTestUser("1", "MixedUser1", 1000, "Updated1");
                DbEngine.Update(updateEntity, null, tran);

                // 3. 删除记录
                DbEngine.Delete<User>("2", null, tran);

                // 4. 批量插入
                var batchEntities = new List<User>
                {
                    CreateTestUser("4", "MixedUser4", 400, "Batch4"),
                    CreateTestUser("5", "MixedUser5", 500, "Batch5")
                };
                DbEngine.Insert(batchEntities, 2, tran);
            });

            // 验证最终状态
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(4, count, "应该有4条记录 (1 original + 1 new + 2 batch - 1 deleted)");

            // 验证具体记录
            var user1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(user1);
            Assert.AreEqual(1000, user1.IntData, "User1应该被更新");
            Assert.AreEqual("Updated1", user1.SpecialString);

            var user2 = DbEngine.SelectById<User>("2");
            Assert.IsNull(user2, "User2应该被删除");

            var user3 = DbEngine.SelectById<User>("3");
            Assert.IsNotNull(user3);
            Assert.AreEqual(300, user3.IntData, "User3应该被插入");

            var user4 = DbEngine.SelectById<User>("4");
            Assert.IsNotNull(user4);
            Assert.AreEqual(400, user4.IntData, "User4应该被批量插入");

            var user5 = DbEngine.SelectById<User>("5");
            Assert.IsNotNull(user5);
            Assert.AreEqual(500, user5.IntData, "User5应该被批量插入");
        }
    }
}