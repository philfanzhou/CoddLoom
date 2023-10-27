using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database;

public class DbEngine
{
    public DbEngine(DbExecutor executor, IEnumerable<TableDefine> tables)
    {
        Executor = executor;

        var tableList = tables.ToList();
        TableColumnsCache.Initialize(tableList);
        Executor.Execute(con =>
        {
            foreach (var table in tableList.Where(table => !Executor.ExistTable(table, con)))
            {
                Executor.Execute(Executor.SqlBuilder.GetCreateTableSql(table), null, null, con);
            }
        });
    }

    public DbExecutor Executor { get; }

    public void Insert(SqlBuilderInsertParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Insert(builderParam);
        Executor.Execute(sql, builderParam.Values.Items, null, con, tran);
    }

    public void Delete(SqlBuilderDeleteParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        if (builderParam.WhereConditions?.WhereParams?.Items == null
            || builderParam.WhereConditions.WhereParams.Items.Count < 1)
        {
            throw new ArgumentNullException(nameof(builderParam.WhereConditions));
        }
        var sql = Executor.SqlBuilder.Delete(builderParam);
        Executor.Execute(sql, builderParam.WhereConditions.WhereParams.Items, null, con, tran);
    }

    public void Update(SqlBuilderUpdateParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        if (builderParam.WhereConditions?.WhereParams?.Items == null
            || builderParam.WhereConditions.WhereParams.Items.Count < 1)
        {
            throw new ArgumentNullException(nameof(builderParam.WhereConditions));
        }

        if (builderParam.Values?.Items == null
            || builderParam.Values.Items.Count < 1)
        {
            throw new ArgumentNullException(nameof(builderParam.Values));
        }
        var sql = Executor.SqlBuilder.Update(builderParam);
        var dbParams = new List<ISqlParameter>();
        dbParams.AddRange(builderParam.Values.Items);
        dbParams.AddRange(builderParam.WhereConditions.WhereParams.Items);
        Executor.Execute(sql, dbParams, null, con, tran);
    }

    public int Count(SqlBuilderCountParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Count(builderParam);
        return Executor.Count(sql, builderParam.WhereConditions?.WhereParams?.Items, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Select(builderParam);
        return Executor.Select(sql, convertor, builderParam.WhereConditions?.WhereParams?.Items, con, tran);
    }

    public T First<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.First(builderParam);
        return Executor.First(sql, convertor, builderParam.WhereConditions?.WhereParams?.Items, con, tran);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor,
        PageParam pageParam, SqlBuilderSelectParam builderParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        totalCount = Count(builderParam, con, tran);
        totalPages = 0;
        if (totalCount > 0)
        {
            totalPages = totalCount / pageParam.PageSize;
            if (Math.Abs(totalCount % pageParam.PageSize) > 0)
            {
                totalPages++;
            }
        }

        var sql = Executor.SqlBuilder.Take(builderParam, pageParam.Offset, pageParam.PageSize);
        var items = Executor.Select(sql, convertor, builderParam.WhereConditions?.WhereParams?.Items, con, tran).ToList();
        return items;
    }
}