using System;
using System.Collections.Generic;
using System.Reflection;

namespace Qz.Infra.Database.Common;

internal static class TypeExt
{
    private const BindingFlags AllMemberFlags = BindingFlags.Public
                                             | BindingFlags.NonPublic
                                             | BindingFlags.Instance
                                             | BindingFlags.Static;

    internal static MemberInfo[] GetAllMembers(this Type self)
    {
        var members = new List<MemberInfo>();
        members.AddRange(self.GetMembers(AllMemberFlags));

        if(self.BaseType != null)
        {
            members.AddRange(self.BaseType.GetAllMembers());
        }

        return members.ToArray();
    }
}