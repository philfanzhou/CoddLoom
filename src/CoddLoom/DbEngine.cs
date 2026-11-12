using CoddLoom.Condition;
using CoddLoom.Cache;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

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
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.DropTableSql(tableName);
        Executor.NonQuery(sql, null, con, tran);
    }

    public int Insert(string tableName, InputValues input,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.Insert(tableName, [input], out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    public int Insert(string tableName, IEnumerable<InputValues> inputs, int batchSize, 
        DbTransaction tran = null)
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
        DbConnection con = null, DbTransaction tran = null, bool force = false)
    {
        var sql = Executor.SqlBuilder.DeleteWithParameters(tableName, where, force, out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    public int Update(string tableName, InputValues input, WhereConditions where,
        DbConnection con = null, DbTransaction tran = null, bool force = false)
    {
        var sql = Executor.SqlBuilder.UpdateWithParameters(tableName, input, where, force, out var dbParams);
        return Executor.NonQuery(sql, dbParams, con, tran);
    }

    public int Count(string tableName, WhereConditions where, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.CountWithParameters(tableName, where, columns, out var dbParams);
        return Executor.Scalar(sql, System.Convert.ToInt32, dbParams, con, tran);
    }

    public List<T> Select<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        DbConnection con = null, DbTransaction tran = null)
    {
        var sql = Executor.SqlBuilder.SelectWithParameters(tableName, where, orderBy, null, columns, out var dbParams);
        return Executor.Reader(sql, convertor, dbParams, con, tran);
    }

    public List<T> PageSelect<T>(Func<IDataRecord, T> convertor, string tableName, WhereConditions where,
        OrderByCondition orderBy, ColumnParam columns,
        PageParam pageParam, out int totalPages, out int totalCount,
        DbConnection con = null, DbTransaction tran = null)
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

        var sql = Executor.SqlBuilder.SelectWithParameters(tableName, where, orderBy, pageParam, columns, out var dbParams);
        return Executor.Reader(sql, convertor, dbParams, con, tran);
    }

    private bool ExistTable(TableDefine tableDefine, DbConnection con)
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
