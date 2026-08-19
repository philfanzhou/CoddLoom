using CoddLoom.Sql;
using CoddLoom.Params;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoddLoom.MySql;

public class MySqlBuilder : SqlBuilder
{
    protected override string GetCreateTableSql(TableDefine table)
    {
        return $"CREATE TABLE IF NOT EXISTS {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
    }

    protected override string GetColumnType(DbType type)
    {
        switch (type)
        {
            case DbType.AnsiString:
                return "VARCHAR(255) CHARACTER SET latin1";  // ANSI string using a single-byte character set
            case DbType.String:
                return "NVARCHAR(255)";  // Unicode string using the utf8mb4 character set by default
            case DbType.StringFixedLength:
                return "CHAR(255) CHARACTER SET utf8mb4";   // Fixed-length Unicode string
            case DbType.AnsiStringFixedLength:
                return "CHAR(255) CHARACTER SET latin1";    // Fixed-length ANSI string
            case DbType.Binary:
                return "BLOB";  // Binary large object
            case DbType.Object:
                return "BLOB";  // Objects can be serialized and stored as BLOBs
            case DbType.Boolean:
                return "TINYINT(1)";  // MySQL commonly represents Boolean values as TINYINT(1)
            case DbType.Byte:
            case DbType.Int16:
            case DbType.Int32:
            case DbType.Int64:
            case DbType.SByte:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
                return "BIGINT"; // MySQL supports BIGINT, INT, SMALLINT, TINYINT, and others
            case DbType.Currency:
                return "DECIMAL(19,4)"; // Currency type
            case DbType.Decimal:
                return "DECIMAL(19,4)"; // Adjust precision and scale as needed
            case DbType.Double:
                return "DOUBLE";
            case DbType.Single:
                return "FLOAT";
            case DbType.VarNumeric:
                return "DECIMAL(19,4)"; // Map VarNumeric to DECIMAL
            case DbType.Date:
                return "DATE";
            case DbType.DateTime:
                return "DATETIME";
            case DbType.DateTime2:
                return "DATETIME(6)"; // High-precision date and time
            case DbType.DateTimeOffset:
                return "DATETIME(6)"; // TIMESTAMP(6) is an alternative
            case DbType.Time:
                return "TIME";
            case DbType.Guid:
                return "CHAR(36)"; // GUIDs are commonly stored as fixed-length strings
            case DbType.Xml:
                return "LONGTEXT"; // XML data can be stored as text
            default:
                throw new NotSupportedException($"{type} not support for column.");
        }
    }

    protected override string GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
    {
        var tableNameParam = new ValueParam(table.Name, "schema_table_name");
        dbParams = new List<ValueParam> { tableNameParam };
        return "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES "
            + $"WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' "
            + $"AND TABLE_NAME = {GetParamName(tableNameParam)}";
    }

    protected override string GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
    {
        var tableNameParam = new ValueParam(table.Name, "schema_table_name");
        dbParams = new List<ValueParam> { tableNameParam };
        return "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS "
            + $"WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {GetParamName(tableNameParam)} "
            + "ORDER BY ORDINAL_POSITION";
    }
}
