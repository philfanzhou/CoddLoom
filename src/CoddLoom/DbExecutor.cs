using CoddLoom.Condition;
using CoddLoom.Params;
using CoddLoom.Sql;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace CoddLoom;

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

    /// <summary>
    /// Gets the maximum number of parameters supported by one command.
    /// Providers with a lower limit should override this value.
    /// </summary>
    public virtual int MaxParametersPerCommand => int.MaxValue;

    /// <summary>
    /// Gets whether commands may continue in a transaction after a command fails.
    /// Providers which abort the transaction on statement failure should return false.
    /// </summary>
    public virtual bool CanContinueTransactionAfterCommandFailure => true;

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
        var result = default(T);
        Transaction(tran =>
        {
            result = func(tran);
        });
        return result;
    }

    public T TryTransaction<T>(Func<IDbTransaction, T> func)
    {
        try
        {
            return Transaction(func);
        }
        catch
        {
            return default(T);
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
        var result = default(T);
        Execute(con =>
        {
            result = func(con);
        });
        return result;
    }

    public T TryExecute<T>(Func<IDbConnection, T> func)
    {
        try
        {
            return Execute(func);
        }
        catch
        {
            return default(T);
        }
    }

    [Obsolete("Schema inspection is now provided by SqlBuilder schema query methods.")]
    protected internal virtual void GetExistTableParam(TableDefine table,
        out string checkTable, out WhereConditions where)
    {
        where = new WhereConditions();
        where.Add("type", "table");
        where.Add("name", table.Name);
        checkTable = "sqlite_master";
    }

    protected abstract Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command);

    protected abstract IDataAdapter GetAdapter(IDbCommand command);

    private IDbCommand BuildCommand(IDbConnection con, string commandText,
        IEnumerable<ValueParam> dbParams = null, IDbTransaction tran = null)
    {
        if (con == null) throw new ArgumentNullException(nameof(con));
        if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));

        var command = con.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 300;

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

    private T Execute<T>(string commandText, Func<IDbCommand, T> func,
        IEnumerable<ValueParam> dbParams = null, IDbConnection con = null, IDbTransaction tran = null)
    {
        if (tran != null)
        {
            using var command = BuildCommand(tran.Connection, commandText, dbParams, tran);
            return func(command);
        }

        if (con != null)
        {
            using var command = BuildCommand(con, commandText, dbParams);
            return func(command);
        }

        return Execute(newCon =>
        {
            using var command = BuildCommand(newCon, commandText, dbParams);
            return func(command);
        });
    }

    private int NonQuery(IDbCommand command)
    {
        return command.ExecuteNonQuery();
    }

    private List<T> Reader<T>(IDbCommand command, Func<IDataRecord, T> convertor)
    {
        using var reader = command.ExecuteReader();
        var result = new List<T>();
        if (reader is not DbDataReader { HasRows: true })
        {
            return result;
        }
        while (reader.Read())
        {
            result.Add(convertor(reader));
        }
        return result;
    }

    private DataSet Adapter(IDbCommand command)
    {
        var adapter = GetAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);
        return ds;
    }

    private T Scalar<T>(IDbCommand command, Func<object, T> convertor)
    {
        var result = command.ExecuteScalar();
        return result == null ? default(T) : convertor(result);
    }

    private T Procedure<T>(IDbCommand command, Func<IDbCommand, T> func)
    {
        command.CommandType = CommandType.StoredProcedure;
        return func(command);
    }

    #region Public

    public int NonQuery(string commandText,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(commandText, NonQuery, dbParams, con, tran);
    }

    public int NonQueryProcedure(string procedureName,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, NonQuery), dbParams, con, tran);
    }

    public List<T> Reader<T>(string commandText, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(commandText, command => Reader(command, convertor), dbParams, con, tran);
    }

    public List<T> ReaderProcedure<T>(string procedureName, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, c => Reader(c, convertor)), dbParams, con, tran);
    }

    public DataSet Adapter(string commandText, 
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(commandText, Adapter, dbParams, con, tran);
    }

    public DataSet AdapterProcedure(string procedureName,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, Adapter), dbParams, con, tran);
    }

    public T Scalar<T>(string commandText, Func<object, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(commandText, command => Scalar(command, convertor), dbParams, con, tran);
    }

    public T ScalarProcedure<T>(string procedureName, Func<object, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, c => Scalar(c, convertor)), dbParams, con, tran);
    }

    #endregion
}
