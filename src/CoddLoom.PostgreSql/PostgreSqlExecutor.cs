using CoddLoom.Sql;
using Npgsql;
using System;
using System.Data;

namespace CoddLoom.PostgreSql;

public class PostgreSqlExecutor : DbExecutor
{
    public PostgreSqlExecutor(string connectionString)
        : base(connectionString, new NpgsqlConnection(connectionString))
    {
    }

    public PostgreSqlExecutor(string host, string database, string user, string password, ushort port = 5432)
        : this(BuildConnectionString(host, database, user, password, port))
    {
    }

    public override SqlBuilder SqlBuilder { get; } = new PostgreSqlBuilder();

    public override int MaxParametersPerCommand => 65535;

    public override bool CanContinueTransactionAfterCommandFailure => false;

    public override IDbConnection GetConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
    {
        if (command is not NpgsqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        return cmd.Parameters.AddWithValue;
    }

    protected override IDataAdapter GetAdapter(IDbCommand command)
    {
        if (command is not NpgsqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        return new NpgsqlDataAdapter(cmd);
    }

    private static string BuildConnectionString(
        string host, string database, string user, string password, ushort port)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = database,
            Username = user,
            Password = password,
            Port = port
        }.ConnectionString;
    }
}
