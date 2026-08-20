using CoddLoom.Cache;
using CoddLoom.Params;
using CoddLoom.Table;
using CoddLoom.Table.Base;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;

namespace CoddLoom;

partial class DbEngine
{
    public void InitializeTable(IEnumerable<TableDefine> tables)
    {
        if (tables == null) return;
        var tableList = tables.ToList();
        if(tableList.Count < 1) return;

        Executor.Execute(con =>
        {
            var plans = BuildInitializationPlans(tableList, con);
            foreach (var plan in plans)
            {
                if (plan.CreateTable)
                {
                    // Create the table when it does not exist.
                    var sql = Executor.SqlBuilder.GetCreateTableSql(plan.Table);
                    Executor.NonQuery(sql, null, con);
                }
                else
                {
                    foreach (var column in plan.MissingColumns)
                    {
                        var sql = Executor.SqlBuilder.GetAddColumnSql(plan.Table.Name, column);
                        Executor.NonQuery(sql, null, con);
                    }
                }
            }
        });
        _tableColumnsCache.Initialize(tableList);
    }

    private List<TableInitializationPlan> BuildInitializationPlans(
        IEnumerable<TableDefine> tables, IDbConnection con)
    {
        var plans = new List<TableInitializationPlan>();
        foreach (var table in tables)
        {
            if (!ExistTable(table, con))
            {
                plans.Add(new TableInitializationPlan(table, true, []));
                continue;
            }

            var existingColumns = GetExistingColumns(table, con);
            var missingColumns = table.Columns
                .Where(column => !existingColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
            var requiredColumns = missingColumns.Where(column => !column.AllowEmpty).ToList();

            if (requiredColumns.Count > 0 && TableHasRows(table, con))
            {
                var columnNames = string.Join("', '", requiredColumns.Select(column => column.Name));
                throw new InvalidOperationException(
                    $"Cannot add required column(s) '{columnNames}' to non-empty table '{table.Name}'. "
                    + "Backfill existing rows before initializing the new schema.");
            }

            plans.Add(new TableInitializationPlan(table, false, missingColumns));
        }

        return plans;
    }

    private List<string> GetExistingColumns(TableDefine table, IDbConnection con)
    {
        var sql = Executor.SqlBuilder.GetTableColumnsSql(table, out var dbParams);
        return Executor.Reader(sql, reader => reader.GetString(0), dbParams, con);
    }

    private bool TableHasRows(TableDefine table, IDbConnection con)
    {
        var page = new PageParam { PageNumber = 1, PageSize = 1 };
        var columns = new ColumnParam().AddSelect("1");
        var sql = Executor.SqlBuilder.Select(table.Name, pageParam: page, columns: columns);
        return Executor.Scalar(sql, _ => true, null, con);
    }

    private sealed class TableInitializationPlan(
        TableDefine table,
        bool createTable,
        IReadOnlyList<DbColumnBaseAttribute> missingColumns)
    {
        internal TableDefine Table { get; } = table;

        internal bool CreateTable { get; } = createTable;

        internal IReadOnlyList<DbColumnBaseAttribute> MissingColumns { get; } = missingColumns;
    }
}
