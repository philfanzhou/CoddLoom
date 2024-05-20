using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database;

public partial class DbEngine
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
                var sql = Executor.SqlBuilder.GetCreateTableSql(table);
                Executor.Execute(sql, null, null, con);
            }
        });
    }

    public DbExecutor Executor { get; }

    public void Insert(string tableName, InputValues input,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        Insert(tableName, new[] { input }, 10, con, tran);
    }

    public void Insert(string tableName, IEnumerable<InputValues> inputs, int batchSize, 
        IDbConnection con = null, IDbTransaction tran = null)
    {
        if (batchSize < 2)
        {
            batchSize = 10;
        }

        var tmpList = new List<InputValues>();
        foreach (var input in inputs)
        {
            tmpList.Add(input);
            if (tmpList.Count >= batchSize)
            {
                DoBatchInsert(tableName, tmpList, con, tran);
                tmpList.Clear();
            }
        }

        if (tmpList.Count > 0)
        {
            DoBatchInsert(tableName, tmpList, con, tran);
        }
    }

    public void Delete(string tableName, WhereConditions where, 
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Delete(tableName, where);
        Executor.Execute(sql, where.Parameters, null, con, tran);
    }

    public void Update(string tableName, InputValues input, WhereConditions where, 
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Update(tableName, input, where);
        var dbParams = new List<ValueParam>();
        dbParams.AddRange(input.Items);
        dbParams.AddRange(where.Parameters);
        Executor.Execute(sql, dbParams, null, con, tran);
    }

    public int Count(string tableName, WhereConditions where,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Count(tableName, where);
        return Executor.Count(sql, where?.Parameters, con, tran);
    }

    public T First<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.First(tableName, where, orderBy);
        return Executor.First(sql, convertor, where?.Parameters, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return PageSelect(convertor, tableName, where, orderBy, null, out _, out _, con, tran);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy, 
        PageParam pageParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        totalPages = 0;
        totalCount = 0;
        if (pageParam != null)
        {
            totalCount = Count(tableName, where, con, tran);
            if (totalCount > 0)
            {
                totalPages = totalCount / pageParam.PageSize;
                if (Math.Abs(totalCount % pageParam.PageSize) > 0)
                {
                    totalPages++;
                }
            }
        }
        
        var sql = Executor.SqlBuilder.Select(tableName, where, orderBy, pageParam);
        return Executor.Select(sql, convertor, where?.Parameters, con, tran);
    }

    private void DoBatchInsert(string tableName, IEnumerable<InputValues> inputs,
        IDbConnection con, IDbTransaction tran)
    {
        var inputList = inputs.ToList();

        var valuesCount = inputList.Count * inputList[0].Items.Count;
        var forceUseParameter = valuesCount < 2100; // sqlserver default parameter count limit.

        var sql = Executor.SqlBuilder.Insert(tableName, inputList, out var dbParams, forceUseParameter);
        Executor.Execute(sql, dbParams, null, con, tran);
    }
}