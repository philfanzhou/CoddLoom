using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoddLoom.Sql;

partial class SqlBuilder
{
    protected virtual string GetInsertColumns(IEnumerable<ValueParam> parameters)
    {
        var columns = $"({string.Join(",", parameters.Select(p => p.Column))})";
        return columns;
    }

    protected virtual string GetSelectColumnSql(ColumnParam columns = null)
    {
        if (columns == null || columns.Select.Count < 1) return "*";
        return string.Join(",", columns.Select.Select(p => GetSelectColumn(p.Column, p.DbFunc, p.Cast, p.Alias)));
    }

    protected internal virtual string GetCastColumn(string column, DbType dbType)
    {
        var typeStr = dbType switch
        {
            DbType.DateTime => "DateTime",
            DbType.Int64 => GetColumnType(DbType.Int64),
            _ => throw new NotSupportedException(dbType.ToString())
        };
        return $"CAST({column} AS {typeStr})";
    }

    protected virtual string GetSelectColumn(string column, string dbFunc, DbType? cast, string alias)
    {
        var columnSql = column;
        if (cast != null)
        {
            columnSql = GetCastColumn(column, cast.Value);
        }
        if (string.IsNullOrEmpty(dbFunc) == false)
        {
            columnSql = $"{dbFunc}({columnSql})";
        }
        if (string.IsNullOrEmpty(alias) == false)
        {
            columnSql = $"{columnSql} AS {alias}";
        }
        return columnSql;
    }
}
