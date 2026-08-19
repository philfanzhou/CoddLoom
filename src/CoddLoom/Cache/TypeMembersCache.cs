using Qz.Infra.Database.Common;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Qz.Infra.Database.Cache;

internal static class TypeMembersCache
{
    private static readonly ConcurrentDictionary<string, MemberInfo[]> MemberInfoCache = new();

    internal static MemberInfo[] Get(Type type)
    {
        if(type == null || string.IsNullOrEmpty(type.FullName))
        {
            return null;
        }

        if (!MemberInfoCache.TryGetValue(type.FullName, out var memberInfo))
        {
            memberInfo = TypeExt.DoGetAllMembers(type);
            MemberInfoCache.TryAdd(type.FullName, memberInfo);
        }

        return memberInfo;
    }
}