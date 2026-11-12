using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

namespace CoddLoom.Sqlite;

// ReSharper disable once InconsistentNaming
public class SqliteExecutor : DbExecutor
{
    static SqliteExecutor()
    {
        SQLiteFunction.RegisterFunction(typeof(SqliteDecimalCollation));
    }

    public SqliteExecutor(string connectionString)
        : base(connectionString)
    {
    }

    /// <summary>
    /// Builds a connection string for a database file. Constructing the executor
    /// performs no I/O: the directory must already exist, and SQLite creates the
    /// database file itself the first time a connection is opened.
    /// </summary>
    public SqliteExecutor(string directory, string dbFileName)
        : this(BuildConnectionString(directory, dbFileName))
    {
    }

    public override DbConnection GetConnection()
    {
        return new SQLiteConnection(ConnectionString);
    }

    public override int MaxParametersPerCommand => 32766;

    protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
    {
        if (command is not SQLiteCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        return cmd.Parameters.AddWithValue;
    }

    protected override IDataAdapter GetAdapter(IDbCommand command)
    {
        if (command is not SQLiteCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        var adapter = new SQLiteDataAdapter();
        adapter.SelectCommand = cmd;
        return adapter;
    }

    private static string BuildConnectionString(string directory, string dbFileName)
    {
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentNullException(nameof(directory));
        }
        if (string.IsNullOrEmpty(dbFileName))
        {
            throw new ArgumentNullException(nameof(dbFileName));
        }

        var filePath = Path.Combine(directory, dbFileName);
        var connStrBuilder = new SQLiteConnectionStringBuilder
        {
            DataSource = filePath,
            ReadOnly = false
        };
        return connStrBuilder.ConnectionString;
    }
}
