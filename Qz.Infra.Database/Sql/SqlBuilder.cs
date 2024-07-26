using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Qz.Infra.Database.Sql;

public partial class SqlBuilder
{
    protected const string DbParameterPrefix = "@";

    public virtual string Insert(string tableName, IEnumerable<InputValues> inputs, out List<ValueParam> dbParams,  
        bool forceParameter = true)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if(inputs == null) throw new ArgumentNullException(nameof(inputs));
        var inputList = inputs.ToList();
        if(inputList.Count < 1 || inputList.Any(p => p == null || p.IsEmpty())) throw new ArgumentNullException(nameof(inputs));

        var columnSql = GetInsertColumns(inputList[0].Items);

        dbParams = new List<ValueParam>();
        var valueList = new List<string>();
        foreach(var input in inputList)
        {
            var values = GetInsertValues(input.Items, out var innerDbParams, forceParameter);
            valueList.Add(values);
            dbParams.AddRange(innerDbParams);
        }
        var valueSql = $"{string.Join(",", valueList.Select(p => p))}";

        return $"INSERT INTO {tableName} {columnSql} VALUES {valueSql}";
    }

    public virtual string Delete(string tableName, WhereConditions where)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (where == null || where.IsEmpty()) throw new ArgumentNullException(nameof(where));

        var sql = $"DELETE FROM {tableName}";
        return AppendWhere(sql, where);
    }

    public virtual string Update(string tableName, InputValues input, WhereConditions where)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.IsEmpty()) throw new ArgumentNullException(nameof(input));
        if (where == null || where.IsEmpty()) throw new ArgumentNullException(nameof(where));

        var valueBuilder = new StringBuilder();
        foreach (var item in input.Items)
        {
            if (valueBuilder.Length > 0)
            {
                valueBuilder.Append(", ");
            }
            valueBuilder.Append($"{item.Column} = ");
            valueBuilder.Append(GetParamName(item));
        }

        var sql = $"UPDATE {tableName} SET {valueBuilder}";
        return AppendWhere(sql, where);
    }

    public virtual string Count(string tableName,
        WhereConditions where = null, ColumnParam columns = null)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        var column = "*";
        if (where?.Parameters.FirstOrDefault() != null)
        {
            // 只查询where条件的第一个column，提高性能
            column = where.Parameters.First().Column;
        }
        else if (columns?.Select.FirstOrDefault() != null)
        {
            column = columns.Select.First().Column;
        }

        var sql = $"SELECT COUNT({column}) FROM {tableName}";
        sql = AppendWhere(sql, where);
        return AppendGroupBy(sql, columns);
    }

    public virtual string First(string tableName,
        WhereConditions where = null, OrderByCondition orderBy = null, ColumnParam columns = null)
    {
        return Select(tableName, where, orderBy, new PageParam { PageNumber = 1, PageSize = 1 }, columns);
    }

    public virtual string Select(string tableName, 
        WhereConditions where = null, OrderByCondition orderBy = null, PageParam pageParam = null, ColumnParam columns = null)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        var sql = $"SELECT {GetSelectColumnSql(columns)} FROM {tableName}";
        sql = AppendWhere(sql, where);
        sql = AppendGroupBy(sql, columns);
        sql = AppendOrderBy(sql, orderBy);
        return AppendLimit(sql, pageParam);
    }

    #region Protected virtual

    protected virtual string AppendWhere(string sql,
        WhereConditions where = null)
    {
        if (where == null || where.IsEmpty()) return sql; // 必须检查是不是Empty，因为有查询条件只有IsNull的查询条件
        return $"{sql} WHERE {where.GetWhereString(this)}";
    }

    protected virtual string AppendGroupBy(string sql, 
        ColumnParam columns = null)
    {
        if (columns == null || columns.GroupBy.Count < 1) return sql;
        return $"{sql} GROUP BY {string.Join(",", columns.GroupBy.Select(p => p.Column))}";
    }

    protected virtual string AppendOrderBy(string sql,
        OrderByCondition orderBy = null)
    {
        if (orderBy == null || string.IsNullOrEmpty(orderBy.Column)) return sql;
        var sort = orderBy.Descending ? "DESC" : "ASC";
        return $"{sql} ORDER BY {orderBy.Column} {sort}";
    }

    protected virtual string AppendLimit(string sql,
        PageParam pageParam = null)
    {
        if (pageParam == null) return sql;
        return $"{sql} LIMIT {pageParam.Offset},{pageParam.PageSize}";
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
            _ => throw new NotSupportedException(dbType.ToString())
        };
        return $"CAST({column} AS {typeStr})";
    }

    protected internal virtual string GetParamName(ValueParam param)
    {
        return $"{DbParameterPrefix}{param.ParamName}";
    }

    protected virtual string GetInsertValue(ValueParam param)
    {
        if(param.Value == DBNull.Value)
        {
            return "NULL";
        }
        else if(param.Value is string)
        {
            return $"'{param.Value.ToString().Replace("'", "''")}'";
        }
        else if(param.Value is DateTime)
        {
            return $"'{param.Value:yyyy-MM-dd HH:mm:ss}'";
        }
        else
        {
            return param.Value.ToString();
        }
    }

    protected virtual string GetInsertColumns(IEnumerable<ValueParam> parameters)
    {
        var columns = $"({string.Join(",", parameters.Select(p => p.Column))})";
        return columns;
    }

    protected virtual string GetInsertValues(IEnumerable<ValueParam> parameters,
        out List<ValueParam> dbParams, bool useParameter = true)
    {
        dbParams = new List<ValueParam>();
        var valuesStrBuilder = new StringBuilder();
        foreach(var item in parameters)
        {
            if(valuesStrBuilder.Length > 0)
            {
                valuesStrBuilder.Append(",");
            }
            if (useParameter || item.ForceParameter)
            {
                valuesStrBuilder.Append(GetParamName(item));
                dbParams.Add(item);
            }
            else
            {
                valuesStrBuilder.Append(GetInsertValue(item));
            }
        }

        return $"({valuesStrBuilder})";
    }

    protected virtual string GetSelectColumn(string column, string dbFunc, DbType? cast, string alias)
    {
        var columnSql = column;
        if(cast != null)
        {
            columnSql = GetCastColumn(column, cast.Value);
        }
        if(string.IsNullOrEmpty(dbFunc) == false)
        {
            columnSql = $"{dbFunc}({columnSql})";
        }
        if(string.IsNullOrEmpty(alias) == false)
        {
            columnSql = $"{columnSql} AS {alias}";
        }
        return columnSql;
    }

    #endregion
}