using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class DbExecutorReaderTest
{
    [TestMethod]
    public void Reader_ReadsAllRowsFromIDataReaderImplementation()
    {
        var executor = new TestExecutor(() => CreateReader("first", "second"));

        var result = executor.Reader("SELECT value", record => record.GetString(0));

        CollectionAssert.AreEqual(new[] { "first", "second" }, result);
    }

    [TestMethod]
    public void Reader_ReturnsEmptyListWhenIDataReaderHasNoRows()
    {
        var executor = new TestExecutor(() => CreateReader());

        var result = executor.Reader("SELECT value", record => record.GetString(0));

        Assert.IsNotNull(result);
        Assert.HasCount(0, result);
    }

    private static IDataReader CreateReader(params string[] values)
    {
        var table = new DataTable();
        table.Columns.Add("value", typeof(string));
        foreach (var value in values)
        {
            table.Rows.Add(value);
        }

        return new InterfaceOnlyDataReader(table.CreateDataReader());
    }

    private sealed class TestExecutor : DbExecutor
    {
        private readonly TestConnection _connection;

        public TestExecutor(Func<IDataReader> readerFactory)
            : this(new TestConnection(readerFactory))
        {
        }

        private TestExecutor(TestConnection connection)
            : base("test", connection)
        {
            _connection = connection;
        }

        public override IDbConnection GetConnection() => _connection;

        protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command) =>
            throw new NotSupportedException();

        protected override IDataAdapter GetAdapter(IDbCommand command) =>
            throw new NotSupportedException();
    }

    private sealed class TestConnection(Func<IDataReader> readerFactory) : IDbConnection
    {
        public string ConnectionString { get; set; }
        public int ConnectionTimeout => 0;
        public string Database => "test";
        public ConnectionState State { get; private set; }

        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public void Close() => State = ConnectionState.Closed;
        public IDbCommand CreateCommand() => new TestCommand(this, readerFactory);
        public void Dispose() => Close();
        public void Open() => State = ConnectionState.Open;
    }

    private sealed class TestCommand(IDbConnection connection, Func<IDataReader> readerFactory) : IDbCommand
    {
        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public IDbConnection Connection { get; set; } = connection;
        public IDataParameterCollection Parameters => throw new NotSupportedException();
        public IDbTransaction Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public IDbDataParameter CreateParameter() => throw new NotSupportedException();
        public void Dispose() { }
        public int ExecuteNonQuery() => throw new NotSupportedException();
        public IDataReader ExecuteReader() => readerFactory();
        public IDataReader ExecuteReader(CommandBehavior behavior) => readerFactory();
        public object ExecuteScalar() => throw new NotSupportedException();
        public void Prepare() { }
    }

    private sealed class InterfaceOnlyDataReader(IDataReader inner) : IDataReader
    {
        public object this[int i] => inner[i];
        public object this[string name] => inner[name];
        public int Depth => inner.Depth;
        public bool IsClosed => inner.IsClosed;
        public int RecordsAffected => inner.RecordsAffected;
        public int FieldCount => inner.FieldCount;

        public void Close() => inner.Close();
        public void Dispose() => inner.Dispose();
        public bool GetBoolean(int i) => inner.GetBoolean(i);
        public byte GetByte(int i) => inner.GetByte(i);
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        public char GetChar(int i) => inner.GetChar(i);
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) =>
            inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        public IDataReader GetData(int i) => inner.GetData(i);
        public string GetDataTypeName(int i) => inner.GetDataTypeName(i);
        public DateTime GetDateTime(int i) => inner.GetDateTime(i);
        public decimal GetDecimal(int i) => inner.GetDecimal(i);
        public double GetDouble(int i) => inner.GetDouble(i);
        public Type GetFieldType(int i) => inner.GetFieldType(i);
        public float GetFloat(int i) => inner.GetFloat(i);
        public Guid GetGuid(int i) => inner.GetGuid(i);
        public short GetInt16(int i) => inner.GetInt16(i);
        public int GetInt32(int i) => inner.GetInt32(i);
        public long GetInt64(int i) => inner.GetInt64(i);
        public string GetName(int i) => inner.GetName(i);
        public int GetOrdinal(string name) => inner.GetOrdinal(name);
        public DataTable GetSchemaTable() => inner.GetSchemaTable();
        public string GetString(int i) => inner.GetString(i);
        public object GetValue(int i) => inner.GetValue(i);
        public int GetValues(object[] values) => inner.GetValues(values);
        public bool IsDBNull(int i) => inner.IsDBNull(i);
        public bool NextResult() => inner.NextResult();
        public bool Read() => inner.Read();
    }
}
