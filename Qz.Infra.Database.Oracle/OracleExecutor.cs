using Oracle.ManagedDataAccess.Client;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Table;
using System;
using System.Data;

namespace Qz.Infra.Database.Oracle;

public class OracleExecutor : DbExecutor
{
    public OracleExecutor(string connectionString)
        : base(connectionString, new OracleConnection(connectionString))
    {
    }

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

        return cmd.Parameters.Add;
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

    protected override void GetExistTableParam(TableDefine table, out string checkTable, out WhereConditions where)
    {
        throw new NotImplementedException();
    }
}