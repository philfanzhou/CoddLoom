using CoddLoom.Cache;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            var setValue = ConvertValue(value, field.FieldType.GetRealDataType());
            if (setValue != null)
            {
                field.SetValue(obj, setValue);
            }
        }
        else if (memberInfo is PropertyInfo property)
        {
            var setValue = ConvertValue(value, property.PropertyType.GetRealDataType());
            if (setValue != null)
            {
                property.SetValue(obj, setValue);
            }
        }
    }

    private static object ConvertValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(Guid))
        {
            if (value is string stringValue)
            {
                return Guid.Parse(stringValue);
            }

            if (value is byte[] bytes)
            {
                return new Guid(bytes);
            }
        }

        if (targetType.IsEnum)
        {
            return value is string enumName
                ? Enum.Parse(targetType, enumName, true)
                : Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(decimal) && value is string decimalValue)
        {
            return decimal.Parse(decimalValue, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(DateTimeOffset))
        {
            if (value is string dateTimeOffsetValue)
            {
                return DateTimeOffset.Parse(dateTimeOffsetValue, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);
            }

            if (value is DateTime dateTimeValue)
            {
                return new DateTimeOffset(dateTimeValue);
            }
        }

        return System.Convert.ChangeType(value, targetType);
    }
}
