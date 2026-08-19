using CoddLoom.Params;
using CoddLoom.Table;
using CoddLoom.Table.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace CoddLoom.Sql;

partial class SqlBuilder
{
    protected internal virtual string DropTableSql(string tableName)
    {
        return $"DROP TABLE IF EXISTS {tableName}";
    }

    protected internal virtual string GetCreateTableSql(TableDefine table)
    {
        return $"CREATE TABLE {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
    }

    protected internal virtual string GetAddColumnSql(string tableName, DbColumnBaseAttribute column)
    {
        return $"ALTER TABLE {tableName} ADD {GetColumnSql(column)}";
    }

    protected internal virtual string GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
    {
        var objectTypeParam = new ValueParam("table", "schema_object_type");
        var tableNameParam = new ValueParam(table.Name, "schema_table_name");
        dbParams = new List<ValueParam>
        {
            objectTypeParam,
            tableNameParam
        };
        return $"SELECT COUNT(*) FROM sqlite_master "
            + $"WHERE type = {GetParamName(objectTypeParam)} AND name = {GetParamName(tableNameParam)}";
    }

    protected internal virtual string GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
    {
        // Preserve dispatch to providers compiled against the former string-based
        // extension point. New providers should override this parameterized overload.
        var legacyMethod = GetType().GetMethod(
            nameof(GetTableColumnsSql),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);

        var isLegacyOverride = legacyMethod != null
            && legacyMethod.DeclaringType != typeof(SqlBuilder)
            && legacyMethod.GetBaseDefinition().DeclaringType == typeof(SqlBuilder);
        if (isLegacyOverride)
        {
            dbParams = new List<ValueParam>();
#pragma warning disable CS0618 // Required while the obsolete provider hook remains supported.
            return GetTableColumnsSql(table.Name);
#pragma warning restore CS0618
        }

        var tableNameParam = new ValueParam(table.Name, "schema_table_name");
        dbParams = new List<ValueParam>
        {
            tableNameParam
        };
        return $"SELECT name FROM pragma_table_info({GetParamName(tableNameParam)})";
    }

    [Obsolete("Use the parameterized TableDefine overload instead.")]
    protected internal virtual string GetTableColumnsSql(string tableName)
    {
        return $"SELECT name FROM pragma_table_info('{tableName}')";
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
        return $"{primaryKey.Name} {GetColumnTypeSql(primaryKey)} PRIMARY KEY {endSql}";
    }

    protected virtual string GetColumnSql(DbColumnBaseAttribute column)
    {
        var sql = $"{column.Name} {GetColumnTypeSql(column)}";
        if (!column.AllowEmpty)
        {
            sql += $" NOT NULL";
        }
        return sql;
    }

    #region ColumnType

    protected virtual string GetColumnTypeSql(DbBaseAttribute column)
    {
        if (column is DbPrimaryKeyIdentityAttribute identityColumn)
        {
            return GetIdentityColumnType(identityColumn);
        }

        if (column is IStringColumn stringColumn)
        {
            return GetStringColumnType(stringColumn);
        }

        if (column is INormalColumn normalColumn)
        {
            return GetColumnType(normalColumn.Type);
        }

        if (column is DbColumnDecimalAttribute decimalColumn)
        {
            return GetDecimalColumnType(decimalColumn);
        }

        if (column is DbColumnBinaryAttribute binaryColumn)
        {
            return GetBinaryColumnType(binaryColumn);
        }

        throw new NotSupportedException(column.GetType().Name);
    }

    protected virtual string GetColumnType(DbType type)
    {
        switch (type)
        {
            case DbType.AnsiString:
            case DbType.String:
            case DbType.StringFixedLength:
            case DbType.AnsiStringFixedLength:
            case DbType.Xml:
            case DbType.Guid:
                return "TEXT";
            case DbType.Binary:
            case DbType.Object:
                return "BLOB";
            case DbType.Boolean:
                return "INTEGER";
            case DbType.Byte:
            case DbType.Int16:
            case DbType.Int32:
            case DbType.Int64:
            case DbType.SByte:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
                return "INTEGER";
            case DbType.Currency:
            case DbType.Decimal:
            case DbType.Double:
            case DbType.Single:
            case DbType.VarNumeric:
                return "REAL";
            case DbType.Date:
            case DbType.DateTime:
            case DbType.DateTime2:
            case DbType.DateTimeOffset:
            case DbType.Time:
                return "TEXT";
            default:
                throw new NotSupportedException($"{type} not support for column.");
        }
    }

    protected virtual string GetIdentityColumnType(DbPrimaryKeyIdentityAttribute column)
    {
        return "INTEGER";
    }

    protected virtual string GetStringColumnType(IStringColumn column)
    {
        return "TEXT";
    }

    protected virtual string GetDecimalColumnType(DbColumnDecimalAttribute decimalColumn)
    {
        return "REAL";
    }

    protected virtual string GetBinaryColumnType(DbColumnBinaryAttribute binaryColumn)
    {
        return "BLOB";
    }

    #endregion
}
