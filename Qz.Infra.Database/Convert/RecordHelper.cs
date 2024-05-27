using Qz.Infra.Database.Cache;
using System;
using System.Data;
using Qz.Infra.Database.Entity;

namespace Qz.Infra.Database.Convert;

public static class RecordHelper
{
    public static T ToEntity<T>(this IDataRecord record)
        where T : new()
    {
        var type = typeof(T);
        if (EntityMap.IsEntity(type))
        {
            var entityMap = EntityMapCache.Get(type);
            return record.ToEntity<T>(entityMap);
        }
        else
        {
            return record.ToEntity<T>(type);
        }
    }

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
}