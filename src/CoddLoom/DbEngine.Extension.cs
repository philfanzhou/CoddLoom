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
    /// <param name="generateId">A function that generates each candidate ID.</param>
    /// <param name="con">An optional database connection used by the existence query.</param>
    /// <param name="tran">An optional transaction used by the existence query.</param>
    /// <param name="tryCount">The maximum number of candidates to test.</param>
    /// <returns>An ID that was not present when the existence query ran.</returns>
    /// <remarks>
    /// This method does not reserve the returned value or guarantee that a later insert will
    /// succeed. Concurrent callers can receive the same candidate between the existence query
    /// and the caller's insert. Supplying a connection or transaction does not eliminate that
    /// race. Prefer a database identity or sequence, a UUID, or an insert protected by a unique
    /// constraint with retry handling when concurrent uniqueness is required.
    /// </remarks>
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
    /// Returns a candidate ID one greater than the current maximum value in a column.
    /// </summary>
    /// <param name="tableName">The table containing the ID column.</param>
    /// <param name="columnName">The ID column to query.</param>
    /// <param name="con">An optional database connection used by the queries.</param>
    /// <param name="tran">An optional transaction used by the queries.</param>
    /// <returns>A candidate ID that was not present when queried.</returns>
    /// <remarks>
    /// This method uses a non-atomic maximum-value query followed by an existence query. It does
    /// not reserve the returned value, and concurrent callers can receive the same ID before
    /// either caller inserts it. Supplying a connection or transaction does not eliminate the
    /// race between returning the ID and the caller's insert. Prefer a database identity or
    /// sequence, a UUID, or an insert protected by a unique constraint with retry handling.
    /// </remarks>
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
    /// <remarks>
    /// The time and random components do not guarantee uniqueness. This method does not reserve
    /// the returned value, and concurrent callers can receive the same candidate before either
    /// caller inserts it. Supplying a connection or transaction does not eliminate that race.
    /// Prefer a database identity or sequence, a UUID, or an insert protected by a unique
    /// constraint with retry handling when concurrent uniqueness is required.
    /// </remarks>
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
    /// <remarks>
    /// The UTC time and random components do not guarantee uniqueness. This method does not
    /// reserve the returned value, and concurrent callers can receive the same candidate before
    /// either caller inserts it. Supplying a connection or transaction does not eliminate that
    /// race. Prefer a database identity or sequence, a UUID, or an insert protected by a unique
    /// constraint with retry handling when concurrent uniqueness is required.
    /// </remarks>
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
