using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.UnitTest;

/// <summary>
/// Contract tests for issue #86: every asynchronous <see cref="DbEngine"/> command
/// method must generate exactly the same SQL text, parameter collection, batch
/// boundaries, and command count as its synchronous counterpart for the same input.
/// The recording fake permits exactly one execution surface at a time: the
/// synchronous engine runs against a connection whose async members throw, and the
/// asynchronous engine runs against a connection whose sync members throw, so any
/// fallback to the wrong surface fails the test loudly.
/// </summary>
[TestClass]
public class DbEngineAsyncSqlParityTest
{
    [TestMethod]
    public async Task SyncAndAsyncCommandFamily_GenerateIdenticalSqlParametersAndChunkBoundaries()
    {
        var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly);
        var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var syncEngine = new DbEngine(new RecordingExecutor(syncConnection));
        var asyncEngine = new DbEngine(new RecordingExecutor(asyncConnection));
        var tables = new List<TableDefine> { new(typeof(UserTable)) };

        // With the scalar fake reporting 0, every table looks absent, so both
        // initialization paths emit the same create-table DDL sequence and warm
        // their column caches for the entity-based methods below.
        syncConnection.ScalarResult = 0;
        asyncConnection.ScalarResult = 0;
        syncEngine.InitializeTable(tables);
        await asyncEngine.InitializeTableAsync(tables);
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "InitializeTable vs InitializeTableAsync");

        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();
        syncConnection.ScalarResult = 1;
        asyncConnection.ScalarResult = 1;

        // Insert: single record, entity and InputValues. The entity is created
        // once and inserted through both paths so every parameter value is the
        // same object on both sides.
        Assert.AreEqual(1, syncEngine.Insert(UserTable.TableName, Row("1")));
        Assert.AreEqual(1, await asyncEngine.InsertAsync(UserTable.TableName, Row("1")));

        var single = CreateUser("2");
        Assert.AreEqual(1, syncEngine.Insert(single));
        Assert.AreEqual(1, await asyncEngine.InsertAsync(single));

        // Insert: batches whose rows are split by the batch size, proving the
        // chunk boundaries are identical (5 rows at size 2 → 2 + 2 + 1). The
        // fake reports one affected row per command, so the affected count
        // equals the command count on both paths.
        var rows = new List<InputValues> { Row("3"), Row("4"), Row("5"), Row("6"), Row("7") };
        var syncCommandsBeforeBatch = syncConnection.Commands.Count;
        var asyncCommandsBeforeBatch = asyncConnection.Commands.Count;
        Assert.AreEqual(3, syncEngine.Insert(UserTable.TableName, rows, 2));
        Assert.AreEqual(3, await asyncEngine.InsertAsync(UserTable.TableName, rows, 2));
        Assert.AreEqual(3, syncConnection.Commands.Count - syncCommandsBeforeBatch,
            "The sync batch insert must split 5 rows at batch size 2 into 3 commands.");
        Assert.AreEqual(3, asyncConnection.Commands.Count - asyncCommandsBeforeBatch,
            "The async batch insert must split 5 rows at batch size 2 into 3 commands.");

        var entities = new List<User> { CreateUser("8"), CreateUser("9"), CreateUser("10"), CreateUser("11") };
        Assert.AreEqual(2, syncEngine.Insert(entities, 3));
        Assert.AreEqual(2, await asyncEngine.InsertAsync(entities, 3));

        // Update: table form and entity form (one shared entity instance).
        var updateWhere = new WhereConditions();
        updateWhere.Add(UserTable.Id, "1");
        var updateInput = new InputValues().Add(UserTable.UnionId, "renamed");
        Assert.AreEqual(1, syncEngine.Update(UserTable.TableName, updateInput, updateWhere));
        Assert.AreEqual(1, await asyncEngine.UpdateAsync(UserTable.TableName, updateInput, updateWhere));

        var toUpdate = CreateUser("2");
        Assert.AreEqual(1, syncEngine.Update(toUpdate));
        Assert.AreEqual(1, await asyncEngine.UpdateAsync(toUpdate));

        // Delete: condition form and primary-key form.
        Assert.AreEqual(1, syncEngine.Delete(UserTable.TableName, updateWhere));
        Assert.AreEqual(1, await asyncEngine.DeleteAsync(UserTable.TableName, updateWhere));

        Assert.AreEqual(1, syncEngine.Delete<User>("3"));
        Assert.AreEqual(1, await asyncEngine.DeleteAsync<User>("3"));

        // Count with and without the column parameter, and Exist.
        var countWhere = new WhereConditions();
        countWhere.Add(UserTable.UnionId, "row%");
        Assert.AreEqual(syncEngine.Count(UserTable.TableName, countWhere, columns: null),
            await asyncEngine.CountAsync(UserTable.TableName, countWhere, columns: null));
        Assert.AreEqual(syncEngine.Count(UserTable.TableName, countWhere),
            await asyncEngine.CountAsync(UserTable.TableName, countWhere));
        Assert.AreEqual(syncEngine.Exist(UserTable.TableName, countWhere),
            await asyncEngine.ExistAsync(UserTable.TableName, countWhere));

        // Drop.
        syncEngine.Drop(UserTable.TableName);
        await asyncEngine.DropAsync(UserTable.TableName);

        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "the whole command family");

        // Surface purity: the async engine never touched a sync driver member,
        // and the sync engine never touched an async one (each would throw).
        Assert.IsTrue(asyncConnection.OpenedAsync > 0, "The async calls must open connections asynchronously.");
        Assert.AreEqual(0, asyncConnection.OpenedSync, "The async calls must never use the synchronous Open.");
        Assert.IsTrue(syncConnection.OpenedSync > 0, "The sync calls must open connections synchronously.");
        Assert.AreEqual(0, syncConnection.OpenedAsync, "The sync calls must never use OpenAsync.");
    }

    [TestMethod]
    public async Task BatchSplitting_RespectsProviderParameterLimit_IdenticallyOnBothPaths()
    {
        var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly);
        var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var syncEngine = new DbEngine(new RecordingExecutor(syncConnection) { ParameterLimit = 15 });
        var asyncEngine = new DbEngine(new RecordingExecutor(asyncConnection) { ParameterLimit = 15 });

        // Warm the entity column caches through the create-table path so the
        // entity batch reaches the chunking logic on both engines.
        syncConnection.ScalarResult = 0;
        asyncConnection.ScalarResult = 0;
        var tables = new List<TableDefine> { new(typeof(UserTable)) };
        syncEngine.InitializeTable(tables);
        await asyncEngine.InitializeTableAsync(tables);
        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();

        // A user row carries 10 parameters, so a limit of 15 forces one row per
        // command even though the requested batch size is 3.
        var entities = new List<User> { CreateUser("1"), CreateUser("2"), CreateUser("3") };

        Assert.AreEqual(3, syncEngine.Insert(entities, 3));
        Assert.AreEqual(3, await asyncEngine.InsertAsync(entities, 3));

        Assert.AreEqual(3, syncConnection.Commands.Count,
            "The provider parameter limit must split the sync batch into one command per row.");
        Assert.AreEqual(3, asyncConnection.Commands.Count,
            "The provider parameter limit must split the async batch into one command per row.");
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "parameter-limit batch splitting");
    }

    [TestMethod]
    public async Task BatchFailure_WrapsTheOriginalError_IdenticallyOnBothPaths()
    {
        var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly)
        {
            FailNonQueryWithMessage = "simulated provider failure",
        };
        var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly)
        {
            FailNonQueryWithMessage = "simulated provider failure",
        };
        var syncEngine = new DbEngine(new RecordingExecutor(syncConnection));
        var asyncEngine = new DbEngine(new RecordingExecutor(asyncConnection));

        var rows = new List<InputValues> { Row("1"), Row("2") };

        var syncError = Assert.ThrowsExactly<InvalidOperationException>(
            () => syncEngine.Insert(UserTable.TableName, rows, 2));
        var asyncError = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => asyncEngine.InsertAsync(UserTable.TableName, rows, 2));

        Assert.AreEqual(syncError.Message, asyncError.Message,
            "The async batch failure must wrap the original error with the same message.");
        Assert.IsInstanceOfType(syncError.InnerException, typeof(InvalidOperationException));
        Assert.IsInstanceOfType(asyncError.InnerException, typeof(InvalidOperationException));
    }

    [TestMethod]
    public async Task PreCancelledToken_ThrowsBeforeAnyDatabaseCall()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var engine = new DbEngine(new RecordingExecutor(connection));
        var cancelled = new CancellationToken(canceled: true);
        var where = new WhereConditions();
        where.Add(UserTable.Id, "1");

        // Warm the entity column cache through the create-table path so the
        // entity-based methods reach the token check; the commands opened for
        // the warm-up form the baseline the canceled calls must not extend.
        connection.ScalarResult = 0;
        await engine.InitializeTableAsync([new TableDefine(typeof(UserTable))]);
        connection.Commands.Clear();
        var openedAsync = connection.OpenedAsync;
        var begunTransactions = connection.BegunTransactions;

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.InsertAsync(UserTable.TableName, Row("1"), cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.InsertAsync(UserTable.TableName, new List<InputValues> { Row("2") }, 2,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.InsertAsync(CreateUser("3"), cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.InsertAsync(new List<User> { CreateUser("4") }, 2, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.UpdateAsync(UserTable.TableName,
                new InputValues().Add(UserTable.UnionId, "renamed"), where,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.UpdateAsync(CreateUser("5"), cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.DeleteAsync(UserTable.TableName, where, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.DeleteAsync<User>("6", cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.CountAsync(UserTable.TableName, where, columns: null, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.CountAsync(UserTable.TableName, where, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.ExistAsync(UserTable.TableName, where, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.DropAsync(UserTable.TableName, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.InitializeTableAsync([new TableDefine(typeof(UserTable))], cancelled));

        Assert.AreEqual(openedAsync, connection.OpenedAsync,
            "A canceled call must not open a connection.");
        Assert.AreEqual(0, connection.OpenedSync,
            "A canceled call must not open a connection synchronously.");
        Assert.AreEqual(0, connection.Commands.Count, "A canceled call must not execute a command.");
        Assert.AreEqual(begunTransactions, connection.BegunTransactions,
            "A canceled call must not begin a transaction.");
    }

    private static User CreateUser(string id)
    {
        return new User
        {
            Id = id,
            UnionId = "user" + id,
            RegistrationDate = DateTime.Now,
            IntData = int.TryParse(id, out var value) ? value : 0,
            Data = [1, 2, 3],
            DoubleData = 123.45,
            DecimalData = 123.456m,
            ShortData = (short)123,
            BoolData = true,
            SpecialString = null
        };
    }

    private static InputValues Row(string id)
    {
        return new InputValues()
            .Add(UserTable.Id, id)
            .Add(UserTable.UnionId, "user" + id)
            .Add(UserTable.DoubleData, 0d)
            .Add(UserTable.DecimalData, 0m)
            .Add(UserTable.ShortData, (short)0)
            .Add(UserTable.IntData, 0)
            .Add(UserTable.BoolData, false);
    }

    private static void AssertIdenticalCommands(
        IReadOnlyList<RecordedCommand> sync, IReadOnlyList<RecordedCommand> asyncCommands, string label)
    {
        Assert.HasCount(sync.Count, asyncCommands,
            $"The command counts must match for {label}.");
        for (var i = 0; i < sync.Count; i++)
        {
            Assert.AreEqual(sync[i].CommandText, asyncCommands[i].CommandText,
                $"command text of command #{i} must match for {label}");
            Assert.AreEqual(sync[i].CommandType, asyncCommands[i].CommandType,
                $"command type of command #{i} must match for {label}");
            Assert.HasCount(sync[i].Parameters.Count, asyncCommands[i].Parameters,
                $"parameter count of command #{i} must match for {label}");
            for (var parameter = 0; parameter < sync[i].Parameters.Count; parameter++)
            {
                Assert.AreEqual(sync[i].Parameters[parameter].ParameterName,
                    asyncCommands[i].Parameters[parameter].ParameterName,
                    $"name of parameter #{parameter} of command #{i} must match for {label}");
                Assert.AreEqual(sync[i].Parameters[parameter].Value,
                    asyncCommands[i].Parameters[parameter].Value,
                    $"value of parameter #{parameter} of command #{i} must match for {label}");
            }
        }
    }

    private enum ExecutionSurface
    {
        SyncOnly,
        AsyncOnly
    }

    private class RecordingExecutor : DbExecutor
    {
        private readonly RecordingConnection _connection;

        public RecordingExecutor(RecordingConnection connection)
            : base("recording")
        {
            _connection = connection;
        }

        /// <summary>Overrides the provider parameter limit; int.MaxValue when unset.</summary>
        public int ParameterLimit { get; set; } = int.MaxValue;

        public override int MaxParametersPerCommand => ParameterLimit;

        public override DbConnection GetConnection() => _connection;

        protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
        {
            return (name, value) =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
                return parameter;
            };
        }

        protected override IDataAdapter GetAdapter(IDbCommand command)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingConnection : DbConnection
    {
        private readonly ExecutionSurface _surface;
        private ConnectionState _state;

        public RecordingConnection(ExecutionSurface surface)
        {
            _surface = surface;
        }

        public List<RecordedCommand> Commands { get; } = [];

        /// <summary>Result returned by the fake scalar members.</summary>
        public object ScalarResult { get; set; } = 1;

        /// <summary>When set, the fake non-query members throw this message instead of executing.</summary>
        public string FailNonQueryWithMessage { get; set; }

        public int OpenedSync { get; private set; }

        public int OpenedAsync { get; private set; }

        public int Closed { get; private set; }

        public int BegunTransactions => Transactions.Count;

        public List<RecordingTransaction> Transactions { get; } = [];

        public override string ConnectionString { get; set; } = "recording";
        public override string Database => "recording";
        public override string DataSource => "recording";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close()
        {
            _state = ConnectionState.Closed;
            Closed++;
        }

        public override void Open()
        {
            if (_surface == ExecutionSurface.AsyncOnly)
            {
                throw new NotSupportedException("The synchronous Open must not be used.");
            }

            OpenedSync++;
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            if (_surface == ExecutionSurface.SyncOnly)
            {
                throw new NotSupportedException("The asynchronous OpenAsync must not be used.");
            }

            OpenedAsync++;
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            var transaction = new RecordingTransaction(this, isolationLevel);
            Transactions.Add(transaction);
            return transaction;
        }

        protected override DbCommand CreateDbCommand() => new RecordingCommand(this, _surface);
    }

    private sealed class RecordingTransaction(RecordingConnection connection, IsolationLevel isolationLevel)
        : DbTransaction
    {
        public int Commits { get; private set; }

        public int Rollbacks { get; private set; }

        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        public override void Commit() => Commits++;

        public override void Rollback() => Rollbacks++;
    }

    private sealed class RecordingCommand(RecordingConnection connection, ExecutionSurface surface) : DbCommand
    {
        private readonly RecordingParameterCollection _parameters = new();
        private bool _disposed;

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction DbTransaction { get; set; }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

        public override void Cancel() { }

        public override int ExecuteNonQuery()
        {
            ForbidAsyncSurface();
            Record();
            ThrowIfConfigured();
            return 1;
        }

        public override object ExecuteScalar()
        {
            ForbidAsyncSurface();
            Record();
            return connection.ScalarResult;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            ForbidAsyncSurface();
            Record();
            return CreateReader();
        }

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            await ForbidSyncSurface();
            Record();
            ThrowIfConfigured();
            return 1;
        }

        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            await ForbidSyncSurface();
            Record();
            return connection.ScalarResult;
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior, CancellationToken cancellationToken)
        {
            await ForbidSyncSurface();
            Record();
            return CreateReader();
        }

        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new RecordingParameter();

        private void ForbidAsyncSurface()
        {
            if (surface == ExecutionSurface.AsyncOnly)
            {
                throw new NotSupportedException("The synchronous execution members must not be used.");
            }
        }

        private async Task ForbidSyncSurface()
        {
            if (surface == ExecutionSurface.SyncOnly)
            {
                throw new NotSupportedException("The asynchronous execution members must not be used.");
            }

            // Yield once so every fake async execution completes genuinely
            // asynchronously: if the engine disposes the command before the
            // execution task finishes, Record() below fails loudly.
            await Task.Yield();
        }

        private static DbDataReader CreateReader()
        {
            var table = new DataTable();
            table.Columns.Add("value", typeof(string));
            table.Rows.Add("first");
            table.Rows.Add("second");
            return table.CreateDataReader();
        }

        private void ThrowIfConfigured()
        {
            if (connection.FailNonQueryWithMessage is { } message)
            {
                throw new InvalidOperationException(message);
            }
        }

        private void Record()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(RecordingCommand),
                    "The command was disposed before its asynchronous execution completed.");
            }

            connection.Commands.Add(new RecordedCommand(
                CommandText,
                CommandType,
                _parameters.Cast<DbParameter>().ToList(),
                DbTransaction));
        }
    }

    private sealed class RecordingParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; }

        public override void ResetDbType() { }
    }

    private sealed class RecordingParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(parameter => parameter.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) =>
            _parameters[IndexOf(parameterName)] = value;
    }

    private sealed record RecordedCommand(
        string CommandText,
        CommandType CommandType,
        IReadOnlyList<DbParameter> Parameters,
        DbTransaction Transaction);
}
