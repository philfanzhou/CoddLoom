using Microsoft.Data.SqlClient;
using CoddLoom.Sql;
using System;
using System.Data;

namespace CoddLoom.SqlServer;

public class SqlServerExecutor : DbExecutor
{
    private readonly bool _trustServerCertificate;

    public SqlServerExecutor(string connectionString, bool trustServer = false)
        : base(connectionString, CreateConnection(connectionString, trustServer))
    {
        _trustServerCertificate = trustServer;
    }

    public override SqlBuilder SqlBuilder { get; } = new SqlServerBuilder();

    public override int MaxParametersPerCommand => 2100;

    public override IDbConnection GetConnection()
    {
        return CreateConnection(ConnectionString, _trustServerCertificate);
    }

    private static IDbConnection CreateConnection(string connectionString, bool trustServer)
    {
        var conn = new SqlConnection(BuildConnectionString(connectionString, trustServer));
        return conn;
    }

    private static string BuildConnectionString(string connectionString, bool trustServer)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (trustServer)
        {
            builder.TrustServerCertificate = true;
        }

        return builder.ConnectionString;
    }

    protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
    {
        if (command is not SqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        return cmd.Parameters.AddWithValue;
    }

    protected override IDataAdapter GetAdapter(IDbCommand command)
    {
        if (command is not SqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        var adapter = new SqlDataAdapter();
        adapter.SelectCommand = cmd;
        return adapter;
    }

}
