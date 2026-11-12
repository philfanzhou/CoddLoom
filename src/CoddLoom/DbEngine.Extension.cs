using CoddLoom.Cache;
using CoddLoom.Condition;
using CoddLoom.Convert;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom;

partial class DbEngine
{
    private const string ConcurrentIdGenerationObsoleteMessage =
        "This check-then-return API cannot guarantee uniqueness under concurrency. " +
        "Prefer a database identity/sequence, UUID, or unique-constraint-protected inserts with retry handling.";

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

    /// <summary>
    /// Generates a candidate ID and returns it when the ID is not present at query time.
    /// </summary>
    /// <typeparam name="T">The type of the ID.</typeparam>
    /// <param name="tableName">The table to query for an existing ID.</param>
    /// <param name="columnName">The ID column to query.</param>
    /// <param name="generateId">A function that produces each candidate ID. It receives the
    /// previously generated candidate, or <c>default(T)</c> on the first call.</param>
    /// <param name="con">An optional database connection used by the existence query.</param>
    /// <param name="tran">An optional transaction used by the existence query.</param>
    /// <param name="tryCount">The maximum number of candidates to test. Must be greater than
    /// zero.</param>
    /// <returns>An ID that was not present when the existence query ran.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tryCount"/> is
    /// less than or equal to zero.</exception>
    /// <exception cref="Exception">Thrown when every generated candidate already exists in the
    /// column.</exception>
    /// <remarks>
    /// This method does not reserve the returned value or guarantee that a later insert will
    /// succeed.
    /// See the "ID generation and concurrency" section of the README for the contract shared by
    /// the four <c>Generate*Id</c> methods, the <paramref name="con"/> and <paramref name="tran"/>
    /// rules, and safer alternatives.
    /// </remarks>
    [Obsolete(ConcurrentIdGenerationObsoleteMessage, false)]
    public T GenerateId<T>(string tableName, string columnName, Func<T, T> generateId,
        DbConnection con = null, DbTransaction tran = null, int tryCount = 10)
    {
        return GenerateId(tableName, columnName, generateId, null, con, tran, tryCount);
    }

    private T GenerateId<T>(string tableName, string columnName, Func<T, T> generateId,
        DbType? candidateCast, DbConnection con = null, DbTransaction tran = null,
        int tryCount = 10)
    {
        if (tryCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tryCount),
                "Try count must be greater than 0.");
        }

        var currentId = default(T);
        for (var i = 0; i < tryCount; i++)
        {
            currentId = generateId(currentId);

            var where = new WhereConditions();
            where.Add(columnName, currentId, candidateCast);
            if (Exist(tableName, where, con, tran) == false)
            {
                return currentId;
            }
        }

        throw new Exception($"Generate new {tableName}.{columnName} ID failed.");
    }

    /// <summary>
    /// Returns a candidate ID one greater than the maximum value after the database converts the
    /// column to its signed 64-bit integer type.
    /// </summary>
    /// <param name="tableName">The table containing the ID column.</param>
    /// <param name="columnName">The numeric or numeric-text ID column to query.</param>
    /// <param name="con">An optional database connection used by the queries.</param>
    /// <param name="tran">An optional transaction used by the queries.</param>
    /// <returns>A candidate ID that was not present when queried, or <c>1</c> when the table is empty
    /// or the column contains only <see langword="null"/> values.</returns>
    /// <exception cref="OverflowException">Thrown when the maximum value equals
    /// <see cref="long.MaxValue"/> and cannot be incremented.</exception>
    /// <exception cref="Exception">Thrown when all ten generated candidates already exist in the
    /// column.</exception>
    /// <remarks>
    /// The database converts values to its signed 64-bit integer type before computing the maximum,
    /// so numeric text is compared numerically and <see langword="null"/> values are ignored.
    /// Non-numeric text follows the provider's conversion behavior and may fail with a
    /// provider-specific exception or convert to a provider-specific value. The maximum-value query
    /// and each existence query are not atomic. When a candidate already exists, the method
    /// recomputes the maximum and retries, up to ten candidates.
    /// See the "ID generation and concurrency" section of the README for the contract shared by
    /// the four <c>Generate*Id</c> methods, the <paramref name="con"/> and <paramref name="tran"/>
    /// rules, and safer alternatives.
    /// </remarks>
    [Obsolete(ConcurrentIdGenerationObsoleteMessage, false)]
    public long GenerateMaxId(string tableName, string columnName,
        DbConnection con = null, DbTransaction tran = null)
    {
        return GenerateId<long>(tableName, columnName, _ =>
        {
            var columns = new ColumnParam().AddSelect(columnName, "MAX", DbType.Int64);
            var sql = Executor.SqlBuilder.Select(tableName, columns: columns);
            var maxInTable = Executor.Scalar(sql, System.Convert.ToInt64, con: con, tran: tran);
            return checked(maxInTable + 1);
        }, DbType.Int64, con, tran);
    }

    /// <summary>
    /// Generates a time-based candidate ID and returns it when it is not present at query time.
    /// </summary>
    /// <param name="tableName">The table to query for an existing ID.</param>
    /// <param name="columnName">The ID column to query.</param>
    /// <param name="getTime">A function that supplies the time portion of the candidate.</param>
    /// <param name="con">An optional database connection used by the existence query.</param>
    /// <param name="tran">An optional transaction used by the existence query.</param>
    /// <returns>A candidate ID that was not present when queried.</returns>
    /// <exception cref="Exception">Thrown when all ten generated candidates already exist in the
    /// column.</exception>
    /// <remarks>
    /// The 12-digit timestamp prefix uses the invariant culture's Gregorian calendar.
    /// The time and random components do not guarantee uniqueness: the timestamp has second
    /// granularity and the suffix has 1000 possible values (<c>000</c> through <c>999</c>), so a
    /// retry can randomly repeat a candidate. Each call uses one local <see cref="Random"/> for all
    /// of its retries.
    /// See the "ID generation and concurrency" section of the README for the contract shared by
    /// the four <c>Generate*Id</c> methods, the <paramref name="con"/> and <paramref name="tran"/>
    /// rules, and safer alternatives.
    /// </remarks>
    [Obsolete(ConcurrentIdGenerationObsoleteMessage, false)]
    public string GenerateTimeId(string tableName, string columnName, Func<DateTime> getTime,
        DbConnection con = null, DbTransaction tran = null)
    {
        var random = new Random();
        return GenerateId<string>(tableName, columnName, _ =>
        {
            var time = getTime();
            return time.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture)
                + FormatTimeIdSuffix(random.Next(0, 1000));
        }, con, tran);
    }

    /// <summary>
    /// Generates a UTC time-based candidate ID and returns it when it is not present at query time.
    /// </summary>
    /// <param name="tableName">The table to query for an existing ID.</param>
    /// <param name="columnName">The ID column to query.</param>
    /// <param name="con">An optional database connection used by the existence query.</param>
    /// <param name="tran">An optional transaction used by the existence query.</param>
    /// <returns>A candidate ID that was not present when queried.</returns>
    /// <exception cref="Exception">Thrown when all ten generated candidates already exist in the
    /// column.</exception>
    /// <remarks>
    /// The 12-digit UTC timestamp prefix uses the invariant culture's Gregorian calendar.
    /// The UTC time and random components do not guarantee uniqueness: the timestamp has second
    /// granularity and the suffix has 1000 possible values (<c>000</c> through <c>999</c>), so a
    /// retry can randomly repeat a candidate. Each call uses one local <see cref="Random"/> for all
    /// of its retries.
    /// See the "ID generation and concurrency" section of the README for the contract shared by
    /// the four <c>Generate*Id</c> methods, the <paramref name="con"/> and <paramref name="tran"/>
    /// rules, and safer alternatives.
    /// </remarks>
    [Obsolete(ConcurrentIdGenerationObsoleteMessage, false)]
    public string GenerateUtcTimeId(string tableName, string columnName,
        DbConnection con = null, DbTransaction tran = null)
    {
        return GenerateTimeId(tableName, columnName, () => DateTime.UtcNow, con, tran);
    }

    internal static string FormatTimeIdSuffix(int suffix)
    {
        return suffix.ToString("D3", CultureInfo.InvariantCulture);
    }

    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public double GetUtcTimeStamp()
    {
        var utcNow = DateTime.UtcNow;
        var span = utcNow - UnixEpoch;
        return span.TotalMilliseconds;
    }
}
