using CoddLoom.Condition;
using CoddLoom.Params;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class DbEngineFirstTest
{
    [TestMethod]
    public void First_ExecutesOneSelectAndPreservesQueryContext()
    {
        var connection = new RecordingConnection();
        var engine = new DbEngine(new RecordingExecutor(connection));
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var where = new WhereConditions("id", 0, WhereOperator.GreaterThan);
        var orderBy = new OrderByCondition("id", descending: true);
        var columns = new ColumnParam().AddSelect("name");

        var result = engine.First(
            record => record["name"].ToString(),
            "first_test",
            where,
            orderBy,
            columns,
            connection,
            transaction);

        Assert.AreEqual("second", result);
        Assert.HasCount(1, connection.Commands);

        var command = connection.Commands.Single();
        StringAssert.Contains(command.CommandText, "SELECT name FROM first_test");
        StringAssert.Contains(command.CommandText, "WHERE id >");
        StringAssert.Contains(command.CommandText, "ORDER BY id DESC");
        StringAssert.Contains(command.CommandText, "LIMIT 0,1");
        Assert.IsFalse(command.CommandText.Contains("COUNT", StringComparison.OrdinalIgnoreCase));
        Assert.AreSame(transaction, command.Transaction);
        Assert.HasCount(1, command.Parameters);
        Assert.AreEqual(0, command.Parameters.Single().Value);
    }

    private sealed class RecordingExecutor : DbExecutor
    {
        private readonly RecordingConnection _connection;

        public RecordingExecutor(RecordingConnection connection)
            : base("recording", connection)
        {
            _connection = connection;
        }

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

        protected override IDataAdapter GetAdapter(IDbCommand command)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingConnection : DbConnection
    {
        private ConnectionState _state;

        public List<RecordedCommand> Commands { get; } = [];

        public override string ConnectionString { get; set; }
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
            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new RecordingTransaction(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new RecordingCommand(this);
        }
    }

    private sealed class RecordingTransaction(RecordingConnection connection, IsolationLevel isolationLevel)
        : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;
        protected override DbConnection DbConnection => connection;

        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class RecordingCommand(RecordingConnection connection) : DbCommand
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
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new RecordingParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            connection.Commands.Add(new RecordedCommand(
                CommandText,
                _parameters.Cast<DbParameter>().ToList(),
                DbTransaction));

            var table = new DataTable();
            table.Columns.Add("name", typeof(string));
            table.Rows.Add("second");
            return table.CreateDataReader();
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
        public override void RemoveAt(string parameterName) => _parameters.RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) =>
            _parameters[IndexOf(parameterName)] = value;
    }

    private sealed record RecordedCommand(
        string CommandText,
        IReadOnlyList<DbParameter> Parameters,
        DbTransaction Transaction);
}
