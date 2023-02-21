using System;
using System.Data;

namespace Qz.Infra.Database.Convert;

internal static class DbTypeConverter
{
    internal static DbType ToDbType(Type type)
    {
        var typeName = type.FullName;
        return typeName switch
        {
            "System.String" => DbType.String,
            "System.Int32" => DbType.Int32,
            "System.Int16" => DbType.Int16,
            "System.Decimal" => DbType.Decimal,
            "System.DateTime" => DbType.DateTime,
            "System.Boolean" => DbType.Boolean,
            _ => throw new NotSupportedException($"{typeName} not support.")
        };
    }

    internal static object ToEntityValue(Type type, object value)
    {
        var typeName = type.FullName;
        var strValue = value.ToString().Trim();
        return typeName switch
        {
            "System.String" => strValue,
            "System.Int32" => (int.TryParse(strValue, out var num32) ? num32 : null),
            "System.Int16" => (short.TryParse(strValue, out var num16) ? num16 : null),
            "System.Decimal" => (decimal.TryParse(strValue, out var dec) ? dec : null),
            "System.DateTime" => (DateTime.TryParse(strValue, out var datetime) ? datetime : null),
            "System.Boolean" => (bool.TryParse(strValue, out var bl) ? bl : null),
            _ => throw new NotSupportedException($"{typeName} not support.")
        };
    }
}