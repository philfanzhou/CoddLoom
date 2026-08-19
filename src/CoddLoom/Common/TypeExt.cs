using CoddLoom.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoddLoom.Common;

internal static class TypeExt
{
    private const BindingFlags AllMemberFlags = BindingFlags.Public
                                             | BindingFlags.NonPublic
                                             | BindingFlags.Instance
                                             | BindingFlags.Static;

    internal static MemberInfo[] DoGetAllMembers(Type type)
    {
        var membersDic = new Dictionary<string, MemberInfo>();
        var members = type.GetMembers(AllMemberFlags);
        foreach (var member in members)
        {
            if (!membersDic.ContainsKey(member.Name))
            {
                membersDic.Add(member.Name, member);
            }
        }

        if (type.BaseType != null)
        {
            var baseMembers = DoGetAllMembers(type.BaseType);
            foreach (var member in baseMembers)
            {
                if (!membersDic.ContainsKey(member.Name))
                {
                    membersDic.Add(member.Name, member);
                }
            }
        }

        return membersDic.Values.ToArray();
    }

    internal static MemberInfo[] GetAllMembers(this Type self)
    {
        return TypeMembersCache.Get(self);
    }

    internal static PropertyInfo[] GetAllProperties(this Type self) 
    {
        return self.GetAllMembers().Select(p => p as PropertyInfo)
            .Where(p => p != null).ToArray();
    }

    internal static FieldInfo[] GetAllConstField(this Type self)
    {
        return self.GetAllMembers().Select(p => p as FieldInfo)
            .Where(p => p != null && p.IsLiteral && !p.IsInitOnly).ToArray();
    }

    internal static object GetMemberValue(this MemberInfo member, object obj)
    {
        object value = null;
        if (member is FieldInfo field)
        {
            value = field.GetValue(obj);
        }
        else if (member is PropertyInfo property)
        {
            value = property.GetValue(obj);
        }

        return value;
    }

    internal static Type GetRealDataType(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Nullable.GetUnderlyingType(type);
        }
        return type;
    }

    internal static void SetValue<T>(this MemberInfo memberInfo, T obj, object value)
    {
        if (value is null or DBNull)
        {
            return;
        }

        if (memberInfo is FieldInfo field)
        {
            var setValue = System.Convert.ChangeType(value, field.FieldType.GetRealDataType());
            if (setValue != null)
            {
                field.SetValue(obj, setValue);
            }
        }
        else if (memberInfo is PropertyInfo property)
        {
            var setValue = System.Convert.ChangeType(value, property.PropertyType.GetRealDataType());
            if (setValue != null)
            {
                property.SetValue(obj, setValue);
            }
        }
    }
}