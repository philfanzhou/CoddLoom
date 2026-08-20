using CoddLoom.Common;
using CoddLoom.Entity;
using System;
using System.Collections.Concurrent;

namespace CoddLoom.Cache;

internal static class EntityMapCache
{
    private static readonly ConcurrentDictionary<Type, EntityMap> MapCache = new();

    internal static EntityMap Get(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return MapCache.GetOrAdd(type, currentType => new EntityMap(currentType.Name, currentType));
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
