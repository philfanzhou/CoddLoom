using MySqlConnector;
using CoddLoom.Sql;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.MariaDb;

public class MariaDbExecutor(string connectionString)
    : DbExecutor(connectionString)
{
    public MariaDbExecutor(string server, string database, string user, string password, uint port = 3306)
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

    /// <summary>
    /// Asynchronously creates the database named in the connection string when
    /// it does not exist yet. This is the asynchronous counterpart of
    /// <see cref="EnsureDatabase"/>; the same validation and safe quoting
    /// guarantees apply.
    /// </summary>
    /// <remarks>
    /// The database name is validated and safely quoted before any connection
    /// is opened. The call awaits the driver's <c>OpenAsync</c> and
    /// <c>ExecuteNonQueryAsync</c> directly; it never wraps the synchronous
    /// path in a background task. A canceled token throws
    /// <see cref="OperationCanceledException"/> before any connection attempt.
    /// Concurrent callers rely on the database's own <c>IF NOT EXISTS</c>
    /// semantics.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The connection string does not name a database.
    /// </exception>
    public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var builder = new MySqlConnectionStringBuilder(ConnectionString);
        var database = builder.Database;
        if (string.IsNullOrEmpty(database))
        {
            throw new ArgumentException(
                "The connection string does not specify a database.", nameof(ConnectionString));
        }

        cancellationToken.ThrowIfCancellationRequested();

        builder.Remove("Database");

        using var connection = new MySqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = connection.CreateCommand();
            command.CommandText = BuildCreateDatabaseSql(database);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            connection.Close();
        }
    }

    internal static string BuildCreateDatabaseSql(string database)
    {
        return $"CREATE DATABASE IF NOT EXISTS {MariaDbIdentifier.QuoteDatabase(database)}";
    }

    public override SqlBuilder SqlBuilder { get; } = new MariaDbBuilder();

    public override DbConnection GetConnection()
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
