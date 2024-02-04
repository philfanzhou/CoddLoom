using System.Reflection;

namespace Qz.Infra.Database.Common;

internal static class MemberInfoExt
{
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

    internal static string GetMemberTypeName(this MemberInfo member)
    {
        string type = null;
        if (member is FieldInfo field)
        {
            type = field.FieldType.FullName;
        }
        else if (member is PropertyInfo property)
        {
            type = property.PropertyType.FullName;
        }

        return type;
    }
}