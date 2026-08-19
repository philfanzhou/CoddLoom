using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qz.Infra.Database.Condition;
using System;
using System.Collections.Generic;
using TestProject.DbCode.Entity;
using TestProject.DbCode.Tables;

namespace TestProject.DbTest
{
    /// <summary>
    /// DbEngine批量操作测试类
    /// 测试DbEngine的批量插入功能（使用Entity操作）
    /// </summary>
    [TestClass]
    public class DbEngineBatchTest : TestBase
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
        /// 测试基础批量插入（使用Entity）
        /// 批量插入应该要么全部成功，要么全部失败
        /// </summary>
        [TestMethod]
        public void Insert_BatchRecords_Should_Succeed()
        {

                // 准备批量测试实体 - 使用完全唯一的ID避免主键冲突
                var entities = new List<User>();
                for (int i = 1; i <= 100; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(), // 使用完整GUID确保绝对唯一性
                        $"BatchUser{i:D3}",
                        i,
                        $"Batch{i}"
                    ));
                }

                // 执行批量插入，批次大小为50（测试分批处理）
                var affected = DbEngine.Insert(entities, 50);

                // 验证结果 - 应该插入所有记录
                Assert.AreEqual(100, affected, "应该插入所有100条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "BatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(100, count, "应该插入所有100条记录");
        }

        /// <summary>
        /// 测试批量插入主键冲突应该抛出异常
        /// 修复后的逻辑应该要么全部成功，要么全部失败
        /// </summary>
        [TestMethod]
        public void Insert_BatchRecords_WithPrimaryKeyConflict_Should_ThrowException()
        {

                // 准备包含重复主键的测试实体，故意制造主键冲突
                var entities = new List<User>();
                var duplicateId = Guid.NewGuid().ToString();
                
                // 添加3条记录，其中2条有相同的主键（故意制造冲突）
                for (int i = 1; i <= 3; i++)
                {
                    entities.Add(CreateTestUser(
                        i <= 2 ? duplicateId : Guid.NewGuid().ToString(), // 前2条记录使用重复ID
                        $"ConflictUser{i}",
                        i,
                        $"Conflict{i}"
                    ));
                }

                // 执行批量插入应该抛出异常
                Assert.ThrowsExactly<InvalidOperationException>(() => 
                {
                    DbEngine.Insert(entities, 2);
                }, "主键冲突时应该抛出异常");

                // 验证没有记录被插入（事务回滚）
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "ConflictUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(0, count, "主键冲突时不应该插入任何记录");
        }

        /// <summary>
        /// 测试大批量记录插入（使用Entity）
        /// 测试大数据量和参数限制处理
        /// </summary>
        [TestMethod]
        public void Insert_LargeBatchRecords_Should_Succeed()
        {

                // 准备大批量测试实体 - 500条记录，测试参数限制处理
                var entities = new List<User>();
                for (int i = 1; i <= 500; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(),
                        $"LargeBatchUser{i:D3}",
                        i,
                        $"Large{i}"
                    ));
                }

                // 执行大批量插入，批次大小为100（测试参数限制和分批处理）
                var affected = DbEngine.Insert(entities, 100);

                // 验证结果 - 应该插入所有记录
                Assert.AreEqual(500, affected, "应该插入所有500条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "LargeBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(500, count, "应该插入所有500条记录");
        }

        /// <summary>
        /// 测试小批次大小的批量插入（使用Entity）
        /// 测试小批次处理逻辑
        /// </summary>
        [TestMethod]
        public void Insert_SmallBatchSize_Should_Succeed()
        {

                // 准备测试实体 - 30条记录
                var entities = new List<User>();
                for (int i = 1; i <= 30; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(),
                        $"SmallBatchUser{i:D2}",
                        i,
                        $"Small{i}"
                    ));
                }

                // 执行批量插入，使用小批次大小（测试分批处理）
                var affected = DbEngine.Insert(entities, 5);

                // 验证结果 - 应该插入所有记录
                Assert.AreEqual(30, affected, "应该插入所有30条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "SmallBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(30, count, "应该插入所有30条记录");
        }

        /// <summary>
        /// 测试事务中的批量插入（使用Entity）
        /// 测试事务中的大批量处理
        /// </summary>
        [TestMethod]
        public void Insert_BatchInTransaction_Should_Succeed()
        {

                // 准备测试实体 - 200条记录
                var entities = new List<User>();
                for (int i = 1; i <= 200; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(),
                        $"TranBatchUser{i:D3}",
                        i,
                        $"Tran{i}"
                    ));
                }

                // 在事务中执行批量插入
                Executor.Transaction(tran =>
                {
                    var affected = DbEngine.Insert(entities, 50, tran);
                    Assert.AreEqual(200, affected, "事务中应该插入所有200条记录");
                });

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "TranBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(200, count, "事务提交后应该查询到所有200条记录");
        }

        /// <summary>
        /// 测试批量插入异常回滚（使用Entity）
        /// </summary>
        [TestMethod]
        public void Insert_BatchExceptionRollback_Should_Succeed()
        {

                // 准备测试实体
                var entities = new List<User>();
                for (int i = 1; i <= 3; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(),
                        $"RollbackBatchUser{i}",
                        i,
                        $"Rollback{i}"
                    ));
                }

                // 在事务中执行批量插入，并故意抛出异常
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                {
                    Executor.Transaction(tran =>
                    {
                        DbEngine.Insert(entities, 2, tran);
                        throw new InvalidOperationException("Intentional exception for rollback test");
                    });
                });

                // 验证数据已回滚
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "RollbackBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(0, count, "事务回滚后应该没有记录");
        }

        

        

        /// <summary>
        /// 测试混合实体类型的批量插入（使用Entity）
        /// 注意：这个测试主要验证Entity操作的灵活性
        /// </summary>
        [TestMethod]
        public void Insert_MixedEntityTypes_Should_Succeed()
        {

                // 准备不同类型的测试实体
                var userEntities = new List<User>();
                for (int i = 1; i <= 3; i++)
                {
                    userEntities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(),
                        $"MixedUser{i}",
                        i * 100,
                        $"Mixed{i}"
                    ));
                }

                // 分别插入不同类型的实体
                var affected1 = DbEngine.Insert(userEntities, 2);
                Assert.AreEqual(3, affected1, "用户实体应该插入所有3条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "MixedUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(3, count, "应该插入所有3条用户记录");
        }

        

        

        /// <summary>
        /// 测试参数限制处理（使用Entity）
        /// 测试接近SQL Server参数限制的大批量插入
        /// </summary>
        [TestMethod]
        public void Insert_ParameterLimitTest_Should_Succeed()
        {

                // 准备接近参数限制的测试实体
                // User实体有10个字段，批次大小200意味着2000个参数，接近2100的限制
                var entities = new List<User>();
                for (int i = 1; i <= 200; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(),
                        $"ParamLimitUser{i:D3}",
                        i,
                        $"Param{i}"
                    ));
                }

                // 执行批量插入，批次大小为200（测试参数限制处理）
                var affected = DbEngine.Insert(entities, 200);

                // 验证结果 - 应该插入所有记录
                Assert.AreEqual(200, affected, "应该插入所有200条记录");

                // 验证数据是否正确插入
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "ParamLimitUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(200, count, "应该插入所有200条记录");
        }

        
    }
}