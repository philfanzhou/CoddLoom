using CoddLoom.Cache;
using CoddLoom.Condition;
using CoddLoom.Convert;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom;

partial class DbEngine
{

    public int Insert<T>(T entity,
        DbConnection con = null, DbTransaction tran = null)
    {
        DbConverter.ToInsert(entity, _tableColumnsCache, out var table, out var inputs);
        return Insert(table, inputs.First(), con, tran);
    }

    /// <summary>
    /// Asynchronously inserts one mapped entity. This is the asynchronous counterpart of
    /// <see cref="Insert{T}(T, DbConnection, DbTransaction)"/>; the generated SQL,
    /// parameters, connection ownership, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> InsertAsync<T>(T entity,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        DbConverter.ToInsert(entity, _tableColumnsCache, out var table, out var inputs);
        return InsertAsync(table, inputs.First(), con, tran, cancellationToken);
    }

    public int Insert<T>(IEnumerable<T> entities, int batchSize,
        DbTransaction tran = null)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be greater than 0.");
        }

        DbConverter.ToInsert(entities, _tableColumnsCache, out var table, out var inputs);
        return Insert(table, inputs, batchSize, tran);
    }

    /// <summary>
    /// Asynchronously inserts multiple mapped entities in batches. This is the
    /// asynchronous counterpart of
    /// <see cref="Insert{T}(IEnumerable{T}, int, DbTransaction)"/>; the batch splitting,
    /// the wrapping transaction, and the exception types are identical.
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
    public Task<int> InsertAsync<T>(IEnumerable<T> entities, int batchSize,
        DbTransaction tran = null, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be greater than 0.");
        }

        DbConverter.ToInsert(entities, _tableColumnsCache, out var table, out var inputs);
        return InsertAsync(table, inputs, batchSize, tran, cancellationToken);
    }

    public int Delete<T>(object id,
        DbConnection con = null, DbTransaction tran = null)
    {
        var where = WhereConditions.ById<T>(id, out var tableName);
        return Delete(tableName, where, con, tran);
    }

    /// <summary>
    /// Asynchronously deletes the entity with the given primary key. This is the
    /// asynchronous counterpart of <see cref="Delete{T}(object, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> DeleteAsync<T>(object id,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        var where = WhereConditions.ById<T>(id, out var tableName);
        return DeleteAsync(tableName, where, con, tran, cancellationToken: cancellationToken);
    }

    public int Update<T>(T entity,
        DbConnection con = null, DbTransaction tran = null)
    {
        DbConverter.ToUpdate(entity, _tableColumnsCache, out var table, out var input, out var where);
        return Update(table, input, where, con, tran);
    }

    /// <summary>
    /// Asynchronously updates the mapped entity's row by its primary key. This is the
    /// asynchronous counterpart of <see cref="Update{T}(T, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> UpdateAsync<T>(T entity,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        DbConverter.ToUpdate(entity, _tableColumnsCache, out var table, out var input, out var where);
        return UpdateAsync(table, input, where, con, tran, cancellationToken: cancellationToken);
    }

    public int Count(string tableName, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Count(tableName, where, null, con, tran);
    }

    /// <summary>
    /// Asynchronously counts records matching the condition. This is the asynchronous
    /// counterpart of <see cref="Count(string, WhereConditions, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<int> CountAsync(string tableName, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return CountAsync(tableName, where, null, con, tran, cancellationToken);
    }

    public bool Exist(string tableName, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Count(tableName, where, null, con, tran) > 0;
    }

    /// <summary>
    /// Asynchronously checks whether any record matches the condition. This is the
    /// asynchronous counterpart of
    /// <see cref="Exist(string, WhereConditions, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public async Task<bool> ExistAsync(string tableName, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return await CountAsync(tableName, where, null, con, tran, cancellationToken)
            .ConfigureAwait(false) > 0;
    }

    public T First<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
    {
        var pageParam = new PageParam { PageSize = 1, PageNumber = 1 };
        var sql = Executor.SqlBuilder.SelectWithParameters(tableName, where, orderBy, pageParam, columns, out var dbParams);
        return Executor.Reader(sql, convertor, dbParams, con, tran)
            .FirstOrDefault();
    }

    public T First<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null)
    {
        return First(convertor, tableName, where, orderBy, null, con, tran);
    }

    public T First<T>(WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return First(DbConverter.ToEntity<T>, table, where, orderBy, con, tran);
    }

    /// <summary>
    /// Asynchronously queries the first record matching the condition and maps it
    /// with the convertor. This is the asynchronous counterpart of
    /// <see cref="First{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, ColumnParam, DbConnection, DbTransaction)"/>;
    /// the generated SQL (including the page of size one), parameters, connection
    /// ownership, and exception types are identical, and the convertor keeps the
    /// synchronous <see cref="IDataRecord"/> signature.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public async Task<T> FirstAsync<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        var pageParam = new PageParam { PageSize = 1, PageNumber = 1 };
        var sql = Executor.SqlBuilder.SelectWithParameters(tableName, where, orderBy, pageParam, columns, out var dbParams);
        var rows = await Executor.ReaderAsync(sql, convertor, dbParams, con, tran,
            cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault();
    }

    /// <summary>
    /// Asynchronously queries the first record matching the condition and maps it
    /// with the convertor. This is the asynchronous counterpart of
    /// <see cref="First{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<T> FirstAsync<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return FirstAsync(convertor, tableName, where, orderBy, null, con, tran, cancellationToken);
    }

    /// <summary>
    /// Asynchronously queries the first mapped entity matching the condition. This
    /// is the asynchronous counterpart of
    /// <see cref="First{T}(WhereConditions, OrderByCondition, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<T> FirstAsync<T>(WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return FirstAsync(DbConverter.ToEntity<T>, table, where, orderBy, con, tran, cancellationToken);
    }

    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public double GetUtcTimeStamp()
    {
        var utcNow = DateTime.UtcNow;
        var span = utcNow - UnixEpoch;
        return span.TotalMilliseconds;
    }
}
