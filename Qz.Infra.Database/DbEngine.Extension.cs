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
        var table = GetTableName<T>();
        var inputValues = DbConverter.ToInsert(entity);
        Insert(table, inputValues, con, tran);
    }

    public void Delete<T>(string primaryKeyValue,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var table = GetTableName<T>();
        var where = GetPrimaryKeyCondition<T>(primaryKeyValue);
        Delete(table, where, con, tran);
    }

    public void Update<T>(T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var table = GetTableName<T>();
        DbConverter.ToUpdate(entity, out var input, out var where);
        Update(table, input, where, con, tran);
    }

    public List<T> Select<T>(WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = GetTableName<T>();
        return Select(DbConverter.ToEntity<T>, table, where, orderBy, con, tran);
    }

    public T First<T>(WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = GetTableName<T>();
        return First(DbConverter.ToEntity<T>, table, where, orderBy, con, tran);
    }

    public List<T> PageSelect<T>(PageParam pageParam, WhereConditions where, OrderByCondition orderBy, 
        out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var table = GetTableName<T>();
        return PageSelect(DbConverter.ToEntity<T>, pageParam, table, where, orderBy, out totalPages, out totalCount,
            con, tran);
    }

    public string GenerateTimeStampId(string tableName, string columnName,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var format = "yyMMddHHmmss";
        var getNewId = new Func<string>(() => DateTime.UtcNow.ToString(format) + new Random().Next(100, 999));

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

    private WhereConditions GetPrimaryKeyCondition<T>(string primaryKeyValue)
    {
        if (string.IsNullOrEmpty(primaryKeyValue))
        {
            throw new ArgumentNullException(nameof(primaryKeyValue));
        }

        var entityMap = EntityMapCache.Get<T>();
        if (string.IsNullOrEmpty(entityMap.PrimaryKey))
        {
            throw new ArgumentException($"{nameof(T)} does not have a primary key");
        }

        var where = new WhereConditions();
        where.Add(entityMap.PrimaryKey, primaryKeyValue);
        return where;
    }
}