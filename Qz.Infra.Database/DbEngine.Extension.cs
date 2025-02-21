using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Convert;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database;

partial class DbEngine
{
    public void Insert(string tableName, InputValues input,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        Insert(tableName, new[] { input }, 10, con, tran);
    }

    public void Insert<T>(IEnumerable<T> entities, int batchSize,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToInsert(entities, out var table, out var inputs);
        Insert(table, inputs, batchSize, con, tran);
    }

    public void Insert<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        Insert(new[] { entity }, 10, con, tran);
    }

    public int Delete<T>(object id,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var where = WhereConditions.Create<T>(id, out var tableName);
        return Delete(tableName, where, con, tran);
    }

    public int Update<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToUpdate(entity, out var table, out var input, out var where);
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
        return PageSelect(convertor, tableName, where, orderBy, columns, pageParam, out var _, out var _, con, tran)
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

    public string GenerateTimeId(string tableName, string columnName, Func<DateTime> getTime,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return GenerateId<string>(tableName, columnName, _ =>
        {
            var time = getTime();
            return time.ToString("yyMMddHHmmss") + new Random().Next(100, 999).ToString().PadRight(3, '0');
        }, con, tran);
    }

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