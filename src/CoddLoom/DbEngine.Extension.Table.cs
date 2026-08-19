using CoddLoom.Cache;
using CoddLoom.Table;
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

        TableColumnsCache.Initialize(tableList);
        Executor.Execute(con =>
        {
            foreach (var table in tableList)
            {
                if (!ExistTable(table, con))
                {
                    // Create the table when it does not exist.
                    var sql = Executor.SqlBuilder.GetCreateTableSql(table);
                    Executor.NonQuery(sql, null, con);
                }
                else
                {
                    // Add any missing columns when the table already exists.
                    CheckAndAddMissingColumns(table, con);
                }
            }
        });
    }

    private void CheckAndAddMissingColumns(TableDefine table, IDbConnection con)
    {
        var existingColumns = GetExistingColumns(table, con);
        
        foreach (var column in table.Columns)
        {
            if (!existingColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
            {
                var sql = Executor.SqlBuilder.GetAddColumnSql(table.Name, column);
                Executor.NonQuery(sql, null, con);
            }
        }
    }

    private List<string> GetExistingColumns(TableDefine table, IDbConnection con)
    {
        var sql = Executor.SqlBuilder.GetTableColumnsSql(table, out var dbParams);
        return Executor.Reader(sql, reader => reader.GetString(0), dbParams, con);
    }
}
