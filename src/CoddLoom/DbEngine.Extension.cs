using CoddLoom.Cache;
using CoddLoom.Condition;
using CoddLoom.Convert;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// <param name="tryCount">The maximum number of candidates to test. When the value is less
    /// than or equal to zero, no candidate is generated or tested.</param>
    /// <returns>An ID that was not present when the existence query ran.</returns>
    /// <exception cref="Exception">Thrown when <paramref name="tryCount"/> is less than or equal
    /// to zero, or when every generated candidate already exists in the column.</exception>
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
        var currentId = default(T);
        for (var i = 0; i < tryCount; i++)
        {
            currentId = generateId(currentId);

            var where = new WhereConditions();
            where.Add(columnName, currentId);
            if (Exist(tableName, where, con, tran) == false)
            {
                return currentId;
            }
        }

        throw new Exception($"Generate new {tableName}.{columnName} ID failed.");
    }

    /// <summary>
    /// Returns a candidate ID one greater than the first value returned by descending database
    /// ordering, after parsing that value as a <see cref="long"/>.
    /// </summary>
    /// <param name="tableName">The table containing the ID column.</param>
    /// <param name="columnName">The numerically ordered ID column to query.</param>
    /// <param name="con">An optional database connection used by the queries.</param>
    /// <param name="tran">An optional transaction used by the queries.</param>
    /// <returns>A candidate ID that was not present when queried, or <c>1</c> when the table is
    /// empty.</returns>
    /// <exception cref="FormatException">Thrown when the selected column value is not in a valid
    /// <see cref="long"/> format.</exception>
    /// <exception cref="OverflowException">Thrown when the selected column value is outside the
    /// range of <see cref="long"/>, or when it equals <see cref="long.MaxValue"/> and cannot be
    /// incremented.</exception>
    /// <exception cref="Exception">Thrown when the single generated candidate already exists in
    /// the column.</exception>
    /// <remarks>
    /// The database performs the descending ordering, so use this method only with a non-nullable
    /// column whose database type sorts numerically: a text column yields a lexicographic maximum,
    /// and a NULL sorts first on providers that default to <c>NULLS FIRST</c>. The maximum-value
    /// query and the existence query are not atomic, and the method makes a single attempt: if the
    /// candidate already exists it throws rather than recomputing the maximum.
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
            var orderBy = new OrderByCondition(columnName, true);
            var maxInTable = First(record => long.Parse(record[columnName].ToString()),
                tableName, null, orderBy, con, tran);
            return checked(maxInTable + 1);
        }, con, tran, 1);
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
            return time.ToString("yyMMddHHmmss") + new Random().Next(100, 999).ToString().PadRight(3, '0');
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
