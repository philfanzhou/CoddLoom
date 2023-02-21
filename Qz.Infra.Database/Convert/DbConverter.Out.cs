using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Entity;
using System;
using System.Data;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

partial class DbConverter
{
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

    public static DateTime ToDateTime(IDataRecord record, string key)
    {
        if (record?[key] == null)
        {
            return DateTime.MinValue;
        }

        var strValue = record[key].ToString();
        return DateTime.TryParse(strValue, out var result) ? result : DateTime.MinValue;
    }

    internal static T ToEntity<T>(IDataRecord record)
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
        if (member is FieldInfo field)
        {
            var setValue = DbTypeConverter.ToEntityValue(field.FieldType, objValue);
            if (setValue != null)
            {
                field.SetValue(entity, setValue);
            }
        }
        else if (member is PropertyInfo property)
        {
            var setValue = DbTypeConverter.ToEntityValue(property.PropertyType, objValue);
            if (setValue != null)
            {
                property.SetValue(entity, setValue);
            }
        }
    }
}