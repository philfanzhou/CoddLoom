using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CoddLoom.Sql;

public partial class SqlBuilder
{
    protected const string DbParameterPrefix = "@";

    public virtual string Insert(string tableName, IEnumerable<InputValues> inputs, out List<ValueParam> dbParams,
        bool useParameter = true)
    {
        var inputRows = PrepareInsertRows(tableName, inputs);
        var columnSql = GetInsertColumns(inputRows[0]);

        dbParams = new List<ValueParam>();
        var valueList = new List<string>();
        foreach (var inputParameters in inputRows)
        {
            var values = GetInsertValues(inputParameters, out var innerDbParams, useParameter);
            valueList.Add(values);
            dbParams.AddRange(innerDbParams);
        }
        var valueSql = $"{string.Join(",", valueList.Select(p => p))}";

        return $"INSERT INTO {tableName} {columnSql} VALUES {valueSql}";
    }

    protected IReadOnlyList<IReadOnlyList<ValueParam>> PrepareInsertRows(
        string tableName, IEnumerable<InputValues> inputs)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var inputList = inputs.ToList();
        if (inputList.Count < 1 || inputList.Any(input => input == null || input.IsEmpty()))
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var expectedColumns = inputList[0].Items.Select(item => item.Column).ToArray();
        var rowsByColumn = inputList.Select(input =>
            input.Items.ToDictionary(item => item.Column, StringComparer.Ordinal)).ToList();
        if (rowsByColumn.Any(row => row.Count != expectedColumns.Length
                || expectedColumns.Any(column => !row.ContainsKey(column))))
        {
            throw new ArgumentException("Every input row must contain the same columns.", nameof(inputs));
        }

        var rows = new List<IReadOnlyList<ValueParam>>();
        for (var inputIndex = 0; inputIndex < inputList.Count; inputIndex++)
        {
            // Directly constructed InputValues instances all start with V0_. Normalize
            // names so every row in a multi-row command has distinct parameters.
            rows.Add(expectedColumns.Select(column => rowsByColumn[inputIndex][column])
                .Select(item => new ValueParam(
                    item.Column, item.Value, $"V{inputIndex}_{item.Column}", item.ForceParameter))
                .ToList().AsReadOnly());
        }

        return rows.AsReadOnly();
    }

    public virtual string Delete(string tableName, WhereConditions where, bool force = false)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if ((where == null || where.IsEmpty()) && !force) throw new ArgumentNullException(nameof(where));

        var sql = $"DELETE FROM {tableName}";
        return AppendWhere(sql, where);
    }

    public virtual string Update(string tableName, InputValues input, WhereConditions where, bool force = false)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.IsEmpty()) throw new ArgumentNullException(nameof(input));
        if ((where == null || where.IsEmpty()) && !force) throw new ArgumentNullException(nameof(where));

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

        if (columns?.GroupBy.Count > 0)
        {
            var groupedSql = AppendWhere($"SELECT 1 AS CoddLoomGroup FROM {tableName}", where);
            groupedSql = AppendGroupBy(groupedSql, columns);
            return $"SELECT COUNT(*) FROM ({groupedSql}) CoddLoomCount";
        }

        return AppendWhere($"SELECT COUNT(*) FROM {tableName}", where);
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

    protected virtual string AppendGroupBy(string sql, 
        ColumnParam columns = null)
    {
        if (columns == null || columns.GroupBy.Count < 1) return sql;
        return $"{sql} GROUP BY {string.Join(",", columns.GroupBy.Select(p => p.Column))}";
    }

    protected virtual string AppendOrderBy(string sql,
        OrderByCondition orderBy = null)
    {
        if (orderBy == null || orderBy.IsEmpty()) return sql;
        var condition = string.Join(",", orderBy.Items.Select(p =>
        $"{p.Column} {(p.Descending ? "DESC" : "ASC")}"));
        return $"{sql} ORDER BY {condition}";
    }

    protected virtual string AppendLimit(string sql,
        PageParam pageParam = null)
    {
        if (pageParam == null) return sql;
        return $"{sql} LIMIT {pageParam.Offset},{pageParam.PageSize}";
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
                valuesStrBuilder.Append(GetInsertValueString(item));
            }
        }

        return $"({valuesStrBuilder})";
    }

    protected virtual string GetInsertValueString(ValueParam param)
    {
        if (param.Value == DBNull.Value)
        {
            return "NULL";
        }
        else if (param.Value is string)
        {
            return $"'{param.Value.ToString().Replace("'", "''")}'";
        }
        else if (param.Value is DateTime)
        {
            return $"'{param.Value:yyyy-MM-dd HH:mm:ss}'";
        }
        else if (param.Value is bool v)
        {
            return v ? "1" : "0";
        }
        else
        {
            return System.Convert.ToString(param.Value, CultureInfo.InvariantCulture);
        }
    }

    protected internal virtual string GetParamName(ValueParam param)
    {
        return $"{DbParameterPrefix}{param.ParamName}";
    }

    #endregion
}
