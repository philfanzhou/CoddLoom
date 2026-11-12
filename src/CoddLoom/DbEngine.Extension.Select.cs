using CoddLoom.Cache;
using CoddLoom.Condition;
using CoddLoom.Convert;
using CoddLoom.Model;
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
    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, 
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Select(convertor, tableName, where, orderBy, null, con, tran);
    }

    /// <summary>
    /// Asynchronously queries records and maps every row with the convertor. This is
    /// the asynchronous counterpart of
    /// <see cref="Select{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical, and the convertor keeps the synchronous
    /// <see cref="IDataRecord"/> signature.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> SelectAsync<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return SelectAsync(convertor, tableName, where, orderBy, null, con, tran, cancellationToken);
    }

    public List<T> Select<T>(WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
        where T : new()
    {
        var tableName = EntityMapCache.GetTableName<T>();
        return Select(DbConverter.ToEntity<T>, tableName, where, orderBy, columns, con, tran);
    }

    /// <summary>
    /// Asynchronously queries mapped entities matching the condition. This is the
    /// asynchronous counterpart of
    /// <see cref="Select{T}(WhereConditions, OrderByCondition, ColumnParam, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> SelectAsync<T>(WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
        where T : new()
    {
        var tableName = EntityMapCache.GetTableName<T>();
        return SelectAsync(DbConverter.ToEntity<T>, tableName, where, orderBy, columns, con, tran, cancellationToken);
    }

    public List<T> Select<T>(WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null)
        where T : new()
    {
        return Select<T>(where, orderBy, null, con, tran);
    }

    /// <summary>
    /// Asynchronously queries mapped entities matching the condition. This is the
    /// asynchronous counterpart of
    /// <see cref="Select{T}(WhereConditions, OrderByCondition, DbConnection, DbTransaction)"/>;
    /// the generated SQL, parameters, connection ownership, and exception types are
    /// identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> SelectAsync<T>(WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
        where T : new()
    {
        return SelectAsync<T>(where, orderBy, null, con, tran, cancellationToken);
    }

    public T SelectById<T>(object id,
        DbConnection con = null, DbTransaction tran = null)
        where T : new()
    {
        var where = WhereConditions.ById<T>(id, out var tableName);
        var ret = Select(DbConverter.ToEntity<T>, tableName, where, null, null, con, tran);
        
        if(ret == null || ret.Count == 0)
        {
            return default(T);
        }

        return ret.Single();
    }

    /// <summary>
    /// Asynchronously queries the mapped entity with the given primary key. This is
    /// the asynchronous counterpart of
    /// <see cref="SelectById{T}(object, DbConnection, DbTransaction)"/>; the generated
    /// SQL, parameters, connection ownership, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public async Task<T> SelectByIdAsync<T>(object id,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
        where T : new()
    {
        var where = WhereConditions.ById<T>(id, out var tableName);
        var ret = await SelectAsync(DbConverter.ToEntity<T>, tableName, where, null, null,
            con, tran, cancellationToken).ConfigureAwait(false);

        if (ret == null || ret.Count == 0)
        {
            return default(T);
        }

        return ret.Single();
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, JoinConditions join, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Select(convertor, join.GetTableName(Executor.SqlBuilder),
            where, orderBy, columns, con, tran);
    }

    /// <summary>
    /// Asynchronously queries joined records and maps every row with the convertor.
    /// This is the asynchronous counterpart of
    /// <see cref="Select{T}(Func{IDataRecord, T}, JoinConditions, WhereConditions, OrderByCondition, ColumnParam, DbConnection, DbTransaction)"/>;
    /// the generated SQL (including the JOIN clause), parameters, connection
    /// ownership, and exception types are identical, and the convertor keeps the
    /// synchronous <see cref="IDataRecord"/> signature.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> SelectAsync<T>(Func<IDataRecord, T> convertor, JoinConditions join, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return SelectAsync(convertor, join.GetTableName(Executor.SqlBuilder),
            where, orderBy, columns, con, tran, cancellationToken);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, JoinConditions join, WhereConditions where, 
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null)
    {
        return Select(convertor, join, 
            where, orderBy, null, con, tran);
    }

    /// <summary>
    /// Asynchronously queries joined records and maps every row with the convertor.
    /// This is the asynchronous counterpart of
    /// <see cref="Select{T}(Func{IDataRecord, T}, JoinConditions, WhereConditions, OrderByCondition, DbConnection, DbTransaction)"/>;
    /// the generated SQL (including the JOIN clause), parameters, connection
    /// ownership, and exception types are identical, and the convertor keeps the
    /// synchronous <see cref="IDataRecord"/> signature.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before any database call
    /// and is then forwarded to the driver's asynchronous execution.
    /// </remarks>
    public Task<List<T>> SelectAsync<T>(Func<IDataRecord, T> convertor, JoinConditions join, WhereConditions where,
        OrderByCondition orderBy,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return SelectAsync(convertor, join,
            where, orderBy, null, con, tran, cancellationToken);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam, out int totalPages, out int totalCount,
        DbConnection con = null, DbTransaction tran = null)
    {
        return PageSelect(convertor,
            tableName, where, orderBy, null, pageParam, out totalPages, out totalCount, con, tran);
    }

    /// <summary>
    /// Asynchronously queries one page of records together with its pagination
    /// metadata. This is the asynchronous counterpart of
    /// <see cref="PageSelect{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, PageParam, out int, out int, DbConnection, DbTransaction)"/>;
    /// because asynchronous methods cannot use <c>out</c> parameters, the metadata
    /// is returned as a <see cref="PageResult{T}"/> instead, with the same
    /// COUNT-then-SELECT semantics as the synchronous version. The generated SQL,
    /// parameters, connection ownership, and exception types are identical, and the
    /// convertor keeps the synchronous <see cref="IDataRecord"/> signature.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before the COUNT and is
    /// then forwarded to both commands' asynchronous execution. See
    /// <see cref="PageSelectAsync{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, ColumnParam, PageParam, DbConnection, DbTransaction, CancellationToken)"/>
    /// for the return structure and the non-atomic semantics of the two commands.
    /// </remarks>
    public Task<PageResult<T>> PageSelectAsync<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
    {
        return PageSelectAsync(convertor,
            tableName, where, orderBy, null, pageParam, con, tran, cancellationToken);
    }

    public List<T> PageSelect<T>(WhereConditions where, OrderByCondition orderBy, ColumnParam columns,
        PageParam pageParam, out int totalPages, out int totalCount,
        DbConnection con = null, DbTransaction tran = null)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return PageSelect(DbConverter.ToEntity<T>,
            table, where, orderBy, columns, pageParam, out totalPages, out totalCount, con, tran);
    }

    /// <summary>
    /// Asynchronously queries one page of mapped entities together with its
    /// pagination metadata. This is the asynchronous counterpart of
    /// <see cref="PageSelect{T}(WhereConditions, OrderByCondition, ColumnParam, PageParam, out int, out int, DbConnection, DbTransaction)"/>;
    /// because asynchronous methods cannot use <c>out</c> parameters, the metadata
    /// is returned as a <see cref="PageResult{T}"/> instead, with the same
    /// COUNT-then-SELECT semantics as the synchronous version. The generated SQL,
    /// parameters, connection ownership, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before the COUNT and is
    /// then forwarded to both commands' asynchronous execution. See
    /// <see cref="PageSelectAsync{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, ColumnParam, PageParam, DbConnection, DbTransaction, CancellationToken)"/>
    /// for the return structure and the non-atomic semantics of the two commands.
    /// </remarks>
    public Task<PageResult<T>> PageSelectAsync<T>(WhereConditions where, OrderByCondition orderBy, ColumnParam columns,
        PageParam pageParam,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return PageSelectAsync(DbConverter.ToEntity<T>,
            table, where, orderBy, columns, pageParam, con, tran, cancellationToken);
    }

    public List<T> PageSelect<T>(WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam, out int totalPages, out int totalCount,
        DbConnection con = null, DbTransaction tran = null)
        where T : new()
    {
        return PageSelect<T>(where, orderBy, null, pageParam, out totalPages, out totalCount, con, tran);
    }

    /// <summary>
    /// Asynchronously queries one page of mapped entities together with its
    /// pagination metadata. This is the asynchronous counterpart of
    /// <see cref="PageSelect{T}(WhereConditions, OrderByCondition, PageParam, out int, out int, DbConnection, DbTransaction)"/>;
    /// because asynchronous methods cannot use <c>out</c> parameters, the metadata
    /// is returned as a <see cref="PageResult{T}"/> instead, with the same
    /// COUNT-then-SELECT semantics as the synchronous version. The generated SQL,
    /// parameters, connection ownership, and exception types are identical.
    /// </summary>
    /// <remarks>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before the COUNT and is
    /// then forwarded to both commands' asynchronous execution. See
    /// <see cref="PageSelectAsync{T}(Func{IDataRecord, T}, string, WhereConditions, OrderByCondition, ColumnParam, PageParam, DbConnection, DbTransaction, CancellationToken)"/>
    /// for the return structure and the non-atomic semantics of the two commands.
    /// </remarks>
    public Task<PageResult<T>> PageSelectAsync<T>(WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam,
        DbConnection con = null, DbTransaction tran = null,
        CancellationToken cancellationToken = default)
        where T : new()
    {
        return PageSelectAsync<T>(where, orderBy, null, pageParam, con, tran, cancellationToken);
    }
}