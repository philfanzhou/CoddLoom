using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Entity;
using System;
using System.Data;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

public static partial class DbConverter
{
    public static bool GetBoolean(this IDataRecord record, string key)
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

        return bool.Parse(strValue);
    }

    public static DateTime GetDateTime(this IDataRecord record, string key)
    {
        if (record?[key] == null)
        {
            return DateTime.MinValue;
        }

        var strValue = record[key].ToString();
        return DateTime.TryParse(strValue, out var result) ? result : DateTime.MinValue;
    }

    public static T ToEntity<T>(this IDataRecord record)
        where T : new()
    {
        var type = typeof(T);
        if (EntityMap.HasMap(type))
        {
            var entityMap = EntityMapCache.Get(type);
            return record.ToEntity<T>(entityMap);
        }
        else
        {
            return record.ToEntity<T>(type);
        }
    }

    private static Type GetDataType(Type type)
    {
        if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Nullable.GetUnderlyingType(type);
        }
        return type;
    }

    private static T ToEntity<T>(this IDataRecord record, EntityMap entityMap)
        where T : new()
    {
        var entity = new T();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            var value = record[attribute.Name];
            if (value is null or DBNull)
            {
                continue;
            }

            if (memberInfo is FieldInfo field)
            {
                var setValue = System.Convert.ChangeType(value, GetDataType(field.FieldType));
                if (setValue != null)
                {
                    field.SetValue(entity, setValue);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                var setValue = System.Convert.ChangeType(value, GetDataType(property.PropertyType));
                if (setValue != null)
                {
                    property.SetValue(entity, setValue);
                }
            }
        }

        return entity;
    }

    private static T ToEntity<T>(this IDataRecord record, Type type)
        where T : new()
    {
        var entity = new T(); 
        for (var i = 0; i < record.FieldCount; i++)
        {
            var value = record.GetValue(i);
            if (value is null or DBNull)
            {
                continue;
            }

            var fieldName = record.GetName(i);
            var property = type.GetProperty(fieldName);
            if (property != null && property.CanWrite)
            {
                var setValue = System.Convert.ChangeType(value, GetDataType(property.PropertyType));
                property.SetValue(entity, setValue);
            }
            else
            {
                Console.WriteLine($"Warning: No matching property or not writable - {fieldName}");
            }
        }

        return entity;
    }
}