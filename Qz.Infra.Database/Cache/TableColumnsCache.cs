using Qz.Infra.Database.Table;
using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Cache;

internal static class TableColumnsCache
{
    private static readonly Dictionary<string, List<string>> InsertColumnCache = new();
    private static readonly Dictionary<string, List<string>> UpdateColumnCache = new();

    internal static void Initialize(IEnumerable<TableDefine> tables)
    {
        var tableList = tables.ToList();
        foreach (var table in tableList)
        {
            if (InsertColumnCache.ContainsKey(table.Name))
            {
                continue;
            }

            InsertColumnCache.Add(table.Name, new List<string>());
            if (table.PrimaryKey != null
                && table.PrimaryKey is not DbPrimaryKeyIdentityAttribute)
            {
                InsertColumnCache[table.Name].Add(table.PrimaryKey.Name);
            }

            foreach (var column in table.Columns)
            {
                InsertColumnCache[table.Name].Add(column.Name);
            }
        }

        foreach(var table in tableList)
        {
            if(UpdateColumnCache.ContainsKey(table.Name))
            {
                continue;
            }

            UpdateColumnCache.Add(table.Name, new List<string>());
            foreach (var column in table.Columns)
            {
                UpdateColumnCache[table.Name].Add(column.Name);
            }
        }
    }

    internal static List<string> GetInsertColumns(string tableName)
    {
        if (InsertColumnCache.TryGetValue(tableName, out var columns))
        {
            return columns;
        }

        return null;
    }

    internal static List<string> GetUpdateColumns(string tableName)
    {
        if (UpdateColumnCache.TryGetValue(tableName, out var columns))
        {
            return columns;
        }

        return null;
    }
}