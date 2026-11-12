using CoddLoom.Cache;
using CoddLoom.Params;
using CoddLoom.Table;
using CoddLoom.Table.Base;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// Asynchronously creates missing tables and adds missing columns. This is the
    /// asynchronous counterpart of <see cref="InitializeTable(IEnumerable{TableDefine})"/>;
    /// the generated schema SQL, the plan order, the failure semantics for adding a
    /// required column to a non-empty table, and the cache update are identical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Like the synchronous version, this method is not safe for concurrent execution
    /// by multiple instances: callers must serialize table initialization themselves.
    /// </para>
    /// <para>
    /// The call delegates to the executor's asynchronous methods and awaits the
    /// driver's asynchronous APIs directly; it never wraps the synchronous path in
    /// <c>Task.Run</c>. The cancellation token is observed before the connection opens
    /// and is then forwarded to the driver's asynchronous execution.
    /// </para>
    /// </remarks>
    public async Task InitializeTableAsync(IEnumerable<TableDefine> tables,
        CancellationToken cancellationToken = default)
    {
        if (tables == null) return;
        var tableList = tables.ToList();
        if(tableList.Count < 1) return;

        await Executor.ExecuteAsync(async con =>
        {
            var plans = await BuildInitializationPlansAsync(tableList, con, cancellationToken)
                .ConfigureAwait(false);
            foreach (var plan in plans)
            {
                if (plan.CreateTable)
                {
                    // Create the table when it does not exist.
                    var sql = Executor.SqlBuilder.GetCreateTableSql(plan.Table);
                    await Executor.NonQueryAsync(sql, null, con,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    foreach (var column in plan.MissingColumns)
                    {
                        var sql = Executor.SqlBuilder.GetAddColumnSql(plan.Table.Name, column);
                        await Executor.NonQueryAsync(sql, null, con,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        _tableColumnsCache.Initialize(tableList);
    }

    private List<TableInitializationPlan> BuildInitializationPlans(
        IEnumerable<TableDefine> tables, DbConnection con)
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

    private async Task<List<TableInitializationPlan>> BuildInitializationPlansAsync(
        IEnumerable<TableDefine> tables, DbConnection con, CancellationToken cancellationToken)
    {
        var plans = new List<TableInitializationPlan>();
        foreach (var table in tables)
        {
            if (!await ExistTableAsync(table, con, cancellationToken).ConfigureAwait(false))
            {
                plans.Add(new TableInitializationPlan(table, true, []));
                continue;
            }

            var existingColumns = await GetExistingColumnsAsync(table, con, cancellationToken)
                .ConfigureAwait(false);
            var missingColumns = table.Columns
                .Where(column => !existingColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
            var requiredColumns = missingColumns.Where(column => !column.AllowEmpty).ToList();

            if (requiredColumns.Count > 0
                && await TableHasRowsAsync(table, con, cancellationToken).ConfigureAwait(false))
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

    private List<string> GetExistingColumns(TableDefine table, DbConnection con)
    {
        var sql = Executor.SqlBuilder.GetTableColumnsSql(table, out var dbParams);
        return Executor.Reader(sql, reader => reader.GetString(0), dbParams, con);
    }

    private Task<List<string>> GetExistingColumnsAsync(TableDefine table, DbConnection con,
        CancellationToken cancellationToken)
    {
        var sql = Executor.SqlBuilder.GetTableColumnsSql(table, out var dbParams);
        return Executor.ReaderAsync(sql, reader => reader.GetString(0), dbParams, con,
            cancellationToken: cancellationToken);
    }

    private bool TableHasRows(TableDefine table, DbConnection con)
    {
        var page = new PageParam { PageNumber = 1, PageSize = 1 };
        var columns = new ColumnParam().AddSelect("1");
        var sql = Executor.SqlBuilder.Select(table.Name, pageParam: page, columns: columns);
        return Executor.Scalar(sql, _ => true, null, con);
    }

    private Task<bool> TableHasRowsAsync(TableDefine table, DbConnection con,
        CancellationToken cancellationToken)
    {
        var page = new PageParam { PageNumber = 1, PageSize = 1 };
        var columns = new ColumnParam().AddSelect("1");
        var sql = Executor.SqlBuilder.Select(table.Name, pageParam: page, columns: columns);
        return Executor.ScalarAsync(sql, _ => true, null, con,
            cancellationToken: cancellationToken);
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
