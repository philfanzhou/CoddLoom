using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Sql.Base;
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

    internal bool ExistTable(TableDefine table, IDbConnection con)
    {
        var builderParam = GetExistTableParam(table);
        if (builderParam == null)
        {
            return false;
        }

        var count = Count(SqlBuilder.Count(builderParam), builderParam.WhereConditions?.Parameters, con);
        return count > 0;
    }

    protected abstract SqlBuilderWhereParam GetExistTableParam(TableDefine table);

    protected abstract Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command);

    private IDbCommand BuildCommand(IDbConnection con, string sql,
        IEnumerable<ISqlParameter> sqlParams = null)
    {
        var command = con.CreateCommand();
        command.CommandText = sql;

        if (sqlParams != null)
        {
            var func = GetAddParameterFunc(command);
            foreach (var item in sqlParams)
            {
                func(SqlBuilder.GetParamName(item), item.Value);
            }
        }

        return command;
    }

    private void Execute(IDbConnection con, string sql,
        IEnumerable<ISqlParameter> sqlParams = null, Action<IDataReader> readerAction = null, IDbTransaction tran = null)
    {
        if (con == null) throw new ArgumentNullException(nameof(con));
        if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

        var doOpenCon = con.State != ConnectionState.Open;
        try
        {
            if (doOpenCon)
            {
                con.Open();
            }

            using var command = BuildCommand(con, sql, sqlParams);

            if (tran != null)
            {
                command.Transaction = tran;
            }

            if (readerAction == null)
            {
                command.ExecuteNonQuery();
            }
            else
            {
                using var reader = command.ExecuteReader();
                if (reader is not DbDataReader { HasRows: true })
                {
                    return;
                }

                readerAction(reader);
            }
        }
        finally
        {
            if (doOpenCon)
            {
                con.Close();
            }
        }
    }

    #region Execute

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

    public void Execute(string sql,
        IEnumerable<ISqlParameter> sqlParams = null, Action<IDataReader> readerAction = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        if (tran != null)
        {
            Execute(tran.Connection, sql, sqlParams, readerAction, tran);
        }
        else if (con != null)
        {
            Execute(con, sql, sqlParams, readerAction);
        }
        else
        {
            Execute(p => { Execute(p, sql, sqlParams, readerAction); });
        }
    }

    #endregion

    #region Execute with reader

    public List<T> Select<T>(string sql, Func<IDataRecord, T> convertor,
        IEnumerable<ISqlParameter> sqlParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        var result = new List<T>();

        Execute(sql, sqlParams, reader =>
        {
            while (reader.Read())
            {
                result.Add(convertor(reader));
            }
        }, con, tran);

        return result;
    }

    public T First<T>(string sql, Func<IDataRecord, T> convertor,
        IEnumerable<ISqlParameter> sqlParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        T result = default;

        Execute(sql, sqlParams, reader =>
        {
            reader.Read();
            result = convertor(reader);
        }, con, tran);

        return result;
    }

    public int Count(string sql,
        IEnumerable<ISqlParameter> sqlParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        var count = 0;

        Execute(sql, sqlParams, reader =>
        {
            reader.Read();
            count = reader.GetInt32(0);
        }, con, tran);

        return count;
    }

    #endregion
}