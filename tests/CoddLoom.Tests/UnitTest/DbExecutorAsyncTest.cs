using CoddLoom.Params;
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
/// Acceptance tests for issue #85: the true-async execution layer of
/// <see cref="DbExecutor"/>. The recording fake permits exactly one execution
/// surface at a time: while a test drives the async methods, the synchronous
/// <c>Open</c>/<c>Execute</c> members throw, so any fallback to the
/// synchronous driver API fails the test loudly. The reverse holds for the
/// synchronous-parity assertions.
/// </summary>
[TestClass]
public class DbExecutorAsyncTest
{
    private static string ReadString(IDataRecord record) => record.GetString(0);

    [TestMethod]
    public async Task AsyncMethods_AwaitDriverAsyncApis_AndNeverUseSyncMembers()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);

        await executor.ValidateConnectionAsync();
        await executor.ExecuteAsync(_ => Task.CompletedTask);
        Assert.AreEqual(42, await executor.ExecuteAsync(_ => Task.FromResult(42)));
        await executor.TransactionAsync(_ => Task.CompletedTask);
        Assert.AreEqual("committed", await executor.TransactionAsync(_ => Task.FromResult("committed")));

        Assert.AreEqual(1, await executor.NonQueryAsync("UPDATE demo SET a = 1"));
        Assert.AreEqual(1, await executor.NonQueryProcedureAsync("reset_demo"));
        CollectionAssert.AreEqual(new[] { "first", "second" },
            await executor.ReaderAsync("SELECT value FROM demo", ReadString));
        CollectionAssert.AreEqual(new[] { "first", "second" },
            await executor.ReaderProcedureAsync("list_demo", ReadString));
        Assert.AreEqual(1, await executor.ScalarAsync("SELECT 1", System.Convert.ToInt32));
        Assert.AreEqual(1, await executor.ScalarProcedureAsync("count_demo", System.Convert.ToInt32));

        // The six command methods plus the five connection-scoped methods each
        // open exactly one connection of their own, asynchronously.
        Assert.AreEqual(11, connection.OpenedAsync, "Every async entry point must open its own connection.");
        Assert.AreEqual(11, connection.Closed, "Every opened connection must be closed.");
        Assert.AreEqual(0, connection.OpenedSync, "The synchronous Open must never be used.");
        Assert.AreEqual(6, connection.Commands.Count);
        Assert.AreEqual(2, connection.BegunTransactions, "Both TransactionAsync calls must begin a transaction.");
    }

    [TestMethod]
    public async Task SyncAndAsyncPaths_GenerateIdenticalSqlAndParameters()
    {
        var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly);
        var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var sync = new RecordingExecutor(syncConnection);
        var asyncExecutor = new RecordingExecutor(asyncConnection);
        var parameters = new List<ValueParam>
        {
            new(2, "id"),
            new("two", "name")
        };

        // Same inputs through both paths; the fakes allow only their own
        // surface, so both halves also re-prove surface purity.
        Assert.AreEqual(1, sync.NonQuery("UPDATE demo SET a = @id WHERE name = @name", parameters));
        Assert.AreEqual(1, await asyncExecutor.NonQueryAsync("UPDATE demo SET a = @id WHERE name = @name", parameters));

        Assert.AreEqual(1, sync.NonQueryProcedure("reset_demo", parameters));
        Assert.AreEqual(1, await asyncExecutor.NonQueryProcedureAsync("reset_demo", parameters));

        CollectionAssert.AreEqual(new[] { "first", "second" },
            sync.Reader("SELECT value FROM demo WHERE id = @id", ReadString, parameters));
        CollectionAssert.AreEqual(new[] { "first", "second" },
            await asyncExecutor.ReaderAsync("SELECT value FROM demo WHERE id = @id", ReadString, parameters));

        CollectionAssert.AreEqual(new[] { "first", "second" },
            sync.ReaderProcedure("list_demo", ReadString, parameters));
        CollectionAssert.AreEqual(new[] { "first", "second" },
            await asyncExecutor.ReaderProcedureAsync("list_demo", ReadString, parameters));

        Assert.AreEqual(1, sync.Scalar<int>("SELECT a FROM demo WHERE name = @name", System.Convert.ToInt32, parameters));
        Assert.AreEqual(1, await asyncExecutor.ScalarAsync<int>("SELECT a FROM demo WHERE name = @name", System.Convert.ToInt32, parameters));

        Assert.AreEqual(1, sync.ScalarProcedure<int>("count_demo", System.Convert.ToInt32, parameters));
        Assert.AreEqual(1, await asyncExecutor.ScalarProcedureAsync<int>("count_demo", System.Convert.ToInt32, parameters));

        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands);
    }

    [TestMethod]
    public async Task CancelledToken_ThrowsBeforeAnyDatabaseCall()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.NonQueryAsync("UPDATE demo SET a = 1", cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.NonQueryProcedureAsync("reset_demo", cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ReaderAsync("SELECT value FROM demo", ReadString, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ReaderProcedureAsync("list_demo", ReadString, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ScalarAsync("SELECT 1", System.Convert.ToInt32, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ScalarProcedureAsync("count_demo", System.Convert.ToInt32, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.TransactionAsync(_ => Task.CompletedTask, cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.TransactionAsync<int>(_ => Task.FromResult(1), cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ExecuteAsync(_ => Task.CompletedTask, cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ExecuteAsync<int>(_ => Task.FromResult(1), cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ValidateConnectionAsync(cancelled));

        Assert.AreEqual(0, connection.OpenedAsync, "A canceled call must not open a connection.");
        Assert.AreEqual(0, connection.OpenedSync, "A canceled call must not open a connection synchronously.");
        Assert.AreEqual(0, connection.Commands.Count, "A canceled call must not execute a command.");
        Assert.AreEqual(0, connection.BegunTransactions, "A canceled call must not begin a transaction.");
    }

    [TestMethod]
    public async Task CancelledToken_ThrowsBeforeCommandExecution_OnCallerOwnedConnectionAndTransaction()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);
        var cancelled = new CancellationToken(canceled: true);
        await connection.OpenAsync(CancellationToken.None);
        using var transaction = connection.BeginTransaction();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.NonQueryAsync("UPDATE demo SET a = 1", con: connection, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.NonQueryAsync("UPDATE demo SET a = 1", tran: transaction, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => executor.ReaderAsync("SELECT value FROM demo", ReadString, tran: transaction, cancellationToken: cancelled));

        Assert.AreEqual(1, connection.OpenedAsync, "Only the explicit setup open may have happened.");
        Assert.AreEqual(0, connection.Commands.Count, "A canceled call must not execute a command.");
    }

    [TestMethod]
    public async Task InFlightCancellation_PropagatesTheDriverException_AndStillClosesTheConnection()
    {
        // The fake command pauses inside its driver-level async member; the
        // bounded delay keeps the test self-terminating even if a regression
        // stops forwarding the token.
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly)
        {
            ExecutionDelay = TimeSpan.FromSeconds(30)
        };
        var executor = new RecordingExecutor(connection);

        using (var scalarCts = new CancellationTokenSource())
        {
            var scalar = executor.ScalarAsync("SELECT 1", System.Convert.ToInt32,
                cancellationToken: scalarCts.Token);
            scalarCts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => scalar);
        }

        using (var readerCts = new CancellationTokenSource())
        {
            var reader = executor.ReaderAsync("SELECT value FROM demo", ReadString,
                cancellationToken: readerCts.Token);
            readerCts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => reader);
        }

        Assert.AreEqual(2, connection.OpenedAsync);
        Assert.AreEqual(2, connection.Closed, "A canceled execution must still close its connection.");
    }

    [TestMethod]
    public async Task CommandMethods_RespectConnectionOwnership_LikeTheSyncVersions()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);

        // Own connection: open and close one dedicated connection.
        Assert.AreEqual(1, await executor.NonQueryAsync("UPDATE demo SET a = 1"));
        Assert.AreEqual(1, connection.OpenedAsync);
        Assert.AreEqual(1, connection.Closed);

        // Caller-owned connection: no additional open by the executor.
        CollectionAssert.AreEqual(new[] { "first", "second" },
            await executor.ReaderAsync("SELECT value FROM demo", ReadString, con: connection));
        Assert.AreEqual(1, connection.OpenedAsync, "A supplied connection must not be opened again.");
        Assert.AreEqual(1, connection.Closed);

        // Caller-owned transaction: the command binds to that transaction.
        await connection.OpenAsync(CancellationToken.None);
        using var transaction = connection.BeginTransaction();
        Assert.AreEqual(1, await executor.ScalarAsync("SELECT 1", System.Convert.ToInt32, tran: transaction));
        Assert.AreSame(transaction, connection.Commands.Last().Transaction);
        Assert.AreEqual(1, connection.BegunTransactions);
        Assert.AreEqual(2, connection.OpenedAsync, "Only the explicit setup open may have been added.");
    }

    [TestMethod]
    public async Task AsyncCommands_ValidateArgumentsAndParameterLimit_LikeSyncVersions()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);
        var limited = new SmallLimitRecordingExecutor(connection);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => executor.NonQueryAsync(null));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => executor.ReaderAsync("", ReadString));
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => executor.ReaderAsync("SELECT 1", ReadString, new ValueParam[] { null }));

        var parameters = new List<ValueParam>
        {
            new(1, "one"),
            new(2, "two"),
            new(3, "three")
        };
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => limited.NonQueryAsync("SELECT 1", parameters));
        StringAssert.Contains(exception.Message, "provider limit of 2 parameters");

        Assert.AreEqual(0, connection.Commands.Count, "Validation failures must not execute anything.");
    }

    [TestMethod]
    public async Task TransactionAsync_CommitsRollsBackAndReturnsValues_LikeTheSyncVersion()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);

        var received = new List<DbTransaction>();
        await executor.TransactionAsync(tran =>
        {
            received.Add(tran);
            return Task.CompletedTask;
        });
        Assert.AreEqual(1, connection.Transactions.Single().Commits);
        Assert.AreEqual(0, connection.Transactions.Single().Rollbacks);
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual(1, connection.Closed);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => executor.TransactionAsync(_ => throw new InvalidOperationException("intentional")));
        Assert.AreEqual(1, connection.Transactions[1].Rollbacks);
        Assert.AreEqual(0, connection.Transactions[1].Commits);
        Assert.AreEqual(2, connection.Closed);

        Assert.AreEqual(7, await executor.TransactionAsync(tran => Task.FromResult(7)));
        Assert.AreEqual(1, connection.Transactions[2].Commits);
    }

    [TestMethod]
    public async Task TransactionAsync_KeepsSyncRollbackMaskingSemantics()
    {
        // The sync Transaction lets a rollback failure mask the original
        // exception (issue #77 tracks the shared fix); TransactionAsync must
        // reproduce that behavior exactly instead of diverging.
        var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly);
        var syncExecutor = new RecordingExecutor(syncConnection);
        var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var asyncExecutor = new RecordingExecutor(asyncConnection);

        Assert.ThrowsExactly<ApplicationException>(() => syncExecutor.Transaction(tran =>
        {
            ((RecordingTransaction)tran).ThrowOnRollback = true;
            throw new InvalidOperationException("original");
        }));

        await Assert.ThrowsExactlyAsync<ApplicationException>(() => asyncExecutor.TransactionAsync(tran =>
        {
            ((RecordingTransaction)tran).ThrowOnRollback = true;
            throw new InvalidOperationException("original");
        }));
    }

    [TestMethod]
    public async Task ExecuteAsync_PassesOpenConnection_AndClosesItEvenOnFailure()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);

        var observedStates = new List<ConnectionState>();
        await executor.ExecuteAsync(con =>
        {
            observedStates.Add(con.State);
            return Task.CompletedTask;
        });
        Assert.AreEqual(1, observedStates.Count);
        Assert.AreEqual(ConnectionState.Open, observedStates[0]);
        Assert.AreEqual(1, connection.Closed);

        Assert.AreEqual(ConnectionState.Open, await executor.ExecuteAsync(con => Task.FromResult(con.State)));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => executor.ExecuteAsync(_ => throw new InvalidOperationException("intentional")));
        Assert.AreEqual(3, connection.Closed, "The connection must be closed after every callback failure.");
    }

    [TestMethod]
    public async Task ScalarAsync_MapsNullAndDbNullToDefault_OtherwiseConverts()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var executor = new RecordingExecutor(connection);

        connection.ScalarResult = null;
        Assert.AreEqual(0, await executor.ScalarAsync("SELECT a FROM demo WHERE id = 99", System.Convert.ToInt32));

        connection.ScalarResult = DBNull.Value;
        Assert.IsNull(await executor.ScalarAsync<int?>("SELECT a FROM demo WHERE id = 99", value => (int?)value));

        connection.ScalarResult = 7;
        Assert.AreEqual(7, await executor.ScalarAsync("SELECT a FROM demo", System.Convert.ToInt32));
    }

    private static void AssertIdenticalCommands(
        IReadOnlyList<RecordedCommand> sync, IReadOnlyList<RecordedCommand> asyncCommands)
    {
        Assert.HasCount(sync.Count, asyncCommands);
        for (var i = 0; i < sync.Count; i++)
        {
            Assert.AreEqual(sync[i].CommandText, asyncCommands[i].CommandText, $"command text of command #{i}");
            Assert.AreEqual(sync[i].CommandType, asyncCommands[i].CommandType, $"command type of command #{i}");
            Assert.HasCount(sync[i].Parameters.Count, asyncCommands[i].Parameters);
            for (var parameter = 0; parameter < sync[i].Parameters.Count; parameter++)
            {
                Assert.AreEqual(sync[i].Parameters[parameter].ParameterName,
                    asyncCommands[i].Parameters[parameter].ParameterName,
                    $"name of parameter #{parameter} of command #{i}");
                Assert.AreEqual(sync[i].Parameters[parameter].Value,
                    asyncCommands[i].Parameters[parameter].Value,
                    $"value of parameter #{parameter} of command #{i}");
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

    private sealed class SmallLimitRecordingExecutor : RecordingExecutor
    {
        public SmallLimitRecordingExecutor(RecordingConnection connection)
            : base(connection)
        {
        }

        public override int MaxParametersPerCommand => 2;
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

        public List<RecordingTransaction> Transactions { get; } = [];

        /// <summary>Result returned by the fake scalar members.</summary>
        public object ScalarResult { get; set; } = 1;

        /// <summary>When set, the fake async execution members await this delay.</summary>
        public TimeSpan? ExecutionDelay { get; set; }

        public int OpenedSync { get; private set; }

        public int OpenedAsync { get; private set; }

        public int Closed { get; private set; }

        public int BegunTransactions => Transactions.Count;

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

        public bool ThrowOnRollback { get; set; }

        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        public override void Commit() => Commits++;

        public override void Rollback()
        {
            if (ThrowOnRollback)
            {
                throw new ApplicationException("rollback failed");
            }

            Rollbacks++;
        }
    }

    private sealed class RecordingCommand(RecordingConnection connection, ExecutionSurface surface) : DbCommand
    {
        private readonly RecordingParameterCollection _parameters = new();

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel() { }

        public override int ExecuteNonQuery()
        {
            ForbidAsyncSurface();
            Record();
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
            await ForbidSyncSurface(cancellationToken);
            Record();
            return 1;
        }

        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            await ForbidSyncSurface(cancellationToken);
            Record();
            return connection.ScalarResult;
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior, CancellationToken cancellationToken)
        {
            await ForbidSyncSurface(cancellationToken);
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

        private async Task ForbidSyncSurface(CancellationToken cancellationToken)
        {
            if (surface == ExecutionSurface.SyncOnly)
            {
                throw new NotSupportedException("The asynchronous execution members must not be used.");
            }

            if (connection.ExecutionDelay is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        private static DbDataReader CreateReader()
        {
            var table = new DataTable();
            table.Columns.Add("value", typeof(string));
            table.Rows.Add("first");
            table.Rows.Add("second");
            return table.CreateDataReader();
        }

        private void Record()
        {
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
