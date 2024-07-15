using Qz.Infra.Database.Common;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Qz.Infra.Database.Entity;

internal class EntityMap
{
    internal EntityMap(string name, Type type)
    {
        Name = name;

        Table = GetTableAttribute(type);
        if (Table == null)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        var memberList = new List<Tuple<MemberInfo, MapColumnAttribute>>();
        var members = type.GetAllMembers();
        foreach (var member in members)
        {
            var attribute = member.GetCustomAttribute<MapColumnAttribute>(true);
            if (attribute != null)
            {
                memberList.Add(new Tuple<MemberInfo, MapColumnAttribute>(member, attribute));
                if (attribute.PrimaryKey)
                {
                    PrimaryKey = attribute.Name;
                }
            }
        }

        if (memberList.Count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Members = memberList.AsReadOnly();
    }

    #region Property

    internal string Name { get; }

    internal MapTableAttribute Table { get; }

    internal string PrimaryKey { get; }

    internal IReadOnlyList<Tuple<MemberInfo, MapColumnAttribute>> Members { get; }

    #endregion

    internal static bool HasMap(Type type)
    {
        var table = GetTableAttribute(type);
        return table != null;
    }

    private static MapTableAttribute GetTableAttribute(MemberInfo type)
    {
        return type.GetCustomAttribute<MapTableAttribute>();
    }
}