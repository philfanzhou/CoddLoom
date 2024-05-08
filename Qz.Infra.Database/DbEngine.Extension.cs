using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Convert;
using Qz.Infra.Database.Input;
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
        DbConverter.ToInsert(entity, out var table, out var input);
        Insert(table, input, con, tran);
    }

    public void Insert<T>(IEnumerable<T> entities, int chunkSize = 0,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        DbConverter.ToInsert(entities, out var table, out List<InputValues> inputs);
        Insert(table, inputs, chunkSize, con, tran);
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

    public List<T> Select<T>(WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = GetTableName<T>();
        return Select(DataRecordExtension.ToEntity<T>, table, where, orderBy, con, tran);
    }

    public T First<T>(WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = GetTableName<T>();
        return First(DataRecordExtension.ToEntity<T>, table, where, orderBy, con, tran);
    }

    public List<T> PageSelect<T>(PageParam pageParam, WhereConditions where, OrderByCondition orderBy, 
        out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = GetTableName<T>();
        return PageSelect(DataRecordExtension.ToEntity<T>, pageParam, table, where, orderBy, out totalPages, out totalCount,
            con, tran);
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