using Oracle.ManagedDataAccess.Client;
using CoddLoom.Sql;
using System;
using System.Data;

namespace CoddLoom.Oracle;

public class OracleExecutor : DbExecutor
{
    public OracleExecutor(string connectionString)
        : base(connectionString, new OracleConnection(connectionString))
    {
    }

    public override SqlBuilder SqlBuilder { get; } = new OracleBuilder();

    public override IDbConnection GetConnection()
    {
        return new OracleConnection(ConnectionString);
    }

    protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
    {
        if(command is not OracleCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        cmd.BindByName = true;
        return (name, value) => cmd.Parameters.Add(name.TrimStart(':'), value);
    }

    protected override IDataAdapter GetAdapter(IDbCommand command)
    {
        if (command is not OracleCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        var adapter = new OracleDataAdapter();
        adapter.SelectCommand = cmd;
        return adapter;
    }

}
