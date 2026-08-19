using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Table;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;

namespace Qz.Infra.Database;

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
                    // 表不存在，创建表
                    var sql = Executor.SqlBuilder.GetCreateTableSql(table);
                    Executor.NonQuery(sql, null, con);
                }
                else
                {
                    // 表存在，检查并添加缺失的列
                    CheckAndAddMissingColumns(table, con);
                }
            }
        });
    }

    private void CheckAndAddMissingColumns(TableDefine table, IDbConnection con)
    {
        var existingColumns = GetExistingColumns(table.Name, con);
        
        foreach (var column in table.Columns)
        {
            if (!existingColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
            {
                var sql = Executor.SqlBuilder.GetAddColumnSql(table.Name, column);
                Executor.NonQuery(sql, null, con);
            }
        }
    }

    private List<string> GetExistingColumns(string tableName, IDbConnection con)
    {
        var sql = Executor.SqlBuilder.GetTableColumnsSql(tableName);
        return Executor.Reader(sql, reader => reader.GetString(0), null, con);
    }
}