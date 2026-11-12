using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Sql;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Condition;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// End-to-end coverage for the issue #86 asynchronous command family of
    /// <see cref="DbEngine"/> against the real database selected by
    /// <see cref="TestExecutorFactory"/>: SQLite in the default local matrix
    /// and the CI integration providers. Every test is the asynchronous twin
    /// of an existing synchronous test: the same inputs must produce the same
    /// SQL, batch boundaries, transaction ownership, and exception types; the
    /// byte-level SQL parity contract is asserted separately by the fake-based
    /// <c>DbEngineAsyncSqlParityTest</c>.
    /// </summary>
    [TestClass]
    public class DbEngineAsyncIntegrationTest : TestBase
    {
        /// <summary>
        /// Creates a test user entity with unique values per call.
        /// </summary>
        private static User CreateUser(string id, string unionId, int intData = 0)
        {
            return new User
            {
                Id = id,
                UnionId = unionId,
                RegistrationDate = DateTime.Now,
                IntData = intData,
                Data = new byte[] { 1, 2, 3 },
                DoubleData = 123.45,
                DecimalData = 123.456m,
                ShortData = (short)123,
                BoolData = true,
                SpecialString = null
            };
        }

        /// <summary>
        /// Creates the minimal full-required-column row for raw inserts.
        /// </summary>
        private static InputValues CreateRequiredRow(string id, string unionId)
        {
            return new InputValues()
                .Add(UserTable.Id, id)
                .Add(UserTable.UnionId, unionId)
                .Add(UserTable.DoubleData, 0d)
                .Add(UserTable.DecimalData, 0m)
                .Add(UserTable.ShortData, (short)0)
                .Add(UserTable.IntData, 0)
                .Add(UserTable.BoolData, false);
        }

        private int CountByMarker(string marker)
        {
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, marker + "%", WhereOperator.Like);
            return DbEngine.Count(UserTable.TableName, where);
        }

        [TestMethod]
        public async Task AsyncCommandFamily_RunEndToEndOnTheRealDatabase()
        {
            // InitializeTableAsync recreates the table through the async path.
            await DbEngine.DropAsync(UserTable.TableName);
            await DbEngine.InitializeTableAsync(new List<TableDefine> { new(typeof(UserTable)) });

            // InsertAsync: single entity.
            Assert.AreEqual(1, await DbEngine.InsertAsync(CreateUser("1", "asyncOne", 10)),
                "One record should be inserted.");

            // InsertAsync: single InputValues.
            Assert.AreEqual(1, await DbEngine.InsertAsync(UserTable.TableName, CreateRequiredRow("2", "asyncTwo")),
                "One record should be inserted.");

            // InsertAsync: entity batch, batch size splits 3 rows into 2 + 1.
            var entityBatch = new List<User>
            {
                CreateUser("3", "asyncThree", 30),
                CreateUser("4", "asyncFour", 40),
                CreateUser("5", "asyncFive", 50)
            };
            Assert.AreEqual(3, await DbEngine.InsertAsync(entityBatch, 2),
                "All three records should be inserted in one transaction.");

            // InsertAsync: InputValues batch inside one command.
            var inputBatch = new List<InputValues>
            {
                CreateRequiredRow("6", "asyncSix"),
                CreateRequiredRow("7", "asyncSeven")
            };
            Assert.AreEqual(2, await DbEngine.InsertAsync(UserTable.TableName, inputBatch, 2));

            // CountAsync: with the column parameter and the convenience overload.
            Assert.AreEqual(7, await DbEngine.CountAsync(UserTable.TableName, new WhereConditions(), columns: null));
            var likeWhere = new WhereConditions();
            likeWhere.Add(UserTable.UnionId, "async%", WhereOperator.Like);
            Assert.AreEqual(7, await DbEngine.CountAsync(UserTable.TableName, likeWhere));

            // ExistAsync: matching and non-matching conditions.
            Assert.IsTrue(await DbEngine.ExistAsync(UserTable.TableName, likeWhere));
            var absentWhere = new WhereConditions();
            absentWhere.Add(UserTable.UnionId, "absent");
            Assert.IsFalse(await DbEngine.ExistAsync(UserTable.TableName, absentWhere));

            // UpdateAsync: entity by primary key.
            var updated = CreateUser("1", "asyncOneUpdated", 11);
            Assert.AreEqual(1, await DbEngine.UpdateAsync(updated));

            // UpdateAsync: table, input, and condition.
            var input = new InputValues().Add(UserTable.UnionId, "asyncTwoUpdated");
            var updateWhere = new WhereConditions();
            updateWhere.Add(UserTable.Id, "2");
            Assert.AreEqual(1, await DbEngine.UpdateAsync(UserTable.TableName, input, updateWhere));

            // DeleteAsync: by primary key and by condition.
            Assert.AreEqual(1, await DbEngine.DeleteAsync<User>("3"));
            var deleteWhere = new WhereConditions();
            deleteWhere.Add(UserTable.Id, "4");
            Assert.AreEqual(1, await DbEngine.DeleteAsync(UserTable.TableName, deleteWhere));

            Assert.AreEqual(5, await DbEngine.CountAsync(UserTable.TableName, new WhereConditions()));

            // DropAsync removes the table; querying it fails afterwards.
            await DbEngine.DropAsync(UserTable.TableName);
            await Assert.ThrowsAsync<Exception>(
                () => DbEngine.CountAsync(UserTable.TableName, new WhereConditions()));

            // Re-create the table so the shared cleanup stays simple.
            await DbEngine.InitializeTableAsync(new List<TableDefine> { new(typeof(UserTable)) });
        }

        [TestMethod]
        public async Task InsertAsync_BatchRecords_Should_Succeed()
        {
            var entities = new List<User>();
            for (var i = 1; i <= 100; i++)
            {
                entities.Add(CreateUser(Guid.NewGuid().ToString(), $"AsyncBatchUser{i:D3}", i));
            }

            Assert.AreEqual(100, await DbEngine.InsertAsync(entities, 50),
                "All 100 records should be inserted.");
            Assert.AreEqual(100, CountByMarker("AsyncBatchUser"),
                "All 100 records should be present.");
        }

        [TestMethod]
        public async Task InsertAsync_LargeBatchRecords_Should_Succeed()
        {
            var entities = new List<User>();
            for (var i = 1; i <= 500; i++)
            {
                entities.Add(CreateUser(Guid.NewGuid().ToString(), $"AsyncLargeUser{i:D3}", i));
            }

            Assert.AreEqual(500, await DbEngine.InsertAsync(entities, 100),
                "All 500 records should be inserted.");
            Assert.AreEqual(500, CountByMarker("AsyncLargeUser"),
                "All 500 records should be present.");
        }

        [TestMethod]
        public async Task InsertAsync_BatchRecords_WithPrimaryKeyConflict_Should_ThrowAndRollBack()
        {
            var duplicateId = Guid.NewGuid().ToString();
            var entities = new List<User>();
            for (var i = 1; i <= 3; i++)
            {
                entities.Add(CreateUser(
                    i <= 2 ? duplicateId : Guid.NewGuid().ToString(),
                    $"AsyncConflictUser{i}",
                    i));
            }

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => DbEngine.InsertAsync(entities, 2),
                "A primary-key conflict should throw.");

            Assert.AreEqual(0, CountByMarker("AsyncConflictUser"),
                "The engine-owned transaction must roll every batch back.");
        }

        [TestMethod]
        public async Task InsertAsync_BatchFailureCaughtInsideCallerTransaction_DoesNotInsertDiagnosticRows()
        {
            var duplicateId = Guid.NewGuid().ToString();
            var marker = $"AsyncHandled{Guid.NewGuid():N}";
            var entities = new List<User>
            {
                CreateUser(duplicateId, marker + "1", 1),
                CreateUser(duplicateId, marker + "2", 2)
            };

            await Executor.TransactionAsync(async transaction =>
            {
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                    () => DbEngine.InsertAsync(entities, 2, transaction));
            });

            Assert.AreEqual(0, CountByMarker(marker),
                "Handling a batch failure must not commit rows inserted by failure diagnostics.");
        }

        [TestMethod]
        public async Task InsertAsync_BatchInTransaction_Should_Succeed()
        {
            var entities = new List<User>();
            for (var i = 1; i <= 200; i++)
            {
                entities.Add(CreateUser(Guid.NewGuid().ToString(), $"AsyncTranUser{i:D3}", i));
            }

            await Executor.TransactionAsync(async transaction =>
            {
                Assert.AreEqual(200, await DbEngine.InsertAsync(entities, 50, transaction),
                    "All 200 records should be inserted within the caller transaction.");
            });

            Assert.AreEqual(200, CountByMarker("AsyncTranUser"),
                "All 200 records should be returned after commit.");
        }

        [TestMethod]
        public async Task InsertAsync_BatchExceptionRollback_Should_Succeed()
        {
            var entities = new List<User>();
            for (var i = 1; i <= 3; i++)
            {
                entities.Add(CreateUser(Guid.NewGuid().ToString(), $"AsyncRollbackUser{i}", i));
            }

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => Executor.TransactionAsync(async transaction =>
                {
                    await DbEngine.InsertAsync(entities, 2, transaction);
                    throw new InvalidOperationException("Intentional exception for rollback test");
                }));

            Assert.AreEqual(0, CountByMarker("AsyncRollbackUser"),
                "No records should remain after rollback.");
        }

        [TestMethod]
        public async Task InsertAsync_ProviderParameterLimit_Should_SplitWithoutLiteralFallback()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"coddloom-async-limit-{Guid.NewGuid():N}.db");
            var executor = new ConstrainedSqliteExecutor(
                $"Data Source={dbPath};Version=3;Pooling=False;", 15, true);
            var engine = new TestDbEngine(executor);

            try
            {
                var entities = new List<User>
                {
                    CreateUser(Guid.NewGuid().ToString(), "AsyncLimitSplit1", 1),
                    CreateUser(Guid.NewGuid().ToString(), "AsyncLimitSplit2", 2),
                    CreateUser(Guid.NewGuid().ToString(), "AsyncLimitSplit3", 3)
                };

                Assert.AreEqual(3, await engine.InsertAsync(entities, 3));
            }
            finally
            {
                await engine.DropAsync(UserTable.TableName);
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [TestMethod]
        public async Task InsertAsync_BatchSizeOfOne_Should_InsertEveryRowAsSingleRowBatch()
        {
            var singleRowDb = Path.Combine(Path.GetTempPath(), $"coddloom-async-batch1-{Guid.NewGuid():N}.db");
            var singleRowExecutor = new BatchCountingSqliteExecutor($"Data Source={singleRowDb};Version=3;Pooling=False;");
            var engine = new TestDbEngine(singleRowExecutor);

            var batchOfTenDb = Path.Combine(Path.GetTempPath(), $"coddloom-async-batch10-{Guid.NewGuid():N}.db");
            var batchOfTenExecutor = new BatchCountingSqliteExecutor($"Data Source={batchOfTenDb};Version=3;Pooling=False;");
            var engineWithBatchOfTen = new TestDbEngine(batchOfTenExecutor);

            try
            {
                var entities = new List<User>();
                for (var i = 1; i <= 5; i++)
                {
                    entities.Add(CreateUser(Guid.NewGuid().ToString(), $"AsyncSingleRow{i}", i));
                }

                Assert.AreEqual(5, await engine.InsertAsync(entities, 1));
                Assert.AreEqual(5, singleRowExecutor.InsertBatchCount,
                    "A batch size of one must issue one insert command per row.");

                Assert.AreEqual(5, await engineWithBatchOfTen.InsertAsync(entities, 10));
                Assert.AreEqual(1, batchOfTenExecutor.InsertBatchCount,
                    "Rows within the batch size limit must be sent as a single insert command.");
            }
            finally
            {
                await engine.DropAsync(UserTable.TableName);
                await engineWithBatchOfTen.DropAsync(UserTable.TableName);
                if (File.Exists(singleRowDb)) File.Delete(singleRowDb);
                if (File.Exists(batchOfTenDb)) File.Delete(batchOfTenDb);
            }
        }

        [TestMethod]
        public async Task InsertAsync_NonPositiveBatchSize_Should_ThrowBeforeTransactionAndEnumeration()
        {
            var entityZero = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => DbEngine.InsertAsync(new UnEnumeratedSequence<User>(), 0),
                "A batch size of zero must be rejected before any work starts.");
            Assert.AreEqual("batchSize", entityZero.ParamName,
                "The exception must come from the batchSize validation, not an upstream mapping error.");

            var entityNegative = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => DbEngine.InsertAsync(new UnEnumeratedSequence<User>(), -5));
            Assert.AreEqual("batchSize", entityNegative.ParamName);

            var inputZero = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => DbEngine.InsertAsync(UserTable.TableName, new UnEnumeratedSequence<InputValues>(), 0));
            Assert.AreEqual("batchSize", inputZero.ParamName);

            var inputNegative = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => DbEngine.InsertAsync(UserTable.TableName, new UnEnumeratedSequence<InputValues>(), -5));
            Assert.AreEqual("batchSize", inputNegative.ParamName);

            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task InitializeTableAsync_AddsMissingColumns_AndRemainsIdempotent()
        {
            var dbEngine = new CoddLoom.DbEngine(Executor);

            try
            {
                await dbEngine.InitializeTableAsync([new TableDefine(typeof(AsyncBaseTable))]);
                Assert.AreEqual(1, await dbEngine.InsertAsync(AsyncBaseTable.TableName,
                    new InputValues()
                        .Add(AsyncBaseTable.Id, "pre-async-migration")
                        .Add(AsyncBaseTable.Name, "existing row")));

                var fullTable = new TableDefine(typeof(AsyncExtendedTable));
                await dbEngine.InitializeTableAsync([fullTable]);
                await dbEngine.InitializeTableAsync([fullTable]);

                var migratedExistingValue = Executor.Scalar(
                    $"SELECT {AsyncExtendedTable.ExtraText} FROM {AsyncExtendedTable.TableName} "
                    + $"WHERE {AsyncExtendedTable.Id} = 'pre-async-migration'",
                    value => value.ToString());
                Assert.IsNull(migratedExistingValue,
                    "An added nullable column must read as null for pre-existing rows.");

                var values = new InputValues()
                    .Add(AsyncExtendedTable.Id, "async-migration-1")
                    .Add(AsyncExtendedTable.Name, "async migration")
                    .Add(AsyncExtendedTable.ExtraText, "added")
                    .Add(AsyncExtendedTable.ExtraNumber, 2);

                Assert.AreEqual(1, await dbEngine.InsertAsync(AsyncExtendedTable.TableName, values));
                var addedValue = Executor.Scalar(
                    $"SELECT {AsyncExtendedTable.ExtraText} FROM {AsyncExtendedTable.TableName} "
                    + $"WHERE {AsyncExtendedTable.Id} = 'async-migration-1'",
                    value => value.ToString());
                Assert.AreEqual("added", addedValue);
            }
            finally
            {
                dbEngine.Drop(AsyncExtendedTable.TableName);
            }
        }

        [TestMethod]
        public async Task InitializeTableAsync_RejectsRequiredColumnOnPopulatedTableBeforeAnyDdl()
        {
            var dbEngine = new CoddLoom.DbEngine(Executor);

            try
            {
                await dbEngine.InitializeTableAsync([new TableDefine(typeof(AsyncBaseTable))]);
                await dbEngine.InsertAsync(AsyncBaseTable.TableName,
                    new InputValues()
                        .Add(AsyncBaseTable.Id, "async-existing-1")
                        .Add(AsyncBaseTable.Name, "existing row"));

                var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                    () => dbEngine.InitializeTableAsync([new TableDefine(typeof(AsyncRequiredColumnTable))]));

                StringAssert.Contains(exception.Message, AsyncRequiredColumnTable.TableName);
                StringAssert.Contains(exception.Message, AsyncRequiredColumnTable.RequiredColumn);
                Assert.IsFalse(CanSelectColumn(AsyncRequiredColumnTable.TableName,
                    AsyncRequiredColumnTable.RequiredColumn),
                    "The failure must happen before any DDL runs.");
            }
            finally
            {
                dbEngine.Drop(AsyncRequiredColumnTable.TableName);
            }
        }

        [TestMethod]
        public async Task PreCancelledToken_ThrowsBeforeAnyDatabaseCall_ForEveryCommandMethod()
        {
            var cancelled = new CancellationToken(canceled: true);
            var where = new WhereConditions();
            where.Add(UserTable.Id, "cancel-1");

            // The TestBase engine's column cache is warm, so the entity-based
            // methods reach the token check instead of failing on cold mapping.
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.InsertAsync(UserTable.TableName, CreateRequiredRow("cancel-1", "cancelOne"),
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.InsertAsync(UserTable.TableName,
                    new List<InputValues> { CreateRequiredRow("cancel-2", "cancelTwo") }, 2,
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.InsertAsync(CreateUser("cancel-3", "cancelThree"),
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.InsertAsync(new List<User> { CreateUser("cancel-4", "cancelFour") }, 2,
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.UpdateAsync(UserTable.TableName,
                    new InputValues().Add(UserTable.UnionId, "x"), where,
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.UpdateAsync(CreateUser("cancel-5", "cancelFive"),
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.DeleteAsync(UserTable.TableName, where, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.DeleteAsync<User>("cancel-6", cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.CountAsync(UserTable.TableName, where, columns: null, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.CountAsync(UserTable.TableName, where, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.ExistAsync(UserTable.TableName, where, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.DropAsync(UserTable.TableName, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.InitializeTableAsync([new TableDefine(typeof(UserTable))], cancelled));

            // Nothing executed: the count query succeeds and observes zero rows,
            // proving no writes ran and the canceled DropAsync kept the table.
            Assert.AreEqual(0, DbEngine.Count(UserTable.TableName, new WhereConditions()),
                "A canceled call must not write anything or drop the table.");
        }

        private bool CanSelectColumn(string tableName, string columnName)
        {
            try
            {
                Executor.Reader($"SELECT {columnName} FROM {tableName}", _ => true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A sequence that fails the test if it is ever enumerated, proving that
        /// argument validation happens before transaction start or data enumeration.
        /// </summary>
        private sealed class UnEnumeratedSequence<T> : IEnumerable<T>
        {
            public IEnumerator<T> GetEnumerator()
            {
                throw new InvalidOperationException("The sequence must never be enumerated by this test.");
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

        /// <summary>
        /// A SQLite executor whose SqlBuilder counts the insert commands it builds,
        /// exposing exactly how many insert batches were sent to the database.
        /// </summary>
        private sealed class BatchCountingSqliteExecutor(string connectionString)
            : SqliteExecutor(connectionString)
        {
            public int InsertBatchCount => ((CountingSqlBuilder)SqlBuilder).InsertBatchCount;

            public override SqlBuilder SqlBuilder { get; } = new CountingSqlBuilder();

            private sealed class CountingSqlBuilder : SqlBuilder
            {
                public int InsertBatchCount { get; private set; }

                public override string Insert(string tableName, IEnumerable<InputValues> inputs,
                    out List<ValueParam> dbParams, bool useParameter = true)
                {
                    InsertBatchCount++;
                    return base.Insert(tableName, inputs, out dbParams, useParameter);
                }
            }
        }

        private static class AsyncBaseTable
        {
            [DbTableName]
            public const string TableName = "AsyncMigrationBaseTable";

            [DbPrimaryKey(Type = DbType.String)]
            public const string Id = "id";

            [DbColumnString(AllowEmpty = false)]
            public const string Name = "name";

            [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
            public const string CreatedDate = "createdDate";
        }

        private static class AsyncExtendedTable
        {
            [DbTableName]
            public const string TableName = AsyncBaseTable.TableName;

            [DbPrimaryKey(Type = DbType.String)]
            public const string Id = AsyncBaseTable.Id;

            [DbColumnString(AllowEmpty = false)]
            public const string Name = AsyncBaseTable.Name;

            [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
            public const string CreatedDate = AsyncBaseTable.CreatedDate;

            [DbColumnString(AllowEmpty = true)]
            public const string ExtraText = "extraText";

            [DbColumn(Type = DbType.Int32, AllowEmpty = true)]
            public const string ExtraNumber = "extraNumber";
        }

        private static class AsyncRequiredColumnTable
        {
            [DbTableName]
            public const string TableName = AsyncBaseTable.TableName;

            [DbPrimaryKey(Type = DbType.String)]
            public const string Id = AsyncBaseTable.Id;

            [DbColumnString(AllowEmpty = false)]
            public const string Name = AsyncBaseTable.Name;

            [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
            public const string CreatedDate = AsyncBaseTable.CreatedDate;

            [DbColumn(Type = DbType.Int32, AllowEmpty = false)]
            public const string RequiredColumn = "asyncRequiredColumn";
        }
    }
}
