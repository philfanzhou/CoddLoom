using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Condition;
using System;
using System.Collections.Generic;
using System.IO;
using CoddLoom.Input;
using CoddLoom.Sqlite;
using CoddLoom.Tests.DbCode;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Tests DbEngine batch operations.
    /// Covers entity-based batch insertion.
    /// </summary>
    [TestClass]
    public class DbEngineBatchTest : TestBase
    {
        

        /// <summary>
        /// Creates a test user entity.
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
        /// Tests a basic batch insert with entities.
        /// A batch insert must either succeed completely or fail completely.
        /// </summary>
        [TestMethod]
        public void Insert_BatchRecords_Should_Succeed()
        {

                // Prepare entities with unique IDs to avoid primary-key conflicts.
                var entities = new List<User>();
                for (int i = 1; i <= 100; i++)
                {
                    entities.Add(CreateTestUser(
                        Guid.NewGuid().ToString(), // Use the full GUID to ensure uniqueness.
                        $"BatchUser{i:D3}",
                        i,
                        $"Batch{i}"
                    ));
                }

                // Insert in batches of 50 to exercise batch splitting.
                var affected = DbEngine.Insert(entities, 50);

                // Verify that every record was inserted.
                Assert.AreEqual(100, affected, "All 100 records should be inserted.");

                // Verify that the data was inserted correctly.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "BatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(100, count, "All 100 records should be present.");
        }

        /// <summary>
        /// Verifies that a batch insert throws on a primary-key conflict.
        /// The operation must either succeed completely or fail completely.
        /// </summary>
        [TestMethod]
        public void Insert_BatchRecords_WithPrimaryKeyConflict_Should_ThrowException()
        {

                // Prepare entities with duplicate primary keys to force a conflict.
                var entities = new List<User>();
                var duplicateId = Guid.NewGuid().ToString();
                
                // Add three records, two of which deliberately share a primary key.
                for (int i = 1; i <= 3; i++)
                {
                    entities.Add(CreateTestUser(
                        i <= 2 ? duplicateId : Guid.NewGuid().ToString(), // The first two records use the duplicate ID.
                        $"ConflictUser{i}",
                        i,
                        $"Conflict{i}"
                    ));
                }

                // The batch insert should throw.
                Assert.ThrowsExactly<InvalidOperationException>(() => 
                {
                    DbEngine.Insert(entities, 2);
                }, "A primary-key conflict should throw.");

                // Verify that the transaction rollback left no inserted records.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "ConflictUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(0, count, "No records should be inserted after a primary-key conflict.");
        }

        [TestMethod]
        public void Insert_BatchFailureCaughtInsideCallerTransaction_DoesNotInsertDiagnosticRows()
        {
            var duplicateId = Guid.NewGuid().ToString();
            var marker = $"HandledFailure{Guid.NewGuid():N}";
            var entities = new List<User>
            {
                CreateTestUser(duplicateId, marker + "1", 1, "Handled1"),
                CreateTestUser(duplicateId, marker + "2", 2, "Handled2")
            };

            Executor.Transaction(transaction =>
            {
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    DbEngine.Insert(entities, 2, transaction));
            });

            var where = new WhereConditions();
            where.Add(UserTable.UnionId, marker + "%", WhereOperator.Like);
            Assert.AreEqual(0, DbEngine.Count(UserTable.TableName, where),
                "Handling a batch failure must not commit rows inserted by failure diagnostics.");
        }

        /// <summary>
        /// Tests inserting a large batch of entities.
        /// Covers large data volumes and parameter-limit handling.
        /// </summary>
        [TestMethod]
        public void Insert_LargeBatchRecords_Should_Succeed()
        {

                // Prepare 500 entities to exercise parameter-limit handling.
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

                // Insert in batches of 100 to exercise parameter limits and splitting.
                var affected = DbEngine.Insert(entities, 100);

                // Verify that every record was inserted.
                Assert.AreEqual(500, affected, "All 500 records should be inserted.");

                // Verify that the data was inserted correctly.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "LargeBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(500, count, "All 500 records should be present.");
        }

        /// <summary>
        /// Tests entity insertion with a small batch size.
        /// Covers small-batch processing.
        /// </summary>
        [TestMethod]
        public void Insert_SmallBatchSize_Should_Succeed()
        {

                // Prepare 30 test entities.
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

                // Use a small batch size to exercise batch splitting.
                var affected = DbEngine.Insert(entities, 5);

                // Verify that every record was inserted.
                Assert.AreEqual(30, affected, "All 30 records should be inserted.");

                // Verify that the data was inserted correctly.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "SmallBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(30, count, "All 30 records should be present.");
        }

        /// <summary>
        /// Tests a batch insert of entities within a transaction.
        /// Covers large-batch processing within a transaction.
        /// </summary>
        [TestMethod]
        public void Insert_BatchInTransaction_Should_Succeed()
        {

                // Prepare 200 test entities.
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

                // Perform the batch insert within a transaction.
                Executor.Transaction(tran =>
                {
                    var affected = DbEngine.Insert(entities, 50, tran);
                    Assert.AreEqual(200, affected, "All 200 records should be inserted within the transaction.");
                });

                // Verify that the data was inserted correctly.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "TranBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(200, count, "All 200 records should be returned after commit.");
        }

        /// <summary>
        /// Tests rollback after an exception during an entity batch insert.
        /// </summary>
        [TestMethod]
        public void Insert_BatchExceptionRollback_Should_Succeed()
        {

                // Prepare the test entities.
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

                // Perform the batch insert in a transaction and deliberately throw.
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                {
                    Executor.Transaction(tran =>
                    {
                        DbEngine.Insert(entities, 2, tran);
                        throw new InvalidOperationException("Intentional exception for rollback test");
                    });
                });

                // Verify that the data was rolled back.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "RollbackBatchUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(0, count, "No records should remain after rollback.");
        }

        

        

        /// <summary>
        /// Tests batch insertion with different entity types.
        /// This test primarily verifies the flexibility of entity operations.
        /// </summary>
        [TestMethod]
        public void Insert_MixedEntityTypes_Should_Succeed()
        {

                // Prepare test entities of different types.
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

                // Insert each entity type separately.
                var affected1 = DbEngine.Insert(userEntities, 2);
                Assert.AreEqual(3, affected1, "All three user entities should be inserted.");

                // Verify that the data was inserted correctly.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "MixedUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(3, count, "All three user records should be present.");
        }

        

        

        /// <summary>
        /// Tests parameter-limit handling with entities.
        /// Covers a large insert close to the SQL Server parameter limit.
        /// </summary>
        [TestMethod]
        public void Insert_ParameterLimitTest_Should_Succeed()
        {

                // Prepare enough entities to approach the parameter limit.
                // User has ten fields, so a batch of 200 uses 2,000 parameters, close to the 2,100 limit.
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

                // Insert with a batch size of 200 to exercise parameter-limit handling.
                var affected = DbEngine.Insert(entities, 200);

                // Verify that every record was inserted.
                Assert.AreEqual(200, affected, "All 200 records should be inserted.");

                // Verify that the data was inserted correctly.
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "ParamLimitUser%", WhereOperator.Like);
                var count = DbEngine.Count(UserTable.TableName, where);
                Assert.AreEqual(200, count, "All 200 records should be present.");
        }

        [TestMethod]
        public void Insert_ProviderParameterLimit_Should_SplitWithoutLiteralFallback()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"coddloom-limit-{Guid.NewGuid():N}.db");
            var executor = new ConstrainedSqliteExecutor(
                $"Data Source={dbPath};Version=3;Pooling=False;", 15, true);
            var engine = new TestDbEngine(executor);

            try
            {
                var entities = new List<User>
                {
                    CreateTestUser(Guid.NewGuid().ToString(), "LimitSplit1", 1, "Split1"),
                    CreateTestUser(Guid.NewGuid().ToString(), "LimitSplit2", 2, "Split2"),
                    CreateTestUser(Guid.NewGuid().ToString(), "LimitSplit3", 3, "Split3")
                };

                Assert.AreEqual(3, engine.Insert(entities, 3));
            }
            finally
            {
                engine.Drop(UserTable.TableName);
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [TestMethod]
        public void Insert_AbortedTransactionProvider_Should_NotRetryFailedCommand()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"coddloom-abort-{Guid.NewGuid():N}.db");
            var executor = new ConstrainedSqliteExecutor(
                $"Data Source={dbPath};Version=3;Pooling=False;", 100, false);
            var engine = new TestDbEngine(executor);
            var duplicateId = Guid.NewGuid().ToString();

            try
            {
                var entities = new List<User>
                {
                    CreateTestUser(duplicateId, "Abort1", 1, "Abort1"),
                    CreateTestUser(duplicateId, "Abort2", 2, "Abort2")
                };

                var exception = Assert.ThrowsExactly<InvalidOperationException>(
                    () => engine.Insert(entities, 2));

                Assert.IsNotNull(exception.InnerException);
                Assert.IsFalse(exception.InnerException is InvalidOperationException,
                    "The original provider exception should be retained instead of retrying the failed transaction.");
            }
            finally
            {
                engine.Drop(UserTable.TableName);
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [TestMethod]
        public void Insert_BatchSizeOfOne_Should_InsertEveryRowAsSingleRowBatch()
        {
            var entities = new List<User>();
            for (int i = 1; i <= 3; i++)
            {
                entities.Add(CreateTestUser(
                    Guid.NewGuid().ToString(),
                    $"SingleRowBatchUser{i}",
                    i,
                    $"Single{i}"
                ));
            }

            var affected = DbEngine.Insert(entities, 1);

            Assert.AreEqual(3, affected, "Each row should be inserted as its own single-row batch.");

            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "SingleRowBatchUser%", WhereOperator.Like);
            var count = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(3, count, "All rows should be present after a batch size of one.");
        }

        [TestMethod]
        public void Insert_NonPositiveBatchSize_Should_ThrowBeforeTransactionAndEnumeration()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => DbEngine.Insert(new UnEnumeratedInputs(), 0),
                "A batch size of zero must be rejected before any work starts.");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => DbEngine.Insert(new UnEnumeratedInputs(), -5),
                "A negative batch size must be rejected before any work starts.");
        }

        private sealed class UnEnumeratedInputs : IEnumerable<InputValues>
        {
            public IEnumerator<InputValues> GetEnumerator()
            {
                throw new InvalidOperationException("Inputs must not be enumerated.");
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class ConstrainedSqliteExecutor(
            string connectionString,
            int maxParametersPerCommand,
            bool canContinueTransactionAfterCommandFailure)
            : SqliteExecutor(connectionString)
        {
            public override int MaxParametersPerCommand { get; } = maxParametersPerCommand;

            public override bool CanContinueTransactionAfterCommandFailure { get; }
                = canContinueTransactionAfterCommandFailure;
        }

        
    }
}
