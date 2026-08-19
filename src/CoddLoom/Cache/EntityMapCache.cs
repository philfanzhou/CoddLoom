using CoddLoom.Common;
using CoddLoom.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CoddLoom.Cache;

internal static class EntityMapCache
{
    private static readonly ConcurrentDictionary<string, EntityMap> MapCache = new();

    internal static EntityMap Get(Type type)
    {
        var name = type.Name;
        if (!MapCache.TryGetValue(name, out var entityMap))
        {
            entityMap = new EntityMap(name, type);
            MapCache.TryAdd(name, entityMap);
        }
        return entityMap;
    }

    internal static EntityMap Get<T>()
    {
        var type = typeof(T);
        return Get(type);
    }


    internal static string GetTableName<T>()
    {
        return Get<T>().Table.Name;
    }
}