using Qz.Infra.Database.Entity;
using System;
using System.Collections.Generic;

namespace Qz.Infra.Database.Cache;

internal static class EntityMapCache
{
    private static readonly Dictionary<string, EntityMap> MapCache = new();

    internal static EntityMap Get(Type type)
    {
        var name = type.Name;
        if (!MapCache.ContainsKey(name))
        {
            MapCache[name] = new EntityMap(name, type);
        }

        return MapCache[name];
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