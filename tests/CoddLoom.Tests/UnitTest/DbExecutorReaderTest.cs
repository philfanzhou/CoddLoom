using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;

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

        return table.CreateDataReader();
    }

    private sealed class TestExecutor : DbExecutor
    {
        private readonly TestConnection _connection;

        public TestExecutor(Func<IDataReader> readerFactory)
            : this(new TestConnection(readerFactory))
        {
        }

        private TestExecutor(TestConnection connection)
            : base("test")
        {
            _connection = connection;
        }

        public override DbConnection GetConnection() => _connection;

        protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command) =>
            throw new NotSupportedException();

        protected override IDataAdapter GetAdapter(IDbCommand command) =>
            throw new NotSupportedException();
    }

    private sealed class TestConnection(Func<IDataReader> readerFactory) : DbConnection
    {
        private ConnectionState _state;

        public override string ConnectionString { get; set; }
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new TestCommand(this, readerFactory);
    }

    private sealed class TestCommand(TestConnection connection, Func<IDataReader> readerFactory) : DbCommand
    {
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            new InterfaceOnlyDbDataReader(readerFactory());
    }

    /// <summary>
    /// Wraps an <see cref="IDataReader"/> that offers nothing beyond the interface surface.
    /// This keeps the regression coverage of issue #17 after the executor moved to
    /// <see cref="DbConnection"/>: the Reader pipeline must consume results through
    /// interface-level members only. DbDataReader-only members throw so that any use of
    /// them fails the surrounding test loudly.
    /// </summary>
    private sealed class InterfaceOnlyDbDataReader(IDataReader inner) : DbDataReader
    {
        public override int Depth => inner.Depth;
        public override int FieldCount => inner.FieldCount;
        public override bool IsClosed => inner.IsClosed;
        public override int RecordsAffected => inner.RecordsAffected;
        public override object this[int i] => inner[i];
        public override object this[string name] => inner[name];

        public override bool GetBoolean(int i) => inner.GetBoolean(i);
        public override byte GetByte(int i) => inner.GetByte(i);
        public override long GetBytes(int i, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
            inner.GetBytes(i, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int i) => inner.GetChar(i);
        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) =>
            inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        public override string GetDataTypeName(int i) => inner.GetDataTypeName(i);
        public override DateTime GetDateTime(int i) => inner.GetDateTime(i);
        public override decimal GetDecimal(int i) => inner.GetDecimal(i);
        public override double GetDouble(int i) => inner.GetDouble(i);
        public override Type GetFieldType(int i) => inner.GetFieldType(i);
        public override float GetFloat(int i) => inner.GetFloat(i);
        public override Guid GetGuid(int i) => inner.GetGuid(i);
        public override short GetInt16(int i) => inner.GetInt16(i);
        public override int GetInt32(int i) => inner.GetInt32(i);
        public override long GetInt64(int i) => inner.GetInt64(i);
        public override string GetName(int i) => inner.GetName(i);
        public override int GetOrdinal(string name) => inner.GetOrdinal(name);
        public override string GetString(int i) => inner.GetString(i);
        public override object GetValue(int i) => inner.GetValue(i);
        public override int GetValues(object[] values) => inner.GetValues(values);
        public override bool IsDBNull(int i) => inner.IsDBNull(i);

        public override bool HasRows =>
            throw new NotSupportedException("DbDataReader-only surface must not be used.");
        public override IEnumerator GetEnumerator() =>
            throw new NotSupportedException("DbDataReader-only surface must not be used.");
        public override DataTable GetSchemaTable() => inner.GetSchemaTable();
        public override bool NextResult() => inner.NextResult();
        public override bool Read() => inner.Read();
        public override void Close() => inner.Close();
    }
}
