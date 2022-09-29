using QuantumZhou.Infrastructure.Data.Database.Table;
using System.Collections.Generic;

namespace QuantumZhou.Infrastructure.Data.Database.Cache
{
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

                if (table.PrimaryKey != null)
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

        internal static List<string> GetTableInsertColumns(string tableName)
        {
            if (InsertColumnCache.ContainsKey(tableName))
            {
                return InsertColumnCache[tableName];
            }

            return null;
        }

        internal static List<string> GetTableUpdateColumns(string tableName)
        {
            if (UpdateColumnCache.ContainsKey(tableName))
            {
                return UpdateColumnCache[tableName];
            }

            return null;
        }
    }
}
