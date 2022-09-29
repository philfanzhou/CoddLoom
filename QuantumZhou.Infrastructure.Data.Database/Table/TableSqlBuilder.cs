using QuantumZhou.Infrastructure.Data.Database.Convert;

namespace QuantumZhou.Infrastructure.Data.Database.Table
{
    internal static class TableSqlBuilder
    {
        internal static string GetPrimaryKeySql(DbPrimaryKeyAttribute primaryKey)
        {
            return $"{primaryKey.Name} {DbTypeConverter.ToColumnSql(primaryKey)} PRIMARY KEY NOT NULL";
        }

        internal static string GetColumnSql(DbColumnAttribute column)
        {
            if (column.AllowEmpty)
            {
                return $"{column.Name} {DbTypeConverter.ToColumnSql(column)}";
            }
            else
            {
                return $"{column.Name} {DbTypeConverter.ToColumnSql(column)} NOT NULL";
            }
        }
    }
}
