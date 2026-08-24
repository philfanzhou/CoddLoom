using CoddLoom.Condition;
using CoddLoom.Cache;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoddLoom;

public partial class DbEngine
{
    private readonly TableColumnsCache _tableColumnsCache = new();

    public DbEngine(DbExecutor executor, IEnumerable<TableDefine> tables)
    {
        Executor = executor;
        InitializeTable(tables);
    }

    public DbEngine(DbExecutor executor) : this(executor, null)
    { }

    public DbExecutor Executor { get; }

    public void Drop(string tableName,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.DropTableSql(tableName);
        Executor.NonQuery(sql, null, con, tran);
    }

    public int Insert(string tableName, InputValues input,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Insert(tableName, [input], out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    public int Insert(string tableName, IEnumerable<InputValues> inputs, int batchSize, 
        IDbTransaction tran = null)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize),
                "Batch size must be greater than 0.");
        }

        if (tran == null)
        {
            return Executor.Transaction(transaction =>
                InsertWithTransaction(tableName, inputs, batchSize, transaction));
        }

        return InsertWithTransaction(tableName, inputs, batchSize, tran);
    }

    public int Delete(string tableName, WhereConditions where, 
        IDbConnection con = null, IDbTransaction tran = null, bool force = false)
    {
        var sql = Executor.SqlBuilder.Delete(tableName, where, force);
        return Executor.NonQuery(sql, where?.Parameters, con, tran);
    }

    public int Update(string tableName, InputValues input, WhereConditions where, 
        IDbConnection con = null, IDbTransaction tran = null, bool force = false)
    {
        var sql = Executor.SqlBuilder.Update(tableName, input, where, force);
        var dbParams = new List<ValueParam>();
        dbParams.AddRange(input.Items);
        if (where != null)
        {
            dbParams.AddRange(where.Parameters);
        }
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    public int Count(string tableName, WhereConditions where, ColumnParam columns,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Count(tableName, where, columns);
        return Executor.Scalar(sql, System.Convert.ToInt32, where?.Parameters, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Select(tableName, where, orderBy, null, columns);
        return Executor.Reader(sql, convertor, where?.Parameters, con, tran);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        PageParam pageParam, out int totalPages, out int totalCount,
        IDbConnection con = null, IDbTransaction tran = null)
    {
        totalPages = 0;
        totalCount = 0;
        if (pageParam != null)
        {
            totalCount = Count(tableName, where, columns, con, tran);
            if (totalCount > 0)
            {
                totalPages = totalCount / pageParam.PageSize;
                if (Math.Abs(totalCount % pageParam.PageSize) > 0)
                {
                    totalPages++;
                }
            }
        }

        var sql = Executor.SqlBuilder.Select(tableName, where, orderBy, pageParam, columns);
        return Executor.Reader(sql, convertor, where?.Parameters, con, tran);
    }

    private bool ExistTable(TableDefine tableDefine, IDbConnection con)
    {
        if (Executor.TryGetLegacyExistTableParam(tableDefine, out var checkTable, out var where))
        {
            if (string.IsNullOrEmpty(checkTable) || where == null)
            {
                return false;
            }

            return Count(checkTable, where, con) > 0;
        }

        var sql = Executor.SqlBuilder.GetTableExistsSql(tableDefine, out var dbParams);
        return Executor.Scalar(sql, System.Convert.ToInt32, dbParams, con) > 0;
    }

}
