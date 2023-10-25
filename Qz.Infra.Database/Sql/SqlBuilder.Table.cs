using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Qz.Infra.Database.Sql;

partial class SqlBuilder
{
    protected internal virtual string GetCreateTableSql(TableDefine table)
    {
        return $"CREATE TABLE {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
    }

    protected virtual string GetCreateColumnsSql(IEnumerable<DbColumnAttribute> columns,
        DbPrimaryKeyAttribute primaryKey = null)
    {
        var columnBuilder = new StringBuilder();

        if (primaryKey != null)
        {
            columnBuilder.Append(GetPrimaryKeySql(primaryKey));
        }

        foreach (var column in columns)
        {
            if (columnBuilder.Length > 0)
            {
                columnBuilder.Append(",");
            }

            columnBuilder.Append(GetColumnSql(column));
        }

        return columnBuilder.ToString();
    }

    protected virtual string GetPrimaryKeySql(DbPrimaryKeyAttribute primaryKey)
    {
        var typeSql = primaryKey.IsIdentity ? "INTEGER" : GetColumnTypeSql(primaryKey);
        var endSql = primaryKey.IsIdentity ? "AUTOINCREMENT" : "NOT NULL";
        return $"{primaryKey.Name} {typeSql} PRIMARY KEY {endSql}";
    }

    protected virtual string GetColumnSql(DbColumnAttribute column)
    {
        var sql = $"{column.Name} {GetColumnTypeSql(column)}";
        if (column.AllowEmpty == false)
        {
            sql += " NOT NULL";
        }
        return sql;
    }

    protected virtual string GetColumnTypeSql(DbColumnBaseAttribute column)
    {
        return column.Type switch
        {
            DbType.String => GetColumnCharType(column),
            DbType.Int32 => "INTEGER",
            DbType.Int16 => "SMALLINT",
            DbType.Decimal => GetColumnDecimalType(column),
            DbType.Boolean => "BIT",
            DbType.Double => "FLOAT",
            DbType.DateTime => "DATETIME",
            DbType.Binary => "BLOB",
            _ => throw new NotSupportedException($"{column.Type} not support.")
        };
    }

    protected virtual string GetColumnCharType(DbColumnBaseAttribute column)
    {
        var sql = "CHAR";
        if (column.FixedLength == false)
        {
            sql = "VAR" + sql;
        }
        sql += $"({column.Length})";

        if (column.AllowUnicode)
        {
            sql = "N" + sql;
        }

        return sql;
    }

    protected virtual string GetColumnDecimalType(DbColumnBaseAttribute column)
    {
        if (!column.FixedLength)
        {
            return "DECIMAL(18,2)";
        }

        return $"DECIMAL({column.Length},{column.PointLength})";
    }
}