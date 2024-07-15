using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

public static partial class DbConverter
{
    public static DataTable CreateTable<T>(List<T> dataList)
    {
        var table = CreateTable<T>(out var properties);

        var columns = new List<string>();
        foreach (DataColumn column in table.Columns)
        {
            columns.Add(column.ColumnName);
        }

        foreach (var data in dataList)
        {
            var row = table.Rows.Add();
            foreach (var p in properties)
            {
                if (!columns.Contains(p.Name))
                {
                    continue;
                }

                var value = p.GetValue(data, null);
                row[p.Name] = value ?? DBNull.Value;
            }
        }

        return table;
    }

    private static DataTable CreateTable<T>(out List<PropertyInfo> properties)
    {
        var objType = typeof(T);
        properties = objType.GetProperties().ToList();
        var table = new DataTable(objType.Name);
        foreach (var p in properties)
        {
            table.Columns.Add(new DataColumn(p.Name, GetRealDataType(p.PropertyType)));
        }
        return table;
    }
}
