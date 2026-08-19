using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace CoddLoom.Sqlite;

// ReSharper disable once InconsistentNaming
public class SqliteExecutor : DbExecutor
{
    public SqliteExecutor(string connectionString)
        : base(connectionString, BuildConnection(connectionString))
    {
    }

    public SqliteExecutor(string directory, string dbFileName)
        : this(BuildConnectionString(directory, dbFileName))
    {
    }

    public override IDbConnection GetConnection()
    {
        return new SQLiteConnection(ConnectionString);
    }

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
        CreateFilePath(directory, dbFileName);
        var filePath = Path.Combine(directory, dbFileName);
        var connStrBuilder = new SQLiteConnectionStringBuilder
        {
            DataSource = filePath,
            ReadOnly = false
        };
        return connStrBuilder.ConnectionString;
    }

    private static SQLiteConnection BuildConnection(string connectionString)
    {
        var builder = new SQLiteConnectionStringBuilder(connectionString);
        var dir = Path.GetDirectoryName(builder.DataSource);
        var file = Path.GetFileName(builder.DataSource);
        CreateFilePath(dir, file);
        return new SQLiteConnection(builder.ConnectionString);
    }

    private static void CreateFilePath(string directory, string dbFileName)
    {
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentNullException(nameof(directory));
        }
        if (string.IsNullOrEmpty(dbFileName))
        {
            throw new ArgumentNullException(nameof(dbFileName));
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fullPath = Path.Combine(directory, dbFileName);
        if (!File.Exists(fullPath))
        {
            SQLiteConnection.CreateFile(fullPath);
        }
    }
}
