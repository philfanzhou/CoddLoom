using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Entity;
using System;
using System.Data;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

public static partial class DbConverter
{
    // TODO: remove
    public static bool ToBoolean(IDataRecord record, string key)
    {
        if (record?[key] == null)
        {
            return false;
        }

        var strValue = record[key].ToString();
        if (string.IsNullOrEmpty(strValue))
        {
            return false;
        }

        if (int.TryParse(strValue, out var intValue))
        {
            return intValue != 0;
        }
        else
        {
            return bool.Parse(strValue);
        }
    }

    // TODO: remove
    public static DateTime ToDateTime(IDataRecord record, string key)
    {
        if (record?[key] == null)
        {
            return DateTime.MinValue;
        }

        var strValue = record[key].ToString();
        return DateTime.TryParse(strValue, out var result) ? result : DateTime.MinValue;
    }

    public static T ToEntity<T>(IDataRecord record)
        where T : new()
    {
        var entityMap = EntityMapCache.Get<T>();
        return ToEntity<T>(record, entityMap);
    }

    internal static T ToEntity<T>(IDataRecord record, EntityMap entityMap)
        where T : new()
    {
        var entity = new T();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            SetEntityValue(entity, memberInfo, attribute, record);
        }

        return entity;
    }

    private static void SetEntityValue<T>(T entity, MemberInfo member, MapColumnAttribute attribute, IDataRecord record)
    {
        var objValue = record[attribute.Name];
        if (objValue is null or DBNull)
        {
            return;
        }

        if (member is FieldInfo field)
        {
            var setValue = ToEntityValue(field.FieldType, objValue);
            if (setValue != null)
            {
                field.SetValue(entity, setValue);
            }
        }
        else if (member is PropertyInfo property)
        {
            var setValue = ToEntityValue(property.PropertyType, objValue);
            if (setValue != null)
            {
                property.SetValue(entity, setValue);
            }
        }
    }

    private static object ToEntityValue(Type type, object value)
    {
        var typeName = type.FullName;
        return typeName switch
        {
            "System.String" => value.ToString().Trim(),
            "System.Int32" => int.TryParse(value.ToString(), out var ret) ? ret : null,
            _ => value
            //"System.Int16" => short.TryParse(strValue, out var ret) ? ret : null,
            //"System.Decimal" => decimal.TryParse(strValue, out var ret) ? ret : null,
            //"System.DateTime" => DateTime.TryParse(strValue, out var ret) ? ret : null,
            //"System.Boolean" => bool.TryParse(strValue, out var ret) ? ret : null,
            //"System.Double" => double.TryParse(strValue, out var ret) ? ret : null,
            //_ => throw new NotSupportedException($"{typeName} not support.")
        };
    }
}