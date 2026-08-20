using System.Collections.Concurrent;
using CoddLoom.Table;
using System.Collections.Generic;
using System.Linq;

namespace CoddLoom.Cache;

internal sealed class TableColumnsCache
{
    private readonly ConcurrentDictionary<string, TableColumns> _columns = new();

    internal void Initialize(IEnumerable<TableDefine> tables)
    {
        foreach (var table in tables)
        {
            var insertColumns = new List<string>();
            if (table.PrimaryKey != null
                && table.PrimaryKey is not DbPrimaryKeyIdentityAttribute)
            {
                insertColumns.Add(table.PrimaryKey.Name);
            }

            insertColumns.AddRange(table.Columns.Select(column => column.Name));
            _columns[table.Name] = new TableColumns(
                insertColumns.ToArray(),
                table.Columns.Select(column => column.Name).ToArray());
        }
    }

    internal IReadOnlyCollection<string> GetInsertColumns(string tableName)
    {
        if (_columns.TryGetValue(tableName, out var columns))
        {
            return columns.Insert;
        }

        return null;
    }

    internal IReadOnlyCollection<string> GetUpdateColumns(string tableName)
    {
        if (_columns.TryGetValue(tableName, out var columns))
        {
            return columns.Update;
        }

        return null;
    }

    private sealed class TableColumns(string[] insert, string[] update)
    {
        internal string[] Insert { get; } = insert;
        internal string[] Update { get; } = update;
    }
}
