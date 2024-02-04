using Qz.Infra.Database.Entity;
using System;
using System.Data;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

internal static partial class DbConverter
{
    internal static T ToEntity<T>(this IDataRecord record, EntityMap entityMap)
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
        };
    }
}