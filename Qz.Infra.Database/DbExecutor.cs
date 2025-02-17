using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Qz.Infra.Database;

public abstract class DbExecutor
{
    protected DbExecutor(string connectionString, IDbConnection connection)
    {
        try
        {
            // because of connection instance not include db password, so need provide connection string.
            connection.Open();
            ConnectionString = connectionString;
        }
        finally
        {
            connection.Close();
        }
    }

    public string ConnectionString { get; }

    public virtual SqlBuilder SqlBuilder { get; } = new();

    public abstract IDbConnection GetConnection();

    public void Transaction(Action<IDbTransaction> action)
    {
        using var conn = GetConnection();
        try
        {
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                action(tran);
                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        finally
        {
            conn.Close();
        }
    }

    public T Transaction<T>(Func<IDbTransaction, T> func)
    {
        using var conn = GetConnection();
        try
        {
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                var result = func(tran);
                tran.Commit();
                return result;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        finally
        {
            conn.Close();
        }
    }

    public void Execute(Action<IDbConnection> action)
    {
        using var con = GetConnection();
        try
        {
            con.Open();
            action(con);
        }
        finally
        {
            con.Close();
        }
    }

    public T Execute<T>(Func<IDbConnection, T> func)
    {
        using var con = GetConnection();
        try
        {
            con.Open();
            return func(con);
        }
        finally
        {
            con.Close();
        }
    }

    protected internal abstract void GetExistTableParam(TableDefine table, 
        out string checkTable, out WhereConditions where);

    protected abstract Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command);

    protected abstract IDataAdapter GetAdapter(IDbCommand command);

    private IDbCommand BuildCommand(IDbConnection con, string sql,
        IEnumerable<ValueParam> dbParams = null, IDbTransaction tran = null)
    {
        if (con == null) throw new ArgumentNullException(nameof(con));
        if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

        var command = con.CreateCommand();
        command.CommandText = sql;

        if (dbParams != null)
        {
            var func = GetAddParameterFunc(command);
            foreach (var item in dbParams)
            {
                func(SqlBuilder.GetParamName(item), item.Value);
            }
        }

        if (tran != null)
        {
            command.Transaction = tran;
        }

        return command;
    }

    private T Execute<T>(string sql, Func<IDbCommand, T> func, 
        IEnumerable<ValueParam> dbParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        if (tran != null)
        {
            using var command = BuildCommand(tran.Connection, sql, dbParams, tran);
            return func(command);
        }

        if (con != null)
        {
            using var command = BuildCommand(con, sql, dbParams);
            return func(command);
        }

        return Execute(newCon =>
        {
            using var command = BuildCommand(newCon, sql, dbParams);
            return func(command);
        });
    }

    internal T Execute<T>(string sql, Func<IDataReader, T> func,
        IEnumerable<ValueParam> dbParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(sql, command =>
        {
            using var reader = command.ExecuteReader();
            return reader is DbDataReader { HasRows: true } ? func(reader) : default(T);
        }, dbParams, con, tran);
    }

    #region Public Execute

    public int Execute(string sql,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(sql, command => command.ExecuteNonQuery(), dbParams, con, tran);
    }

    public List<T> Execute<T>(string sql, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(sql, reader =>
        {
            var result = new List<T>();
            while (reader.Read())
            {
                result.Add(convertor(reader));
            }
            return result;
        }, dbParams, con, tran);
    }

    public DataSet ExecuteAdapter(string sql,
        IEnumerable<ValueParam> dbParams = null, 
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(sql, command =>
        {
            var adapter = GetAdapter(command);
            var ds = new DataSet();
            adapter.Fill(ds);
            return ds;
        }, dbParams, con, tran);
    }

    #endregion
}