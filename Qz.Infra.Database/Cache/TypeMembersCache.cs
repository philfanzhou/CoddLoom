using Qz.Infra.Database.Common;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Qz.Infra.Database.Cache;

internal static class TypeMembersCache
{
    private static readonly Dictionary<string, MemberInfo[]> MapCache = new();

    internal static MemberInfo[] Get(Type type)
    {
        if(type == null || string.IsNullOrEmpty(type.FullName))
        {
            return null;
        }

        if (MapCache.TryGetValue(type.FullName, out var value))
        {
            return value;
        }

        value = TypeExt.DoGetAllMembers(type);
        MapCache.Add(type.FullName, value);
        return value;
    }
}