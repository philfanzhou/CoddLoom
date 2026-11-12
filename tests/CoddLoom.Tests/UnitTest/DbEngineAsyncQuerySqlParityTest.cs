using CoddLoom.Condition;
using CoddLoom.MariaDb;
using CoddLoom.MySql;
using CoddLoom.Oracle;
using CoddLoom.Params;
using CoddLoom.PostgreSql;
using CoddLoom.Sql;
using CoddLoom.SqlServer;
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
/// Contract tests for issue #87: every asynchronous <see cref="DbEngine"/> query
/// method must generate exactly the same SQL text, parameter collection, command
/// count, and command order as its synchronous counterpart for the same input,
/// across the JOIN overloads and every provider pagination dialect including the
/// SQL Server automatic-order-by paths. The recording fake permits exactly one
/// execution surface at a time: the synchronous engine runs against a connection
/// whose async members throw, and the asynchronous engine runs against a
/// connection whose sync members throw, so any fallback to the wrong surface
/// fails the test loudly.
/// </summary>
[TestClass]
public class DbEngineAsyncQuerySqlParityTest
{
    [TestMethod]
    public async Task SyncAndAsyncQueryFamily_GenerateIdenticalSqlParametersAndCommandSequences()
    {
        var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly);
        var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var syncEngine = new DbEngine(new RecordingExecutor(syncConnection));
        var asyncEngine = new DbEngine(new RecordingExecutor(asyncConnection));
        var tables = new List<TableDefine> { new(typeof(UserTable)) };

        // Warm the entity column caches through the create-table path so the
        // entity-based query methods can map; the DDL sequences must match too.
        syncConnection.ScalarResult = 0;
        asyncConnection.ScalarResult = 0;
        syncEngine.InitializeTable(tables);
        await asyncEngine.InitializeTableAsync(tables);
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "InitializeTable vs InitializeTableAsync");

        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();
        syncConnection.ScalarResult = 5;
        asyncConnection.ScalarResult = 5;

        var where = new WhereConditions();
        where.Add(UserTable.UnionId, "parity%", WhereOperator.Like);
        var orderBy = new OrderByCondition(UserTable.IntData);
        var columns = new ColumnParam();
        columns.AddSelect(UserTable.Id);
        columns.AddSelect(UserTable.UnionId);
        var page = new PageParam { PageNumber = 2, PageSize = 2 };
        var join = new JoinConditions(
            UserTable.TableName, UserTable.Id, "ParityOther", "userId");

        // Select: all six overloads. Every overload runs on both engines with
        // the same inputs, so the generated command streams must be identical.
        await AssertSelectsMatchAsync(syncEngine, asyncEngine, where, orderBy, columns, join);
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "the Select overloads");

        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();

        // First: all three overloads; the page-of-one generation must match.
        Assert.AreEqual(
            syncEngine.First(MapUnionId, UserTable.TableName, where, orderBy, columns),
            await asyncEngine.FirstAsync(MapUnionId, UserTable.TableName, where, orderBy, columns));
        Assert.AreEqual(
            syncEngine.First(MapUnionId, UserTable.TableName, where, orderBy),
            await asyncEngine.FirstAsync(MapUnionId, UserTable.TableName, where, orderBy));
        Assert.AreEqual(
            syncEngine.First<User>(where, orderBy).UnionId,
            (await asyncEngine.FirstAsync<User>(where, orderBy)).UnionId);
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "the First overloads");

        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();

        // SelectById: primary-key where clause must match. The fake reader must
        // return a single row because SelectById applies Single().
        syncConnection.ReaderRowCount = 1;
        asyncConnection.ReaderRowCount = 1;
        Assert.AreEqual(
            syncEngine.SelectById<User>("parity-1").UnionId,
            (await asyncEngine.SelectByIdAsync<User>("parity-1")).UnionId);
        syncConnection.ReaderRowCount = 2;
        asyncConnection.ReaderRowCount = 2;
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "SelectById vs SelectByIdAsync");

        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();

        // PageSelect: all four overloads. The COUNT-then-SELECT command pair,
        // including its order and parameters, must match byte for byte, and the
        // returned metadata must equal the synchronous out results.
        var syncPage1 = syncEngine.PageSelect(MapUnionId, UserTable.TableName, where, orderBy,
            columns, page, out var syncPages1, out var syncCount1);
        var asyncPage1 = await asyncEngine.PageSelectAsync(MapUnionId, UserTable.TableName,
            where, orderBy, columns, page);
        var syncPage2 = syncEngine.PageSelect(MapUnionId, UserTable.TableName, where, orderBy,
            page, out var syncPages2, out var syncCount2);
        var asyncPage2 = await asyncEngine.PageSelectAsync(MapUnionId, UserTable.TableName,
            where, orderBy, page);
        var syncPage3 = syncEngine.PageSelect<User>(where, orderBy, columns, page,
            out var syncPages3, out var syncCount3);
        var asyncPage3 = await asyncEngine.PageSelectAsync<User>(where, orderBy, columns, page);
        var syncPage4 = syncEngine.PageSelect<User>(where, orderBy, page,
            out var syncPages4, out var syncCount4);
        var asyncPage4 = await asyncEngine.PageSelectAsync<User>(where, orderBy, page);

        Assert.AreEqual((syncPages1, syncCount1), (asyncPage1.TotalPage, asyncPage1.TotalCount));
        Assert.AreEqual((syncPages2, syncCount2), (asyncPage2.TotalPage, asyncPage2.TotalCount));
        Assert.AreEqual((syncPages3, syncCount3), (asyncPage3.TotalPage, asyncPage3.TotalCount));
        Assert.AreEqual((syncPages4, syncCount4), (asyncPage4.TotalPage, asyncPage4.TotalCount));
        CollectionAssert.AreEqual(syncPage1, asyncPage1.Items);
        CollectionAssert.AreEqual(syncPage2, asyncPage2.Items);
        Assert.AreEqual(syncPage3.Count, asyncPage3.Items.Count);
        Assert.AreEqual(syncPage3[0].UnionId, asyncPage3.Items[0].UnionId);
        Assert.AreEqual(syncPage4.Count, asyncPage4.Items.Count);
        Assert.AreEqual(syncPage4[0].UnionId, asyncPage4.Items[0].UnionId);
        Assert.AreEqual(2, asyncPage1.PageNumber, "The async result should echo the page number.");
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "the PageSelect overloads");

        syncConnection.Commands.Clear();
        asyncConnection.Commands.Clear();

        // A caller-supplied connection and transaction must be forwarded
        // identically: both engines bind the same recording transaction to
        // every command and neither engine opens a connection itself.
        var syncTran = (RecordingTransaction)syncConnection.BeginTransaction();
        var asyncTran = (RecordingTransaction)asyncConnection.BeginTransaction();
        var tranWhere = new WhereConditions(UserTable.Id, "tran-1");
        var tranOrder = new OrderByCondition(UserTable.Id);

        Assert.AreEqual(
            syncEngine.First(MapUnionId, UserTable.TableName, tranWhere, tranOrder,
                con: syncConnection, tran: syncTran),
            await asyncEngine.FirstAsync(MapUnionId, UserTable.TableName, tranWhere, tranOrder,
                con: asyncConnection, tran: asyncTran));
        await AssertSelectsMatchAsync(syncEngine, asyncEngine, where, orderBy, columns, join,
            con1: syncConnection, tran1: syncTran, con2: asyncConnection, tran2: asyncTran);
        AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
            "connection/transaction forwarding");
        Assert.IsTrue(syncConnection.Commands.All(command => command.Transaction == syncTran),
            "Every synchronous command must bind the caller's transaction.");
        Assert.IsTrue(asyncConnection.Commands.All(command => command.Transaction == asyncTran),
            "Every asynchronous command must bind the caller's transaction.");

        // Surface purity: the async engine never touched a sync driver member,
        // and the sync engine never touched an async one (each would throw).
        Assert.IsTrue(asyncConnection.OpenedAsync > 0, "The async calls must open connections asynchronously.");
        Assert.AreEqual(0, asyncConnection.OpenedSync, "The async calls must never use the synchronous Open.");
        Assert.IsTrue(syncConnection.OpenedSync > 0, "The sync calls must open connections synchronously.");
        Assert.AreEqual(0, syncConnection.OpenedAsync, "The sync calls must never use OpenAsync.");
    }

    [TestMethod]
    public async Task PageSelectAsync_IssuesCountThenSelectAsTwoSeparateNonAtomicCommands()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var engine = new DbEngine(new RecordingExecutor(connection));
        connection.ScalarResult = 7;
        var where = new WhereConditions();
        where.Add(UserTable.UnionId, "two-command%", WhereOperator.Like);
        var page = new PageParam { PageNumber = 2, PageSize = 2 };

        var result = await engine.PageSelectAsync(
            MapUnionId, UserTable.TableName, where, new OrderByCondition(UserTable.IntData),
            null, page);

        // Exactly two commands, COUNT first and SELECT second, in order.
        Assert.HasCount(2, connection.Commands,
            "A paged query must run exactly one COUNT and one SELECT.");
        StringAssert.Contains(connection.Commands[0].CommandText, "COUNT");
        StringAssert.Contains(connection.Commands[1].CommandText, "SELECT");
        Assert.IsFalse(connection.Commands[0].CommandText.Contains("LIMIT"),
            "The COUNT command must not be paged.");

        // The metadata comes from the COUNT; the items from the SELECT.
        Assert.AreEqual(7, result.TotalCount, "The total count must come from the COUNT command.");
        Assert.AreEqual(4, result.TotalPage, "Seven rows at two per page should produce four pages.");
        Assert.AreEqual(2, result.PageNumber);
        Assert.HasCount(2, result.Items, "The fake reader returns two rows.");

        // Without a caller connection each command opens its own connection:
        // two separate, non-atomic commands, exactly like the synchronous
        // version.
        Assert.AreEqual(2, connection.OpenedAsync,
            "Each of the two commands must open its own connection.");
        Assert.AreEqual(0, connection.OpenedSync);
    }

    [TestMethod]
    public async Task PageSelectAsync_WithoutPageParam_RunsOnlyTheSelect()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var engine = new DbEngine(new RecordingExecutor(connection));
        var where = new WhereConditions();
        where.Add(UserTable.UnionId, "unpaged%", WhereOperator.Like);

        var result = await engine.PageSelectAsync(
            MapUnionId, UserTable.TableName, where, new OrderByCondition(UserTable.IntData),
            columns: null, pageParam: null);

        Assert.HasCount(1, connection.Commands,
            "A null page param must skip the COUNT command.");
        StringAssert.Contains(connection.Commands.Single().CommandText, "SELECT");
        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.TotalPage);
        Assert.AreEqual(0, result.PageNumber);
        Assert.AreEqual(1, connection.OpenedAsync);
    }

    [TestMethod]
    public async Task PaginationDialects_GenerateIdenticalSqlOnBothPaths_IncludingSqlServerAutoOrdering()
    {
        var builders = new (string Label, Func<SqlBuilder> Create)[]
        {
            ("Sqlite/Default", () => new SqlBuilder()),
            ("MySql", () => new MySqlBuilder()),
            ("MariaDb", () => new MariaDbBuilder()),
            ("SqlServer", () => new SqlServerBuilder()),
            ("Oracle", () => new OracleBuilder()),
            ("PostgreSql", () => new PostgreSqlBuilder()),
        };
        var expectedPagination = new Dictionary<string, string>
        {
            ["Sqlite/Default"] = "LIMIT 20,10",
            ["MySql"] = "LIMIT 20,10",
            ["MariaDb"] = "LIMIT 20,10",
            ["SqlServer"] = "OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY",
            ["Oracle"] = "OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY",
            ["PostgreSql"] = "LIMIT 10 OFFSET 20",
        };

        foreach (var (label, createBuilder) in builders)
        {
            var syncConnection = new RecordingConnection(ExecutionSurface.SyncOnly);
            var asyncConnection = new RecordingConnection(ExecutionSurface.AsyncOnly);
            var syncEngine = new DbEngine(new RecordingExecutor(syncConnection, createBuilder()));
            var asyncEngine = new DbEngine(new RecordingExecutor(asyncConnection, createBuilder()));
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "dialect%", WhereOperator.Like);
            var orderBy = new OrderByCondition(UserTable.IntData);
            var page = new PageParam { PageNumber = 3, PageSize = 10 };

            // With an explicit order by, both paths must emit the provider's
            // pagination dialect.
            syncEngine.PageSelect(MapUnionId, UserTable.TableName, where, orderBy, null, page,
                out _, out _);
            await asyncEngine.PageSelectAsync(
                MapUnionId, UserTable.TableName, where, orderBy, null, page);
            AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
                $"{label} ordered pagination");
            StringAssert.Contains(asyncConnection.Commands[1].CommandText,
                expectedPagination[label],
                $"The {label} async path must use the provider pagination dialect.");

            syncConnection.Commands.Clear();
            asyncConnection.Commands.Clear();

            // Without an order by but with a condition, SQL Server derives the
            // ordering from the first where column; every provider must stay
            // identical between the two paths.
            syncEngine.PageSelect(MapUnionId, UserTable.TableName, where, null, null, page,
                out _, out _);
            await asyncEngine.PageSelectAsync(
                MapUnionId, UserTable.TableName, where, null, null, page);
            AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
                $"{label} condition-derived pagination");
            if (label == "SqlServer")
            {
                StringAssert.Contains(asyncConnection.Commands[1].CommandText,
                    "ORDER BY unionId ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY",
                    "The SQL Server async path must derive the ordering from the first where column.");
            }

            syncConnection.Commands.Clear();
            asyncConnection.Commands.Clear();

            // With neither order by nor condition, SQL Server supplies its
            // constant automatic ordering on both paths.
            syncEngine.PageSelect(MapUnionId, UserTable.TableName, null, null, null, page,
                out _, out _);
            await asyncEngine.PageSelectAsync(
                MapUnionId, UserTable.TableName, null, null, null, page);
            AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
                $"{label} automatic pagination");
            if (label == "SqlServer")
            {
                StringAssert.Contains(asyncConnection.Commands[1].CommandText,
                    "ORDER BY (SELECT NULL) ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY",
                    "The SQL Server async path must supply the automatic ordering.");
            }

            syncConnection.Commands.Clear();
            asyncConnection.Commands.Clear();

            // First is a page of one through the same dialect machinery.
            syncEngine.First(MapUnionId, UserTable.TableName, null, null);
            await asyncEngine.FirstAsync(MapUnionId, UserTable.TableName, null, null);
            AssertIdenticalCommands(syncConnection.Commands, asyncConnection.Commands,
                $"{label} page-of-one");
            if (label == "SqlServer")
            {
                StringAssert.Contains(asyncConnection.Commands.Single().CommandText,
                    "ORDER BY (SELECT NULL) ASC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY",
                    "The SQL Server async First must supply the automatic ordering.");
            }
        }
    }

    [TestMethod]
    public async Task PreCancelledToken_ThrowsBeforeAnyDatabaseCall()
    {
        var connection = new RecordingConnection(ExecutionSurface.AsyncOnly);
        var engine = new DbEngine(new RecordingExecutor(connection));
        var cancelled = new CancellationToken(canceled: true);
        var where = new WhereConditions();
        where.Add(UserTable.Id, "1");
        var orderBy = new OrderByCondition(UserTable.Id);
        var columns = new ColumnParam();
        columns.AddSelect(UserTable.Id);
        var page = new PageParam { PageNumber = 1, PageSize = 1 };
        var join = new JoinConditions(UserTable.TableName, UserTable.Id, UserTable.TableName, UserTable.Id);

        // Warm the entity column cache through the create-table path so the
        // entity-based methods reach the token check; the commands opened for
        // the warm-up form the baseline the canceled calls must not extend.
        connection.ScalarResult = 0;
        await engine.InitializeTableAsync([new TableDefine(typeof(UserTable))]);
        connection.Commands.Clear();
        var openedAsync = connection.OpenedAsync;

        // SelectAsync, all six overloads.
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectAsync(MapUnionId, UserTable.TableName, where, orderBy, columns,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectAsync(MapUnionId, UserTable.TableName, where, orderBy,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectAsync<User>(where, orderBy, columns, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectAsync<User>(where, orderBy, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectAsync(MapUnionId, join, where, orderBy, columns,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectAsync(MapUnionId, join, where, orderBy,
                cancellationToken: cancelled));

        // FirstAsync, all three overloads.
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.FirstAsync(MapUnionId, UserTable.TableName, where, orderBy, columns,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.FirstAsync(MapUnionId, UserTable.TableName, where, orderBy,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.FirstAsync<User>(where, orderBy, cancellationToken: cancelled));

        // SelectByIdAsync.
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.SelectByIdAsync<User>("1", cancellationToken: cancelled));

        // PageSelectAsync, all four overloads.
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.PageSelectAsync(MapUnionId, UserTable.TableName, where, orderBy, columns,
                page, cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.PageSelectAsync(MapUnionId, UserTable.TableName, where, orderBy, page,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.PageSelectAsync<User>(where, orderBy, columns, page,
                cancellationToken: cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => engine.PageSelectAsync<User>(where, orderBy, page,
                cancellationToken: cancelled));

        Assert.AreEqual(openedAsync, connection.OpenedAsync,
            "A canceled call must not open a connection.");
        Assert.AreEqual(0, connection.OpenedSync,
            "A canceled call must not open a connection synchronously.");
        Assert.AreEqual(0, connection.Commands.Count, "A canceled call must not execute a command.");
    }

    private static string MapUnionId(IDataRecord record) => record["unionId"].ToString();

    /// <summary>
    /// Runs all six Select overloads on both engines and asserts the mapped
    /// results match. The fake reader returns two rows, so the row collections
    /// are compared element-wise.
    /// </summary>
    private static async Task AssertSelectsMatchAsync(DbEngine syncEngine, DbEngine asyncEngine,
        WhereConditions where, OrderByCondition orderBy, ColumnParam columns, JoinConditions join,
        DbConnection con1 = null, DbTransaction tran1 = null,
        DbConnection con2 = null, DbTransaction tran2 = null)
    {
        var sync1 = syncEngine.Select(MapUnionId, UserTable.TableName, where, orderBy, columns, con1, tran1);
        var async1 = await asyncEngine.SelectAsync(MapUnionId, UserTable.TableName, where, orderBy, columns, con2, tran2);
        CollectionAssert.AreEqual(sync1, async1, "convertor overload with columns");

        var sync2 = syncEngine.Select(MapUnionId, UserTable.TableName, where, orderBy, con1, tran1);
        var async2 = await asyncEngine.SelectAsync(MapUnionId, UserTable.TableName, where, orderBy, con2, tran2);
        CollectionAssert.AreEqual(sync2, async2, "convertor overload without columns");

        var sync3 = syncEngine.Select<User>(where, orderBy, columns, con1, tran1);
        var async3 = await asyncEngine.SelectAsync<User>(where, orderBy, columns, con2, tran2);
        Assert.AreEqual(sync3.Count, async3.Count, "entity overload with columns");
        Assert.AreEqual(sync3[0].UnionId, async3[0].UnionId, "entity overload with columns");

        var sync4 = syncEngine.Select<User>(where, orderBy, con1, tran1);
        var async4 = await asyncEngine.SelectAsync<User>(where, orderBy, con2, tran2);
        Assert.AreEqual(sync4.Count, async4.Count, "entity overload without columns");
        Assert.AreEqual(sync4[0].UnionId, async4[0].UnionId, "entity overload without columns");

        var sync5 = syncEngine.Select(MapUnionId, join, where, orderBy, columns, con1, tran1);
        var async5 = await asyncEngine.SelectAsync(MapUnionId, join, where, orderBy, columns, con2, tran2);
        CollectionAssert.AreEqual(sync5, async5, "join overload with columns");

        var sync6 = syncEngine.Select(MapUnionId, join, where, orderBy, con1, tran1);
        var async6 = await asyncEngine.SelectAsync(MapUnionId, join, where, orderBy, con2, tran2);
        CollectionAssert.AreEqual(sync6, async6, "join overload without columns");
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
        private readonly SqlBuilder _sqlBuilder;

        public RecordingExecutor(RecordingConnection connection, SqlBuilder sqlBuilder = null)
            : base("recording")
        {
            _connection = connection;
            _sqlBuilder = sqlBuilder ?? new SqlBuilder();
        }

        public override SqlBuilder SqlBuilder => _sqlBuilder;

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

        /// <summary>Number of rows returned by the fake reader members.</summary>
        public int ReaderRowCount { get; set; } = 2;

        public int OpenedSync { get; private set; }

        public int OpenedAsync { get; private set; }

        public override string ConnectionString { get; set; } = "recording";
        public override string Database => "recording";
        public override string DataSource => "recording";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close()
        {
            _state = ConnectionState.Closed;
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
            return new RecordingTransaction(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand() => new RecordingCommand(this, _surface);
    }

    private sealed class RecordingTransaction(RecordingConnection connection, IsolationLevel isolationLevel)
        : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        public override void Commit() { }

        public override void Rollback() { }
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

        private DbDataReader CreateReader()
        {
            var table = new DataTable();
            table.Columns.Add("unionId", typeof(string));
            table.Columns.Add("intData", typeof(int));
            for (var i = 1; i <= connection.ReaderRowCount; i++)
            {
                table.Rows.Add($"row{i}", i);
            }

            return table.CreateDataReader();
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
