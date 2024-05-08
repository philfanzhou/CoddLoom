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
        var sql = Executor.SqlBuilder.Insert(tableName, input);
        Executor.Execute(sql, input.Items, null, con, tran);
    }

    public void BatchInsert(string tableName, IEnumerable<InputValues> inputs,
        IDbConnection con = null, IDbTransaction tran = null, int chunkSize = 0)
    {
        if(chunkSize < 1)
        {
            chunkSize = 500;
        }

        var tmpList = new List<InputValues>();
        foreach (var input in inputs)
        {
            if (tmpList.Count < chunkSize)
            {
                tmpList.Add(input);
            }
            else
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
        var dbParams = new List<ColumnValueParameter>();
        dbParams.AddRange(input.Items);
        dbParams.AddRange(where.Parameters);
        Executor.Execute(sql, dbParams, null, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where, OrderByCondition orderBy,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Select(tableName, where, orderBy);
        return Executor.Select(sql, convertor, where?.Parameters, con, tran);
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

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor,
        PageParam pageParam, string tableName, WhereConditions where, OrderByCondition orderBy, 
        out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        totalCount = Count(tableName, where, con, tran);
        totalPages = 0;
        if (totalCount > 0)
        {
            totalPages = totalCount / pageParam.PageSize;
            if (Math.Abs(totalCount % pageParam.PageSize) > 0)
            {
                totalPages++;
            }
        }
        
        var sql = Executor.SqlBuilder.Take(tableName, pageParam.Offset, pageParam.PageSize, where, orderBy);
        var items = Executor.Select(sql, convertor, where?.Parameters, con, tran).ToList();
        return items;
    }

    public bool Exist(string tableName, WhereConditions where,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        return Count(tableName, where, con, tran) > 0;
    }

    private string GetTableName<T>()
    {
        var entityMap = EntityMapCache.Get<T>();
        return entityMap.Table.Name;
    }

    private void DoBatchInsert(string tableName, IEnumerable<InputValues> inputs,
        IDbConnection con, IDbTransaction tran)
    {
        var inputList = inputs.ToList();

        var parameterCount = inputList.Count * inputList[0].Items.Count;
        if(parameterCount >= 2100) // sqlserver default parameter count limit.
        {
            var sql = Executor.SqlBuilder.Insert(tableName, inputList, false);
            Executor.Execute(sql, null, null, con, tran);
        }
        else
        {
            var sql = Executor.SqlBuilder.Insert(tableName, inputList);
            var paramList = new List<ColumnValueParameter>();
            foreach (var input in inputList)
            {
                paramList.AddRange(input.Items);
            }
            Executor.Execute(sql, paramList, null, con, tran);
        }
    }
}