using MySql.Data.MySqlClient;
using CoddLoom.Sql;
using System;
using System.Data;

namespace CoddLoom.MySql;

public class MySqlExecutor : DbExecutor
{
    public MySqlExecutor(string connectionString)
        : base(connectionString)
    {
    }

    public MySqlExecutor(string server, string database, string user, string password, uint port = 3306)
        : this(BuildConnectionString(server, database, user, password, port))
    {
    }

    /// <summary>
    /// Creates the database named in the connection string when it does not exist
    /// yet. This is the explicit replacement for the construction-time
    /// <c>CREATE DATABASE IF NOT EXISTS</c> removed in 0.2.0: the five-argument
    /// constructor only builds a connection string, so callers that rely on
    /// automatic creation must call this method.
    /// </summary>
    /// <remarks>
    /// The database name is safely quoted; no other DDL is issued. Concurrent
    /// callers rely on the database's own <c>IF NOT EXISTS</c> semantics.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The connection string does not name a database.
    /// </exception>
    public void EnsureDatabase()
    {
        var builder = new MySqlConnectionStringBuilder(ConnectionString);
        var database = builder.Database;
        if (string.IsNullOrEmpty(database))
        {
            throw new ArgumentException(
                "The connection string does not specify a database.", nameof(ConnectionString));
        }

        builder.Remove("Database");

        using var connection = new MySqlConnection(builder.ConnectionString);
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = BuildCreateDatabaseSql(database);
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }

    internal static string BuildCreateDatabaseSql(string database)
    {
        return $"CREATE DATABASE IF NOT EXISTS {MySqlIdentifier.QuoteDatabase(database)}";
    }

    public override SqlBuilder SqlBuilder { get; } = new MySqlBuilder();

    public override IDbConnection GetConnection()
    {
        return new MySqlConnection(ConnectionString);
    }

    protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
    {
        if (command is not MySqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        return cmd.Parameters.AddWithValue;
    }

    protected override IDataAdapter GetAdapter(IDbCommand command)
    {
        if (command is not MySqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        var adapter = new MySqlDataAdapter();
        adapter.SelectCommand = cmd;
        return adapter;
    }

    private static string BuildConnectionString(string server, string database,
        string user, string password, uint port)
    {
        return new MySqlConnectionStringBuilder
        {
            Server = server,
            UserID = user,
            Password = password,
            Port = port,
            Database = database
        }.ConnectionString;
    }
}
