using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Table;
using System.Collections.Generic;
using System.Linq;

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
            foreach (var table in tableList.Where(table => !ExistTable(table, con)))
            {
                var sql = Executor.SqlBuilder.GetCreateTableSql(table);
                Executor.NonQuery(sql, null, con);
            }
        });
    }
}