using System;
using System.Data;

namespace CoddLoom.Common;

internal static class DataRecordExt
{
    internal static string[] GetColumns(this IDataRecord record)
    {
        var columnNames = new string[record.FieldCount];
        for (var i = 0; i < record.FieldCount; i++)
        {
            columnNames[i] = record.GetName(i);
        }

        return columnNames;
    }

    internal static object GetValue(this IDataRecord record, string[] columns, string name)
    {
        var index = Array.FindIndex(columns,
            column => string.Equals(column, name, StringComparison.OrdinalIgnoreCase));
        if (index == -1)
        {
            return null;
        }

        var value = record.GetValue(index);
        return value;
    }
}
