using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Common;
using Qz.Infra.Database.Entity;
using System;
using System.Data;

namespace Qz.Infra.Database.Convert;

public static partial class DbConverter
{
    public static T ToEntity<T>(this IDataRecord record)
        where T : new()
    {
        var type = typeof(T);
        if (EntityMap.HasMap(type))
        {
            return record.ToEntityFromMap<T>(type);
        }
        else
        {
            return record.ToEntityFromType<T>(type);
        }
    }

    private static T ToEntityFromMap<T>(this IDataRecord record, Type type)
        where T : new()
    {
        var entityMap = EntityMapCache.Get(type);
        var entity = new T();
        var columns = record.GetColumns();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            var value = record.GetValue(columns, attribute.Name);
            memberInfo.SetValue(entity, value);
        }

        return entity;
    }

    private static T ToEntityFromType<T>(this IDataRecord record, Type type)
        where T : new()
    {
        var entity = new T();
        var columns = record.GetColumns();
        var members = type.GetAllMembers();
        foreach (var member in members)
        {
            var value = record.GetValue(columns, member.Name);
            member.SetValue(entity, value);
        }

        return entity;
    }
}