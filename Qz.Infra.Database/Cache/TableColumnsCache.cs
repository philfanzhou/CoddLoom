using Qz.Infra.Database.Table;
using System.Collections.Generic;

namespace Qz.Infra.Database.Cache;

internal static class TableColumnsCache
{
    private static readonly Dictionary<string, List<string>> InsertColumnCache = new();
    private static readonly Dictionary<string, List<string>> UpdateColumnCache = new();

    internal static void Initialize(IEnumerable<TableDefine> tables)
    {
        foreach (var table in tables)
        {
            InsertColumnCache.Add(table.Name, new List<string>());
            UpdateColumnCache.Add(table.Name, new List<string>());

            if (table.PrimaryKey != null 
                && table.PrimaryKey.IsIdentity == false)
            {
                InsertColumnCache[table.Name].Add(table.PrimaryKey.Name);
            }

            foreach (var column in table.Columns)
            {
                InsertColumnCache[table.Name].Add(column.Name);
                UpdateColumnCache[table.Name].Add(column.Name);
            }
        }
    }

    internal static List<string> GetInsertColumns(string tableName)
    {
        if (InsertColumnCache.ContainsKey(tableName))
        {
            return InsertColumnCache[tableName];
        }

        return null;
    }

    internal static List<string> GetUpdateColumns(string tableName)
    {
        if (UpdateColumnCache.ContainsKey(tableName))
        {
            return UpdateColumnCache[tableName];
        }

        return null;
    }
}