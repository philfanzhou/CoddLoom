using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Convert;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System;
using System.Collections.Generic;
using System.Data;

namespace Qz.Infra.Database;

public static class DbEngineExtension
{
    public static void Insert<T>(this DbEngine self, T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        self.Insert(DbConverter.ToInsert(entity), con, tran);
    }

    public static void Delete<T>(this DbEngine self, string keyValue,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var where = GetKeyWhere<T>(keyValue);
        self.Delete(new SqlBuilderDeleteParam<T>(where), con, tran);
    }

    public static void Update<T>(this DbEngine self, T entity,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        self.Update(DbConverter.ToUpdate(entity), con, tran);
    }

    public static bool Exist(this DbEngine self, SqlBuilderCountParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return self.Count(builderParam, con, tran) > 0;
    }

    public static List<T> Select<T>(this DbEngine self, SqlBuilderSelectParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return self.Select(DbConverter.ToEntity<T>, builderParam, con, tran);
    }

    public static List<T> Select<T>(this DbEngine self,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var entityMap = EntityMapCache.Get<T>();
        var builderParam = new SqlBuilderSelectParam(entityMap.Table.Name);
        return self.Select<T>(builderParam, con, tran);
    }

    public static List<T> Select<T>(this DbEngine self, string keyValue,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var where = GetKeyWhere<T>(keyValue);
        var builderParam = new SqlBuilderSelectParam<T>(where);
        return self.Select<T>(builderParam, con, tran);
    }

    public static T First<T>(this DbEngine self, SqlBuilderSelectParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return self.First(DbConverter.ToEntity<T>, builderParam, con, tran);
    }

    public static List<T> PageSelect<T>(this DbEngine self,
        PageParam pageParam, SqlBuilderSelectParam builderParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return self.PageSelect(DbConverter.ToEntity<T>, pageParam, builderParam, out totalPages, out totalCount,
            con, tran);
    }

    public static string GenerateTimeStampId(this DbEngine self, 
        string tableName, string columnName,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var getNewId = new Func<string>(() => DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(100, 999));

        var newId = getNewId();
        var where = new WhereConditions();
        where.Add(columnName, newId);
        var sqlBuilderParam = new SqlBuilderCountParam(tableName, where);

        var tryCount = 0;
        while (self.Exist(sqlBuilderParam, con, tran))
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

    private static WhereConditions GetKeyWhere<T>(string keyValue)
    {
        if (string.IsNullOrEmpty(keyValue))
        {
            throw new ArgumentNullException(nameof(keyValue));
        }

        var entityMap = EntityMapCache.Get<T>();
        if (string.IsNullOrEmpty(entityMap.PrimaryKey))
        {
            throw new ArgumentException($"{nameof(T)} does not have a primary key");
        }

        var where = new WhereConditions();
        where.Add(entityMap.PrimaryKey, keyValue);
        return where;
    }
}