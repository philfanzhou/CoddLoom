using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Convert;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;

namespace Qz.Infra.Database;

partial class DbEngine
{
    public void Insert<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        Insert(new[] { entity }, 10, con, tran);
    }

    public void Insert<T>(IEnumerable<T> entities, int batchSize,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToInsert(entities, out var table, out var inputs);
        Insert(table, inputs, batchSize, con, tran);
    }

    public void Delete<T>(string primaryKeyValue,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToDelete<T>(primaryKeyValue, out var table, out var where);
        Delete(table, where, con, tran);
    }

    public void Update<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToUpdate(entity, out var table, out var input, out var where);
        Update(table, input, where, con, tran);
    }

    public int Count(string tableName, WhereConditions where,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Count(tableName, where, null, con, tran);
    }

    public bool Exist(string tableName, WhereConditions where, ColumnParam columns, 
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Count(tableName, where, columns, con, tran) > 0;
    }

    public bool Exist(string tableName, WhereConditions where,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Exist(tableName, where, null, con, tran);
    }

    public T First<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return First(convertor, tableName, where, orderBy, null, con, tran);
    }

    public T First<T>(WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return First(RecordHelper.ToEntity<T>, table, where, orderBy, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Select(convertor, 
            tableName, where, orderBy, null, con, tran);
    }

    public List<T> Select<T>(WhereConditions where, OrderByCondition orderBy, ColumnParam columns, 
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return Select(RecordHelper.ToEntity<T>, 
            table, where, orderBy, columns, con, tran);
    }

    public List<T> Select<T>(WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return Select<T>(where, orderBy, null, con, tran);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return PageSelect(convertor, 
            tableName, where, orderBy, null, pageParam, out totalPages, out totalCount, con, tran);
    }

    public List<T> PageSelect<T>(WhereConditions where, OrderByCondition orderBy, ColumnParam columns,
        PageParam pageParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = EntityMapCache.GetTableName<T>();
        return PageSelect(RecordHelper.ToEntity<T>,
            table, where, orderBy, columns, pageParam, out totalPages, out totalCount, con, tran);
    }

    public List<T> PageSelect<T>(WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return PageSelect<T>(where, orderBy, null, pageParam, out totalPages, out totalCount, con, tran);
    }

    public string GenerateUtcTimeStampId(string tableName, string columnName,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return GenerateTimeStampId(tableName, columnName, () => DateTime.UtcNow, con, tran);
    }

    public string GenerateTimeStampId(string tableName, string columnName, Func<DateTime> getTime,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var format = "yyMMddHHmmss";
        var getNewId = new Func<string>(() =>
        {
            var time = getTime();
            return time.ToString(format) + new Random().Next(100, 999).ToString().PadRight(3, '0');
        });

        var newId = getNewId();
        var where = new WhereConditions();
        where.Add(columnName, newId);

        var tryCount = 0;
        while (Exist(tableName, where, con, tran))
        {
            if (tryCount > 10)
            {
                throw new Exception($"Generate new {tableName}.{columnName} ID failed.");
            }

            newId = getNewId();
            where = new WhereConditions();
            where.Add(columnName, newId);
            tryCount++;
        }

        return newId;
    }

    public int GenerateMaxId(string tableName, string columnName,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var orderBy = new OrderByCondition(columnName, true);
        var max = First(record => int.Parse(record[columnName].ToString()),
            tableName, null, orderBy, con, tran);
        return checked(max + 1);
    }
}