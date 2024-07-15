using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Common;
using Qz.Infra.Database.Entity;
using System;
using System.Data;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

public static partial class DbConverter
{
    public static T ToEntity<T>(this IDataRecord record)
        where T : new()
    {
        var type = typeof(T);
        if (EntityMap.HasMap(type))
        {
            var entityMap = EntityMapCache.Get(type);
            return record.ToEntityFromMap<T>(entityMap);
        }
        else
        {
            return record.ToEntityFromType<T>(type);
        }
    }

    private static T ToEntityFromMap<T>(this IDataRecord record, EntityMap entityMap)
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
                var setValue = System.Convert.ChangeType(value, GetRealDataType(field.FieldType));
                if (setValue != null)
                {
                    field.SetValue(entity, setValue);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                var setValue = System.Convert.ChangeType(value, GetRealDataType(property.PropertyType));
                if (setValue != null)
                {
                    property.SetValue(entity, setValue);
                }
            }
        }

        return entity;
    }

    private static T ToEntityFromType<T>(this IDataRecord record, Type type)
        where T : new()
    {
        var columnNames = new string[record.FieldCount];
        for(var i = 0; i < record.FieldCount; i++)
        {
            columnNames[i] = record.GetName(i);
        }

        var properties = type.GetAllProperties();
        var entity = new T();
        foreach(var property in  properties)
        {
            var index = Array.IndexOf(columnNames, property.Name);
            if (index == -1)
            {
                continue;
            }

            var value = record.GetValue(index);
            if (value is null or DBNull)
            {
                continue;
            }

            var setValue = System.Convert.ChangeType(value, GetRealDataType(property.PropertyType));
            property.SetValue(entity, setValue);
        }

        return entity;
    }

    private static Type GetRealDataType(Type type)
    {
        if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Nullable.GetUnderlyingType(type);
        }
        return type;
    }
}