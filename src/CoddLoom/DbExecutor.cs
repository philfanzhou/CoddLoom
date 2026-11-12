using CoddLoom.Condition;
using CoddLoom.Params;
using CoddLoom.Sql;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom;

public abstract class DbExecutor
{
    /// <summary>
    /// Initializes the executor with a connection string. Constructing an executor
    /// performs no I/O: it does not open a connection, issue DDL, or create files.
    /// Use <see cref="ValidateConnection"/> to verify reachability explicitly.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connectionString"/> is null or empty.
    /// </exception>
    protected DbExecutor(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        ConnectionString = connectionString;
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

    public abstract DbConnection GetConnection();

    /// <summary>
    /// Opens and closes one connection to verify that the configured connection
    /// string is reachable. This is the explicit replacement for the construction
    /// -time connection probe removed in 0.2.0: construction performs no I/O, so
    /// callers that want eager validation must call this method.
    /// </summary>
    /// <remarks>
    /// A successful return only proves that the connection could be opened at the
    /// moment of the call; it does not reserve or keep the connection. Failures
    /// surface as the underlying driver's connection exception.
    /// </remarks>
    public void ValidateConnection()
    {
        using var connection = GetConnection();
        try
        {
            connection.Open();
        }
        finally
        {
            connection.Close();
        }
    }

    /// <summary>
    /// Asynchronously opens and closes one connection to verify that the
    /// configured connection string is reachable. This is the asynchronous
    /// counterpart of <see cref="ValidateConnection"/>; construction still
    /// performs no I/O.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call awaits the driver's <c>OpenAsync</c> directly; it never wraps
    /// the synchronous <c>Open</c> in a background task. A canceled token
    /// throws <see cref="OperationCanceledException"/> before any connection
    /// attempt; cancellation during the open surfaces as the driver's
    /// exception.
    /// </para>
    /// <para>
    /// A successful return only proves that the connection could be opened at
    /// the moment of the call; it does not reserve or keep the connection.
    /// </para>
    /// </remarks>
    public async Task ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = GetConnection();
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            connection.Close();
        }
    }

    /// <summary>
    /// Runs the action inside one transaction on a dedicated connection and
    /// commits when it completes; any failure rolls the transaction back and
    /// rethrows the callback's exception.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the callback throws, the transaction is rolled back before the
    /// callback's original exception is rethrown. If the rollback itself
    /// also throws — for example because the connection already broke — the
    /// original exception still propagates unchanged; the rollback failure
    /// is retained in the original exception's
    /// <see cref="Exception.Data"/> dictionary under the key
    /// <c>CoddLoom.RollbackException</c>, so it cannot replace the original
    /// exception and hide the root cause.
    /// </para>
    /// </remarks>
    public void Transaction(Action<DbTransaction> action)
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
            catch (Exception exception)
            {
                RollbackPreservingException(tran, exception);
                throw;
            }
        }
        finally
        {
            conn.Close();
        }
    }

    public T Transaction<T>(Func<DbTransaction, T> func)
    {
        var result = default(T);
        Transaction(tran =>
        {
            result = func(tran);
        });
        return result;
    }

    /// <summary>
    /// The key under which a failed rollback's exception is retained in the
    /// original callback exception's <see cref="Exception.Data"/> dictionary.
    /// </summary>
    internal const string RollbackExceptionDataKey = "CoddLoom.RollbackException";

    /// <summary>
    /// Rolls the transaction back without letting a rollback failure replace
    /// the callback's original exception. When the rollback throws, the
    /// rollback failure is retained in the original exception's
    /// <see cref="Exception.Data"/> dictionary instead; the rollback outcome
    /// itself still depends on the driver and the connection state.
    /// </summary>
    private static void RollbackPreservingException(DbTransaction tran, Exception originalException)
    {
        try
        {
            tran.Rollback();
        }
        catch (Exception rollbackFailure)
        {
            originalException.Data[RollbackExceptionDataKey] = rollbackFailure;
        }
    }

    /// <summary>
    /// Runs the awaited action inside one transaction on a dedicated
    /// connection and commits when it completes; any failure rolls the
    /// transaction back and rethrows. This is the asynchronous counterpart of
    /// <see cref="Transaction(Action{DbTransaction})"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The connection is opened asynchronously and the callback's task is
    /// awaited directly; the callback must not block. Transaction begin,
    /// commit, and rollback use their synchronous members: netstandard2.0
    /// offers no <c>BeginTransactionAsync</c>, and for most drivers beginning
    /// a transaction is not a network round trip.
    /// </para>
    /// <para>
    /// Rollback failure semantics match the synchronous version exactly: when
    /// the callback throws and the rollback itself also throws, the
    /// callback's original exception propagates and the rollback failure is
    /// retained in its <see cref="Exception.Data"/> dictionary under the key
    /// <c>CoddLoom.RollbackException</c>. The cancellation token is observed
    /// before the connection opens and is forwarded to <c>OpenAsync</c>;
    /// callbacks receive no token.
    /// </para>
    /// </remarks>
    public async Task TransactionAsync(Func<DbTransaction, Task> action,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var conn = GetConnection();
        try
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var tran = conn.BeginTransaction();
            try
            {
                await action(tran).ConfigureAwait(false);
                tran.Commit();
            }
            catch (Exception exception)
            {
                RollbackPreservingException(tran, exception);
                throw;
            }
        }
        finally
        {
            conn.Close();
        }
    }

    /// <summary>
    /// Runs the awaited function inside one transaction on a dedicated
    /// connection, commits on success, and returns the function's result; any
    /// failure rolls the transaction back and rethrows. This is the
    /// asynchronous counterpart of <see cref="Transaction{T}(Func{DbTransaction, T})"/>.
    /// </summary>
    /// <remarks>
    /// Transaction and cancellation semantics match
    /// <see cref="TransactionAsync(Func{DbTransaction, Task}, CancellationToken)"/>,
    /// including the synchronous transaction members and the rollback failure
    /// behavior of the synchronous version.
    /// </remarks>
    public async Task<T> TransactionAsync<T>(Func<DbTransaction, Task<T>> func,
        CancellationToken cancellationToken = default)
    {
        var result = default(T);
        await TransactionAsync(async tran =>
        {
            result = await func(tran).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public T TryTransaction<T>(Func<DbTransaction, T> func)
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

    public void Execute(Action<DbConnection> action)
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

    public T Execute<T>(Func<DbConnection, T> func)
    {
        var result = default(T);
        Execute(con =>
        {
            result = func(con);
        });
        return result;
    }

    /// <summary>
    /// Opens one dedicated connection asynchronously, awaits the action with
    /// it, and closes the connection afterwards. This is the asynchronous
    /// counterpart of <see cref="Execute(Action{DbConnection})"/>.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>OpenAsync</c> directly; it never wraps
    /// the synchronous path in a background task, and the callback must not
    /// block. The connection is closed even when the callback throws. The
    /// cancellation token is observed before the connection opens and is
    /// forwarded to <c>OpenAsync</c>; callbacks receive no token.
    /// </remarks>
    public async Task ExecuteAsync(Func<DbConnection, Task> action,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var con = GetConnection();
        try
        {
            await con.OpenAsync(cancellationToken).ConfigureAwait(false);
            await action(con).ConfigureAwait(false);
        }
        finally
        {
            con.Close();
        }
    }

    /// <summary>
    /// Opens one dedicated connection asynchronously, awaits the function with
    /// it, closes the connection afterwards, and returns the function's
    /// result. This is the asynchronous counterpart of
    /// <see cref="Execute{T}(Func{DbConnection, T})"/>.
    /// </summary>
    /// <remarks>
    /// Connection and cancellation semantics match
    /// <see cref="ExecuteAsync(Func{DbConnection, Task}, CancellationToken)"/>.
    /// </remarks>
    public async Task<T> ExecuteAsync<T>(Func<DbConnection, Task<T>> func,
        CancellationToken cancellationToken = default)
    {
        var result = default(T);
        await ExecuteAsync(async con =>
        {
            result = await func(con).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public T TryExecute<T>(Func<DbConnection, T> func)
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

    protected abstract Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command);

    protected abstract IDataAdapter GetAdapter(IDbCommand command);

    private DbCommand BuildCommand(DbConnection con, string commandText,
        IEnumerable<ValueParam> dbParams = null, DbTransaction tran = null)
    {
        if (con == null) throw new ArgumentNullException(nameof(con));
        if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));

        var parameters = PrepareParameters(dbParams);
        var command = con.CreateCommand();
        try
        {
            command.CommandText = commandText;
            command.CommandTimeout = 300;

            if (parameters != null)
            {
                var func = GetAddParameterFunc(command);
                foreach (var item in parameters)
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
        catch
        {
            command.Dispose();
            throw;
        }
    }

    private IReadOnlyList<ValueParam> PrepareParameters(IEnumerable<ValueParam> dbParams)
    {
        if (dbParams == null)
        {
            return null;
        }

        var parameters = new List<ValueParam>();
        foreach (var parameter in dbParams)
        {
            if (parameter == null)
            {
                throw new ArgumentException("Parameter collections cannot contain null values.", nameof(dbParams));
            }
            if (parameters.Count >= MaxParametersPerCommand)
            {
                throw new InvalidOperationException(
                    $"The command requires more than the provider limit of {MaxParametersPerCommand} parameters.");
            }

            parameters.Add(parameter);
        }

        return parameters;
    }

    private T Execute<T>(string commandText, Func<IDbCommand, T> func,
        IEnumerable<ValueParam> dbParams = null, DbConnection con = null, DbTransaction tran = null)
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
        return result == null || result == DBNull.Value ? default(T) : convertor(result);
    }

    private T Procedure<T>(IDbCommand command, Func<IDbCommand, T> func)
    {
        command.CommandType = CommandType.StoredProcedure;
        return func(command);
    }

    private async Task<T> ExecuteAsync<T>(string commandText,
        Func<DbCommand, CancellationToken, Task<T>> func,
        IEnumerable<ValueParam> dbParams = null, DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tran != null)
        {
            using var command = BuildCommand(tran.Connection, commandText, dbParams, tran);
            return await func(command, cancellationToken).ConfigureAwait(false);
        }

        if (con != null)
        {
            using var command = BuildCommand(con, commandText, dbParams);
            return await func(command, cancellationToken).ConfigureAwait(false);
        }

        return await ExecuteAsync(async newCon =>
        {
            // Await inside the using scope: returning the task without
            // awaiting would dispose the command while its asynchronous
            // execution is still running.
            using var command = BuildCommand(newCon, commandText, dbParams);
            return await func(command, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> NonQueryAsync(DbCommand command, CancellationToken cancellationToken)
    {
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<T>> ReaderAsync<T>(DbCommand command, Func<IDataRecord, T> convertor,
        CancellationToken cancellationToken)
    {
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(convertor(reader));
        }
        return result;
    }

    private async Task<T> ScalarAsync<T>(DbCommand command, Func<object, T> convertor,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result == null || result == DBNull.Value ? default(T) : convertor(result);
    }

    private async Task<T> ProcedureAsync<T>(DbCommand command,
        Func<DbCommand, CancellationToken, Task<T>> func, CancellationToken cancellationToken)
    {
        command.CommandType = CommandType.StoredProcedure;
        return await func(command, cancellationToken).ConfigureAwait(false);
    }

    #region Public

    public int NonQuery(string commandText,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(commandText, NonQuery, dbParams, con, tran);
    }

    /// <summary>
    /// Executes a non-query statement asynchronously and returns the number of
    /// affected rows. This is the asynchronous counterpart of
    /// <see cref="NonQuery(string, IEnumerable{ValueParam}, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, one-connection-per-command lifecycle,
    /// command timeout, parameter limit, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>ExecuteNonQueryAsync</c> directly; it
    /// never wraps the synchronous execution in a background task. The
    /// cancellation token is observed before any database call and is
    /// forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> NonQueryAsync(string commandText,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(commandText, NonQueryAsync, dbParams, con, tran, cancellationToken);
    }

    public int NonQueryProcedure(string procedureName,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, NonQuery), dbParams, con, tran);
    }

    /// <summary>
    /// Executes a stored procedure without a result set asynchronously and
    /// returns the number of affected rows. This is the asynchronous
    /// counterpart of <see cref="NonQueryProcedure"/>; the generated command,
    /// parameters, lifecycle, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>ExecuteNonQueryAsync</c> directly; it
    /// never wraps the synchronous execution in a background task. The
    /// cancellation token is observed before any database call and is
    /// forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> NonQueryProcedureAsync(string procedureName,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(procedureName,
            (command, token) => ProcedureAsync(command, NonQueryAsync, token),
            dbParams, con, tran, cancellationToken);
    }

    public List<T> Reader<T>(string commandText, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(commandText, command => Reader(command, convertor), dbParams, con, tran);
    }

    /// <summary>
    /// Executes a query asynchronously and maps every row with the convertor.
    /// This is the asynchronous counterpart of
    /// <see cref="Reader{T}(string, Func{IDataRecord, T}, IEnumerable{ValueParam}, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, lifecycle, and exception types are
    /// identical, and the convertor keeps the synchronous
    /// <see cref="IDataRecord"/> signature.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>ExecuteReaderAsync</c> and
    /// <c>ReadAsync</c> directly; it never wraps the synchronous execution in
    /// a background task. The cancellation token is observed before any
    /// database call and is forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> ReaderAsync<T>(string commandText, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(commandText,
            (command, token) => ReaderAsync(command, convertor, token),
            dbParams, con, tran, cancellationToken);
    }

    public List<T> ReaderProcedure<T>(string procedureName, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, c => Reader(c, convertor)), dbParams, con, tran);
    }

    /// <summary>
    /// Executes a stored procedure asynchronously and maps every result row
    /// with the convertor. This is the asynchronous counterpart of
    /// <see cref="ReaderProcedure{T}(string, Func{IDataRecord, T}, IEnumerable{ValueParam}, DbConnection, DbTransaction)"/>;
    /// the generated command, parameters, lifecycle, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>ExecuteReaderAsync</c> and
    /// <c>ReadAsync</c> directly; it never wraps the synchronous execution in
    /// a background task. The cancellation token is observed before any
    /// database call and is forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> ReaderProcedureAsync<T>(string procedureName, Func<IDataRecord, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(procedureName,
            (command, token) => ProcedureAsync(command, (c, t) => ReaderAsync(c, convertor, t), token),
            dbParams, con, tran, cancellationToken);
    }

    public DataSet Adapter(string commandText, 
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(commandText, Adapter, dbParams, con, tran);
    }

    public DataSet AdapterProcedure(string procedureName,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, Adapter), dbParams, con, tran);
    }

    public T Scalar<T>(string commandText, Func<object, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(commandText, command => Scalar(command, convertor), dbParams, con, tran);
    }

    /// <summary>
    /// Executes a query asynchronously and converts the first column of the
    /// first row, mapping <c>null</c> and database null to the type's default
    /// value. This is the asynchronous counterpart of
    /// <see cref="Scalar{T}(string, Func{object, T}, IEnumerable{ValueParam}, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, lifecycle, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>ExecuteScalarAsync</c> directly; it
    /// never wraps the synchronous execution in a background task. The
    /// cancellation token is observed before any database call and is
    /// forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<T> ScalarAsync<T>(string commandText, Func<object, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(commandText,
            (command, token) => ScalarAsync(command, convertor, token),
            dbParams, con, tran, cancellationToken);
    }

    public T ScalarProcedure<T>(string procedureName, Func<object, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Execute(procedureName, command => Procedure(command, c => Scalar(c, convertor)), dbParams, con, tran);
    }

    /// <summary>
    /// Executes a stored procedure asynchronously and converts the first
    /// column of the first row, mapping <c>null</c> and database null to the
    /// type's default value. This is the asynchronous counterpart of
    /// <see cref="ScalarProcedure{T}(string, Func{object, T}, IEnumerable{ValueParam}, DbConnection, DbTransaction)"/>;
    /// the generated command, parameters, lifecycle, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call awaits the driver's <c>ExecuteScalarAsync</c> directly; it
    /// never wraps the synchronous execution in a background task. The
    /// cancellation token is observed before any database call and is
    /// forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<T> ScalarProcedureAsync<T>(string procedureName, Func<object, T> convertor,
        IEnumerable<ValueParam> dbParams = null,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(procedureName,
            (command, token) => ProcedureAsync(command, (c, t) => ScalarAsync(c, convertor, t), token),
            dbParams, con, tran, cancellationToken);
    }

    #endregion
}
