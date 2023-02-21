using Qz.Infra.Database.Entity;
using System.Collections.Generic;

namespace Qz.Infra.Database.Cache;

internal static class EntityMapCache
{
    #region Internal cache

    private static readonly Dictionary<string, EntityMap> MapCache = new();

    internal static EntityMap Get<T>()
    {
        var type = typeof(T);
        var name = type.Name;
        if (!MapCache.ContainsKey(name))
        {
            MapCache[name] = new EntityMap(name, type);
        }

        return MapCache[name];
    }

    #endregion
}