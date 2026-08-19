using CoddLoom.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace CoddLoom.Convert;

partial class DbConverter
{
    public static DataTable CreateTable<T>(List<T> dataList)
    {
        var table = CreateTable<T>(out var properties);

        var tableColumns = new List<string>();
        foreach (DataColumn column in table.Columns)
        {
            tableColumns.Add(column.ColumnName);
        }

        foreach (var item in dataList)
        {
            var row = table.Rows.Add();
            foreach (var property in properties)
            {
                var column = property.Name;
                if (!tableColumns.Contains(column))
                {
                    continue;
                }

                var value = property.GetValue(item, null);
                row[column] = value ?? DBNull.Value;
            }
        }

        return table;
    }

    private static DataTable CreateTable<T>(out List<PropertyInfo> properties)
    {
        var objType = typeof(T);
        var table = new DataTable(objType.Name);
        properties = objType.GetAllProperties().ToList();
        foreach (var p in properties)
        {
            table.Columns.Add(new DataColumn(p.Name, p.PropertyType.GetRealDataType()));
        }
        return table;
    }
}