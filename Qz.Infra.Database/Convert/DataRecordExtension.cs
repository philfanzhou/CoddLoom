using Qz.Infra.Database.Cache;
using System;
using System.Data;

namespace Qz.Infra.Database.Convert;

public static class DataRecordExtension
{
    public static T ToEntity<T>(this IDataRecord record)
        where T : new()
    {
        var entityMap = EntityMapCache.Get<T>();
        return record.ToEntity<T>(entityMap);
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