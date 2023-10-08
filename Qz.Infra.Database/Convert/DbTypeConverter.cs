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
            "System.Double" => DbType.Double,
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
            "System.Int32" => int.TryParse(strValue, out var ret) ? ret : null,
            "System.Int16" => short.TryParse(strValue, out var ret) ? ret : null,
            "System.Decimal" => decimal.TryParse(strValue, out var ret) ? ret : null,
            "System.DateTime" => DateTime.TryParse(strValue, out var ret) ? ret : null,
            "System.Boolean" => bool.TryParse(strValue, out var ret) ? ret : null,
            "System.Double" => double.TryParse(strValue, out var ret) ? ret : null,
            _ => throw new NotSupportedException($"{typeName} not support.")
        };
    }
}