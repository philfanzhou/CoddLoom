using Qz.Infra.Database.Table;
using Qz.Infra.Database.Table.Base;
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

    protected virtual string GetCreateColumnsSql(IEnumerable<DbColumnBaseAttribute> columns,
        DbPrimaryKeyBaseAttribute primaryKey = null)
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

    protected virtual string GetPrimaryKeySql(DbPrimaryKeyBaseAttribute primaryKey)
    {
        var isIdentity = primaryKey is DbPrimaryKeyIdentityAttribute;
        var endSql = isIdentity ? "AUTOINCREMENT" : "NOT NULL";
        return $"{primaryKey.Name} {GetPrimaryKeyTypeSql(primaryKey)} PRIMARY KEY {endSql}";
    }

    protected virtual string GetColumnSql(DbColumnBaseAttribute column)
    {
        var sql = $"{column.Name} {GetColumnTypeSql(column)}";
        if (column.AllowEmpty)
        {
            return sql;
        }

        return $"{sql} NOT NULL";
    }

    protected virtual string GetPrimaryKeyTypeSql(DbPrimaryKeyBaseAttribute primaryKey)
    {
        if (primaryKey is DbPrimaryKeyIdentityAttribute)
        {
            return "INTEGER";
        }

        if (primaryKey is DbPrimaryKeyStringAttribute stringPrimaryKey)
        {
            return GetColumnCharType(stringPrimaryKey);
        }

        if (primaryKey is DbPrimaryKeyAttribute normalPrimaryKey)
        {
            return normalPrimaryKey.Type switch
            {
                DbType.Int16 => "SMALLINT",
                DbType.Int32 => "INTEGER",
                DbType.Int64 => "BIGINT",
                DbType.DateTime => "DATETIME",
                _ => throw new NotSupportedException($"{normalPrimaryKey.Type} not support for PrimaryKey.")
            };
        }


        throw new NotSupportedException(primaryKey.GetType().Name);
    }

    protected virtual string GetColumnTypeSql(DbColumnBaseAttribute column)
    {
        if (column is DbColumnStringAttribute stringColumn)
        {
            return GetColumnCharType(stringColumn);
        }

        if (column is DbColumnDecimalAttribute decimalColumn)
        {
            return GetColumnDecimalType(decimalColumn);
        }

        if (column is DbColumnBinaryAttribute binaryColumn)
        {
            return GetColumnBinaryType(binaryColumn);
        }

        if (column is DbColumnAttribute normalColumn)
        {
            return normalColumn.Type switch
            {
                DbType.Int16 => "SMALLINT",
                DbType.Int32 => "INTEGER",
                DbType.Int64 => "BIGINT",
                DbType.Double => "FLOAT",
                DbType.Boolean => "BIT",
                DbType.DateTime => "DATETIME",
                _ => throw new NotSupportedException($"{normalColumn.Type} not support for column.")
            };
        }

        throw new NotSupportedException(column.GetType().Name);
    }

    protected virtual string GetColumnCharType(IStringColumn column)
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

    protected virtual string GetColumnDecimalType(DbColumnDecimalAttribute decimalColumn)
    {
        return $"DECIMAL({decimalColumn.Length},{decimalColumn.PointLength})";
    }

    protected virtual string GetColumnBinaryType(DbColumnBinaryAttribute binaryColumn)
    {
        return "BLOB";
    }
}