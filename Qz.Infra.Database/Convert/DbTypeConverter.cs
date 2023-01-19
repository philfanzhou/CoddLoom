using Qz.Infra.Database.Table;
using System;
using System.Data;

namespace Qz.Infra.Database.Convert
{
    internal static class DbTypeConverter
    {
        internal static DbType ToDbType(Type type)
        {
            var typeName = type.FullName;
            switch (typeName)
            {
                case "System.String":
                    return DbType.String;
                case "System.Int32":
                    return DbType.Int32;
                case "System.Int16":
                    return DbType.Int16;
                case "System.Decimal":
                    return DbType.Decimal;
            }

            throw new NotSupportedException($"{typeName} not support.");
        }

        internal static object ToEntityValue(Type type, object value)
        {
            var typeName = type.FullName;
            switch (typeName)
            {
                case "System.String":
                    return value.ToString();
                case "System.Int32":
                    return int.TryParse(value.ToString(), out var num32) ? num32 : null;
                case "System.Int16":
                    return short.TryParse(value.ToString(), out var num16) ? num16 : null;
                case "System.Decimal":
                    return decimal.TryParse(value.ToString(), out var dec) ? dec : null;
            }

            throw new NotSupportedException($"{typeName} not support.");
        }

        internal static string ToValueSql(string value, DbType dbType)
        {
            switch (dbType)
            {
                case DbType.String:
                    return $"'{value}'";
                case DbType.Int32:
                case DbType.Int16:
                case DbType.Decimal:
                    return $"{value}";
            }

            throw new NotSupportedException($"{dbType} not support.");
        }

        internal static string ToColumnSql(DbColumnBaseAttribute column)
        {
            switch (column.Type)
            {
                case DbType.String:
                    return column.Length > 50 ? $"VARCHAR({column.Length})" : $"CHAR({column.Length})";
                case DbType.Int32:
                    return "INTEGER";
                case DbType.Int16:
                    return "SMALLINT";
                case DbType.Decimal:
                    return "DECIMAL(18,2)";
            }

            throw new NotSupportedException($"{column.Type} not support.");
        }
    }
}
