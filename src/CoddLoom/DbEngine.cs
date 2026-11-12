using CoddLoom.Condition;
using CoddLoom.Cache;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom;

public partial class DbEngine
{
    private readonly TableColumnsCache _tableColumnsCache = new();

    public DbEngine(DbExecutor executor, IEnumerable<TableDefine> tables)
    {
        Executor = executor;
        InitializeTable(tables);
    }

    public DbEngine(DbExecutor executor) : this(executor, null)
    { }

    public DbExecutor Executor { get; }

    public void Drop(string tableName,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.DropTableSql(tableName);
        Executor.NonQuery(sql, null, con, tran);
    }

    /// <summary>
    /// Asynchronously drops the table. This is the asynchronous counterpart of
    /// <see cref="Drop(string, DbConnection, DbTransaction)"/>; the generated SQL,
    /// parameters, connection ownership, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task DropAsync(string tableName,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        var sql = Executor.SqlBuilder.DropTableSql(tableName);
        return Executor.NonQueryAsync(sql, null, con, tran, cancellationToken);
    }

    public int Insert(string tableName, InputValues input,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Insert(tableName, [input], out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    /// <summary>
    /// Asynchronously inserts one record. This is the asynchronous counterpart of
    /// <see cref="Insert(string, InputValues, DbConnection, DbTransaction)"/>; the
    /// generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> InsertAsync(string tableName, InputValues input,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        var sql = Executor.SqlBuilder.Insert(tableName, [input], out var dbParams);
        return Executor.NonQueryAsync(sql, dbParams, con, tran, cancellationToken);
    }

    public int Insert(string tableName, IEnumerable<InputValues> inputs, int batchSize, 
        DbTransaction tran = null)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be greater than 0.");
        }

        if (tran == null)
        {
            return Executor.Transaction(transaction =>
                InsertWithTransaction(tableName, inputs, batchSize, transaction));
        }

        return InsertWithTransaction(tableName, inputs, batchSize, tran);
    }

    /// <summary>
    /// Asynchronously inserts multiple records in batches. This is the asynchronous
    /// counterpart of <see cref="Insert(string, IEnumerable{InputValues}, int, DbTransaction)"/>;
    /// the batch splitting by batch size and provider parameter limit, the wrapping
    /// transaction, and the exception types are identical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <paramref name="tran"/> is null, the whole insert runs inside one
    /// engine-owned transaction: every batch commits together, and any failure rolls
    /// everything back. A supplied <paramref name="tran"/> is used as-is and stays
    /// owned by the caller, exactly like the synchronous version.
    /// </para>
    /// <para>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before the connection
    /// opens and is then forwarded to the driver's asynchronous execution.
    /// </para>
    /// </remarks>
    public Task<int> InsertAsync(string tableName, IEnumerable<InputValues> inputs, int batchSize,
        DbTransaction tran = null, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be greater than 0.");
        }

        if (tran == null)
        {
            return Executor.TransactionAsync(transaction =>
                InsertWithTransactionAsync(tableName, inputs, batchSize, transaction, cancellationToken),
                cancellationToken);
        }

        return InsertWithTransactionAsync(tableName, inputs, batchSize, tran, cancellationToken);
    }

    public int Delete(string tableName, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null, bool force = false)
    {
        var sql = Executor.SqlBuilder.DeleteWithParameters(tableName, where, force, out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    /// <summary>
    /// Asynchronously deletes records matching the condition. This is the asynchronous
    /// counterpart of
    /// <see cref="Delete(string, WhereConditions, DbConnection, DbTransaction, bool)"/>;
    /// the generated SQL, parameters, the <c>force</c> guard, connection ownership,
    /// and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> DeleteAsync(string tableName, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null, bool force = false,
        CancellationToken cancellationToken = default)
    {
        var sql = Executor.SqlBuilder.DeleteWithParameters(tableName, where, force, out var dbParams);
        return Executor.NonQueryAsync(sql, dbParams, con, tran, cancellationToken);
    }

    public int Update(string tableName, InputValues input, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null, bool force = false)
    {
        var sql = Executor.SqlBuilder.UpdateWithParameters(tableName, input, where, force, out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    /// <summary>
    /// Asynchronously updates records matching the condition. This is the asynchronous
    /// counterpart of
    /// <see cref="Update(string, InputValues, WhereConditions, DbConnection, DbTransaction, bool)"/>;
    /// the generated SQL, parameters, the <c>force</c> guard, connection ownership,
    /// and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> UpdateAsync(string tableName, InputValues input, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null, bool force = false,
        CancellationToken cancellationToken = default)
    {
        var sql = Executor.SqlBuilder.UpdateWithParameters(tableName, input, where, force, out var dbParams);
        return Executor.NonQueryAsync(sql, dbParams, con, tran, cancellationToken);
    }

    public int Count(string tableName, WhereConditions where, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.CountWithParameters(tableName, where, columns, out var dbParams);
        return Executor.Scalar(sql, System.Convert.ToInt32, dbParams, con, tran);
    }

    /// <summary>
    /// Asynchronously counts records matching the condition. This is the asynchronous
    /// counterpart of
    /// <see cref="Count(string, WhereConditions, ColumnParam, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> CountAsync(string tableName, WhereConditions where, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        var sql = Executor.SqlBuilder.CountWithParameters(tableName, where, columns, out var dbParams);
        return Executor.ScalarAsync(sql, System.Convert.ToInt32, dbParams, con, tran, cancellationToken);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.SelectWithParameters(tableName, where, orderBy, null, columns, out var dbParams);
        return Executor.Reader(sql, convertor, dbParams, con, tran);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        PageParam pageParam, out int totalPages, out int totalCount,
        DbConnection con = null, DbTransaction tran = null)
    {
        totalPages = 0;
        totalCount = 0;
        if (pageParam != null)
        {
            totalCount = Count(tableName, where, columns, con, tran);
            if (totalCount > 0)
            {
                totalPages = totalCount / pageParam.PageSize;
                if (Math.Abs(totalCount % pageParam.PageSize) > 0)
                {
                    totalPages++;
                }
            }
        }

        var sql = Executor.SqlBuilder.SelectWithParameters(tableName, where, orderBy, pageParam, columns, out var dbParams);
        return Executor.Reader(sql, convertor, dbParams, con, tran);
    }

    private bool ExistTable(TableDefine tableDefine, DbConnection con)
    {
        if (Executor.TryGetLegacyExistTableParam(tableDefine, out var checkTable, out var where))
        {
            if (string.IsNullOrEmpty(checkTable) || where == null)
            {
                return false;
            }

            return Count(checkTable, where, con) > 0;
        }

        var sql = Executor.SqlBuilder.GetTableExistsSql(tableDefine, out var dbParams);
        return Executor.Scalar(sql, System.Convert.ToInt32, dbParams, con) > 0;
    }

    private async Task<bool> ExistTableAsync(TableDefine tableDefine, DbConnection con,
        CancellationToken cancellationToken)
    {
        if (Executor.TryGetLegacyExistTableParam(tableDefine, out var checkTable, out var where))
        {
            if (string.IsNullOrEmpty(checkTable) || where == null)
            {
                return false;
            }

            return await CountAsync(checkTable, where, null, con,
                cancellationToken: cancellationToken).ConfigureAwait(false) > 0;
        }

        var sql = Executor.SqlBuilder.GetTableExistsSql(tableDefine, out var dbParams);
        return await Executor.ScalarAsync(sql, System.Convert.ToInt32, dbParams, con,
            cancellationToken: cancellationToken).ConfigureAwait(false) > 0;
    }

}
