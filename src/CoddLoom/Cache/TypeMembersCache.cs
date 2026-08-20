using CoddLoom.Common;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace CoddLoom.Cache;

internal static class TypeMembersCache
{
    private static readonly ConcurrentDictionary<Type, MemberInfo[]> MemberInfoCache = new();

    internal static MemberInfo[] Get(Type type)
    {
        if(type == null)
        {
            return null;
        }

        return MemberInfoCache.GetOrAdd(type, TypeExt.DoGetAllMembers);
    }
}
