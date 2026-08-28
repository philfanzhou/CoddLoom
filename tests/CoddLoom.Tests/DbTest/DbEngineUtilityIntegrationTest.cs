using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using CoddLoom.Table;

namespace CoddLoom.Tests.DbTest;

[TestClass]
public class DbEngineUtilityIntegrationTest : TestBase
{
    [TestMethod]
    public void GenerateId_RetriesCollisionsAndReportsExhaustion()
    {
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("1", "existing"));

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID API.
        var generated = DbEngine.GenerateId<string>(UserTable.TableName, UserTable.Id,
            current => current == null ? "1" : (int.Parse(current) + 1).ToString());

        Assert.AreEqual("2", generated);
        var exception = Assert.ThrowsExactly<Exception>(() =>
            DbEngine.GenerateId<string>(UserTable.TableName, UserTable.Id, _ => "1", tryCount: 2));
#pragma warning restore CS0618
        StringAssert.Contains(exception.Message, "Generate new UserTable.id ID failed");
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void GenerateId_NonPositiveTryCount_ThrowsBeforeGeneratingOrQuerying(int tryCount)
    {
        var generationCount = 0;

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID API.
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            DbEngine.GenerateId<string>("GenerateIdMustNotQuery", "id", _ =>
            {
                generationCount++;
                return "candidate";
            }, tryCount: tryCount));
#pragma warning restore CS0618

        Assert.AreEqual("tryCount", exception.ParamName);
        Assert.AreEqual(0, generationCount);
    }

    [TestMethod]
    public void GenerateMaxAndTimeIds_ReturnExpectedShapes()
    {
        var numericTable = new TableDefine(typeof(NumericIdTable));
        DbEngine.InitializeTable([numericTable]);
        try
        {
            DbEngine.Insert(NumericIdTable.TableName, new InputValues().Add(NumericIdTable.Id, 3L));
#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID APIs.
            Assert.AreEqual(4L, DbEngine.GenerateMaxId(NumericIdTable.TableName, NumericIdTable.Id));
#pragma warning restore CS0618
        }
        finally
        {
            DbEngine.Drop(NumericIdTable.TableName);
        }

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID APIs.
        var timeId = DbEngine.GenerateTimeId(UserTable.TableName, UserTable.Id,
            () => new DateTime(2024, 2, 3, 4, 5, 6));
        StringAssert.StartsWith(timeId, "240203040506");
        Assert.HasCount(15, timeId);

        var utcId = DbEngine.GenerateUtcTimeId(UserTable.TableName, UserTable.Id);
#pragma warning restore CS0618
        Assert.HasCount(15, utcId);

        var before = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        var timestamp = DbEngine.GetUtcTimeStamp();
        var after = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        Assert.IsGreaterThanOrEqualTo(before, timestamp);
        Assert.IsLessThanOrEqualTo(after, timestamp);
    }

    [TestMethod]
    public void GenerateMaxId_RequeriesMaximumAfterCandidateCollision()
    {
        var executor = new ScriptedIdExecutor([1L, 2L], [true, false]);
        var engine = new DbEngine(executor);

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID API.
        var generated = engine.GenerateMaxId("ScriptedIds", "id");
#pragma warning restore CS0618

        Assert.AreEqual(3L, generated);
        Assert.AreEqual(2, executor.MaximumQueryCount);
        Assert.AreEqual(2, executor.ExistenceQueryCount);
    }

    [TestMethod]
    public void GenerateMaxId_TenCandidateCollisions_ReportsExhaustion()
    {
        var executor = new ScriptedIdExecutor(
            Enumerable.Range(1, 10).Select(value => (long)value),
            Enumerable.Repeat(true, 10));
        var engine = new DbEngine(executor);

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID API.
        var exception = Assert.ThrowsExactly<Exception>(() =>
            engine.GenerateMaxId("ScriptedIds", "id"));
#pragma warning restore CS0618

        StringAssert.Contains(exception.Message, "Generate new ScriptedIds.id ID failed");
        Assert.AreEqual(10, executor.MaximumQueryCount);
        Assert.AreEqual(10, executor.ExistenceQueryCount);
    }

    [TestMethod]
    public void ExecutorConvenienceApis_HandleDataSetsConnectionsAndFailures()
    {
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("1", "adapter"));

        var dataSet = Executor.Adapter($"SELECT {UserTable.Id} FROM {UserTable.TableName}");
        Assert.HasCount(1, dataSet.Tables);
        Assert.HasCount(1, dataSet.Tables[0].Rows);

        var connectionState = Executor.Execute(connection => connection.State);
        Assert.AreEqual(ConnectionState.Open, connectionState);
        Assert.AreEqual(0, Executor.TryExecute<int>(_ => throw new InvalidOperationException("expected")));
        var nullSql = TestExecutorFactory.CurrentDatabaseType == TestExecutorFactory.DatabaseType.Oracle
            ? "SELECT NULL FROM DUAL"
            : "SELECT NULL";
        Assert.AreEqual(0, Executor.Scalar(nullSql, _ => 1));
    }

    [TestMethod]
    public void GenerateTimeId_UsesInvariantGregorianCalendar()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            var cultures = new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("th-TH"),
                originalCulture
            };

            foreach (var culture in cultures)
            {
                CultureInfo.CurrentCulture = culture;
#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID API.
                var generated = DbEngine.GenerateTimeId(UserTable.TableName, UserTable.Id,
                    () => new DateTime(2024, 2, 3, 4, 5, 6));
#pragma warning restore CS0618

                StringAssert.StartsWith(generated, "240203040506", culture.Name);
                Assert.HasCount(15, generated, culture.Name);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void SelectById_ReturnsDefaultForMissingRows()
    {
        Assert.IsNull(DbEngine.SelectById<DbCode.Entity.User>("missing"));
    }

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

    private static class NumericIdTable
    {
        [DbTableName] internal const string TableName = "NumericIdTable";
        [DbPrimaryKey(Type = DbType.Int64)] internal const string Id = "id";
    }

    private sealed class ScriptedIdExecutor : DbExecutor
    {
        private readonly ScriptedIdConnection _connection;

        public ScriptedIdExecutor(IEnumerable<long> maximums, IEnumerable<bool> candidateExists)
            : this(new ScriptedIdConnection(maximums, candidateExists))
        {
        }

        private ScriptedIdExecutor(ScriptedIdConnection connection)
            : base("scripted", connection)
        {
            _connection = connection;
        }

        public int MaximumQueryCount => _connection.MaximumQueryCount;
        public int ExistenceQueryCount => _connection.ExistenceQueryCount;

        public override IDbConnection GetConnection() => _connection;

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

        protected override IDataAdapter GetAdapter(IDbCommand command) =>
            throw new NotSupportedException();
    }

    private sealed class ScriptedIdConnection : IDbConnection
    {
        private readonly Queue<long> _maximums;
        private readonly Queue<bool> _candidateExists;

        public ScriptedIdConnection(IEnumerable<long> maximums, IEnumerable<bool> candidateExists)
        {
            _maximums = new Queue<long>(maximums);
            _candidateExists = new Queue<bool>(candidateExists);
        }

        public int MaximumQueryCount { get; private set; }
        public int ExistenceQueryCount { get; private set; }
        public string ConnectionString { get; set; }
        public int ConnectionTimeout => 0;
        public string Database => "scripted";
        public ConnectionState State { get; private set; }

        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public void Close() => State = ConnectionState.Closed;
        public IDbCommand CreateCommand() => new ScriptedIdCommand(this);
        public void Dispose() => Close();
        public void Open() => State = ConnectionState.Open;

        public IDataReader ReadMaximum()
        {
            MaximumQueryCount++;
            var table = new DataTable();
            table.Columns.Add("id", typeof(long));
            table.Rows.Add(_maximums.Dequeue());
            return table.CreateDataReader();
        }

        public object ReadCandidateExists()
        {
            ExistenceQueryCount++;
            return _candidateExists.Dequeue() ? 1 : 0;
        }
    }

    private sealed class ScriptedIdCommand(ScriptedIdConnection connection) : IDbCommand
    {
        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public IDbConnection Connection { get; set; } = connection;
        public IDataParameterCollection Parameters { get; } = new ScriptedParameterCollection();
        public IDbTransaction Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public IDbDataParameter CreateParameter() => new ScriptedParameter();
        public void Dispose() { }
        public int ExecuteNonQuery() => throw new NotSupportedException();
        public IDataReader ExecuteReader() => connection.ReadMaximum();
        public IDataReader ExecuteReader(CommandBehavior behavior) => connection.ReadMaximum();
        public object ExecuteScalar() => connection.ReadCandidateExists();
        public void Prepare() { }
    }

    private sealed class ScriptedParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; }
        public string SourceColumn { get; set; }
        public DataRowVersion SourceVersion { get; set; }
        public object Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }

    private sealed class ScriptedParameterCollection : ArrayList, IDataParameterCollection
    {
        public bool Contains(string parameterName) => IndexOf(parameterName) >= 0;
        public int IndexOf(string parameterName) =>
            this.Cast<IDbDataParameter>().ToList()
                .FindIndex(parameter => parameter.ParameterName == parameterName);
        public void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));

        public object this[string parameterName]
        {
            get => this[IndexOf(parameterName)];
            set => this[IndexOf(parameterName)] = value;
        }
    }
}
