using System;
using System.Data;

namespace Qz.Infra.Database.Common;

internal static class DataRecordExt
{
    internal static string[] GetColumns(IDataRecord record)
    {
        var columnNames = new string[record.FieldCount];
        for (var i = 0; i < record.FieldCount; i++)
        {
            columnNames[i] = record.GetName(i).ToLower();
        }

        return columnNames;
    }

    internal static object GetValue(IDataRecord record, string[] columns, string name)
    {
        var index = Array.IndexOf(columns, name.ToLower());
        if (index == -1)
        {
            return null;
        }

        var value = record.GetValue(index);
        return value;
    }
}