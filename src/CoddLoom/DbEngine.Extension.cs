using CoddLoom.Cache;
using CoddLoom.Condition;
using CoddLoom.Convert;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace CoddLoom;

partial class DbEngine
{
    private const string ConcurrentIdGenerationObsoleteMessage =
        "This check-then-return API cannot guarantee uniqueness under concurrency. " +
        "Prefer a database identity/sequence, UUID, or unique-constraint-protected inserts with retry handling.";

    public int Insert<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToInsert(entity, _tableColumnsCache, out var table, out var inputs);
        return Insert(table, inputs.First(), con, tran);
    }

    public int Insert<T>(IEnumerable<T> entities, int batchSize,
        IDbTransaction tran = null)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be greater than 0.");
        }

        DbConverter.ToInsert(entities, _tableColumnsCache, out var table, out var inputs);
        return Insert(table, inputs, batchSize, tran);
    }

    public int Delete<T>(object id,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var where = WhereConditions.ById<T>(id, out var tableName);
        return Delete(tableName, where, con, tran);
    }

    public int Update<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToUpdate(entity, _tableColumnsCache, out var table, out var input, out var where);
        return Update(table, input, where, con, tran);
    }

    public int Count(string tableName, WhereConditions where,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Count(tableName, where, null, con, tran);
    }

    public bool Exist(string tableName, WhereConditions where,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Count(tableName, where, null, con, tran) > 0;
    }

    public T First<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var pageParam = new PageParam { PageSize = 1, PageNumber = 1 };
        var sql = Executor.SqlBuilder.Select(tableName, where, orderBy, pageParam, columns);
        return Executor.Reader(sql, convertor, where?.Parameters, con, tran)
            .FirstOrDefault();
    }

    public T First<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return First(convertor, tableName, where, orderBy, null, con, tran);
    }

    public T First<T>(WhereConditions where,
        OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return First(DbConverter.ToEntity<T>, table, where, orderBy, con, tran);
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
        IDbConnection con = null, IDbTransaction tran = null, int tryCount = 10)
    {
        return GenerateId(tableName, columnName, generateId, null, con, tran, tryCount);
    }

    private T GenerateId<T>(string tableName, string columnName, Func<T, T> generateId,
        DbType? candidateCast, IDbConnection con = null, IDbTransaction tran = null,
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
        IDbConnection con = null, IDbTransaction tran = null)
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
    /// granularity and the suffix has only 899 possible values (100 through 998), so a retry can
    /// repeat a candidate. On .NET Framework, where rapidly constructed <see cref="Random"/>
    /// instances share a time-based seed, every retry within one seed tick repeats it.
    /// See the "ID generation and concurrency" section of the README for the contract shared by
    /// the four <c>Generate*Id</c> methods, the <paramref name="con"/> and <paramref name="tran"/>
    /// rules, and safer alternatives.
    /// </remarks>
    [Obsolete(ConcurrentIdGenerationObsoleteMessage, false)]
    public string GenerateTimeId(string tableName, string columnName, Func<DateTime> getTime,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return GenerateId<string>(tableName, columnName, _ =>
        {
            var time = getTime();
            return time.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture)
                + new Random().Next(100, 999).ToString().PadRight(3, '0');
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
    /// granularity and the suffix has only 899 possible values (100 through 998), so a retry can
    /// repeat a candidate. On .NET Framework, where rapidly constructed <see cref="Random"/>
    /// instances share a time-based seed, every retry within one seed tick repeats it.
    /// See the "ID generation and concurrency" section of the README for the contract shared by
    /// the four <c>Generate*Id</c> methods, the <paramref name="con"/> and <paramref name="tran"/>
    /// rules, and safer alternatives.
    /// </remarks>
    [Obsolete(ConcurrentIdGenerationObsoleteMessage, false)]
    public string GenerateUtcTimeId(string tableName, string columnName,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return GenerateTimeId(tableName, columnName, () => DateTime.UtcNow, con, tran);
    }

    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public double GetUtcTimeStamp()
    {
        var utcNow = DateTime.UtcNow;
        var span = utcNow - UnixEpoch;
        return span.TotalMilliseconds;
    }
}
