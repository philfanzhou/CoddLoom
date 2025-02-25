using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Convert;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database;

partial class DbEngine
{
    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, 
        OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Select(convertor, tableName, where, orderBy, null, con, tran);
    }

    public List<T> Select<T>(WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var tableName = EntityMapCache.GetTableName<T>();
        return Select(DbConverter.ToEntity<T>, tableName, where, orderBy, columns, con, tran);
    }

    public List<T> Select<T>(WhereConditions where,
        OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return Select<T>(where, orderBy, null, con, tran);
    }

    public T Select<T>(object id,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        var where = WhereConditions.Create<T>(id, out var tableName);
        var ret = Select(DbConverter.ToEntity<T>, tableName, where, null, null, con, tran);
        
        if(ret == null || ret.Count == 0)
        {
            return default(T);
        }

        return ret.Single();
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, JoinConditions join, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Select(convertor, join.GetTableName(Executor.SqlBuilder),
            where, orderBy, columns, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, JoinConditions join, WhereConditions where, 
        OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Select(convertor, join, 
            where, orderBy, null, con, tran);
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
        return PageSelect(DbConverter.ToEntity<T>,
            table, where, orderBy, columns, pageParam, out totalPages, out totalCount, con, tran);
    }

    public List<T> PageSelect<T>(WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
        where T : new()
    {
        return PageSelect<T>(where, orderBy, null, pageParam, out totalPages, out totalCount, con, tran);
    }
}