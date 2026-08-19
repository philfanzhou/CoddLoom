using Microsoft.Data.SqlClient;
using CoddLoom.Condition;
using CoddLoom.Sql;
using CoddLoom.Table;
using System;
using System.Data;

namespace CoddLoom.SqlServer;

public class SqlServerExecutor : DbExecutor
{
    private const string TrustServerConfig = "TrustServerCertificate";
    private readonly bool _trustServerCertificate;

    public SqlServerExecutor(string connectionString, bool trustServer = true)
        : base(connectionString, CreateConnection(connectionString, trustServer))
    {
        _trustServerCertificate = trustServer;
    }

    public override SqlBuilder SqlBuilder { get; } = new SqlServerBuilder();

    public override IDbConnection GetConnection()
    {
        return CreateConnection(ConnectionString, _trustServerCertificate);
    }

    private static IDbConnection CreateConnection(string connectionString, bool trustServer)
    {
        if (trustServer && !connectionString.ToLower().Contains(TrustServerConfig.ToLower()))
        {
            connectionString = $"{connectionString};{TrustServerConfig}=true";
        }
        var conn = new SqlConnection(connectionString);
        return conn;
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

    protected override void GetExistTableParam(TableDefine table, out string checkTable, out WhereConditions where)
    {
        where = new WhereConditions();
        where.Add("xtype", "U");
        where.Add("name", table.Name);
        checkTable = "sysobjects";
    }
}