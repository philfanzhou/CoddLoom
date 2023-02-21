using Qz.Infra.Database.Cache;
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
        var whereParams = GetKeyWhereParam<T>(keyValue);
        self.Delete(new SqlBuilderDeleteParam<T>(whereParams), con, tran);
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
        var whereParams = GetKeyWhereParam<T>(keyValue);
        var builderParam = new SqlBuilderSelectParam<T>(whereParams);
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

    private static WhereParams GetKeyWhereParam<T>(string keyValue)
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

        var whereParams = new WhereParams(entityMap.PrimaryKey, keyValue);
        return whereParams;
    }
}