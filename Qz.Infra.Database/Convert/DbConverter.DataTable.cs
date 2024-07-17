using Qz.Infra.Database.Common;
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

        foreach (var item in dataList)
        {
            var row = table.Rows.Add();
            foreach (var property in properties)
            {
                if (!columns.Contains(property.Name))
                {
                    continue;
                }

                var value = property.GetValue(item, null);
                row[property.Name] = value ?? DBNull.Value;
            }
        }

        return table;
    }

    private static DataTable CreateTable<T>(out List<PropertyInfo> properties)
    {
        var objType = typeof(T);
        properties = objType.GetAllProperties().ToList();
        var table = new DataTable(objType.Name);
        foreach (var p in properties)
        {
            table.Columns.Add(new DataColumn(p.Name, p.PropertyType.GetRealDataType()));
        }
        return table;
    }
}