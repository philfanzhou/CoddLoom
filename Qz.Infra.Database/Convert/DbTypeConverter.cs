using System;
using System.Data;
using System.Runtime.CompilerServices;

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
            "System.Byte[]" => DbType.Binary,
            _ => throw new NotSupportedException($"{typeName} not support.")
        };
    }

    internal static object ToEntityValue(Type type, object value)
    {
        var typeName = type.FullName;
        return typeName switch
        {
            "System.String" => value.ToString().Trim(),
            "System.Int32" => int.TryParse(value.ToString(), out var ret) ? ret : null,
            _ => value
            //"System.Int16" => short.TryParse(strValue, out var ret) ? ret : null,
            //"System.Decimal" => decimal.TryParse(strValue, out var ret) ? ret : null,
            //"System.DateTime" => DateTime.TryParse(strValue, out var ret) ? ret : null,
            //"System.Boolean" => bool.TryParse(strValue, out var ret) ? ret : null,
            //"System.Double" => double.TryParse(strValue, out var ret) ? ret : null,
            //_ => throw new NotSupportedException($"{typeName} not support.")
        };
    }
}