using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Data;

namespace Qz.Infra.Database.MySql;

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
                return "VARCHAR(255) CHARACTER SET latin1";  // ANSI 字符串，使用单字节字符集
            case DbType.String:
                return "NVARCHAR(255)";  // Unicode 字符串，默认使用 utf8mb4 字符集
            case DbType.StringFixedLength:
                return "CHAR(255) CHARACTER SET utf8mb4";   // 固定长度 Unicode 字符串
            case DbType.AnsiStringFixedLength:
                return "CHAR(255) CHARACTER SET latin1";    // 固定长度 ANSI 字符串
            case DbType.Binary:
                return "BLOB";  // 二进制大对象
            case DbType.Object:
                return "BLOB";  // 对象可以序列化后存储为 BLOB
            case DbType.Boolean:
                return "TINYINT(1)";  // MySQL 中布尔值通常用 TINYINT(1) 表示
            case DbType.Byte:
            case DbType.Int16:
            case DbType.Int32:
            case DbType.Int64:
            case DbType.SByte:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
                return "BIGINT"; // MySQL 支持 BIGINT, INT, SMALLINT, TINYINT 等
            case DbType.Currency:
                return "DECIMAL(19,4)"; // 货币类型
            case DbType.Decimal:
                return "DECIMAL(19,4)"; // 可以根据精度和小数位数调整
            case DbType.Double:
                return "DOUBLE";
            case DbType.Single:
                return "FLOAT";
            case DbType.VarNumeric:
                return "DECIMAL(19,4)"; // VarNumeric 映射到 DECIMAL
            case DbType.Date:
                return "DATE";
            case DbType.DateTime:
                return "DATETIME";
            case DbType.DateTime2:
                return "DATETIME(6)"; // 高精度时间
            case DbType.DateTimeOffset:
                return "DATETIME(6)"; // 或者使用 TIMESTAMP(6)
            case DbType.Time:
                return "TIME";
            case DbType.Guid:
                return "CHAR(36)"; // GUID 通常作为固定长度的字符串存储
            case DbType.Xml:
                return "LONGTEXT"; // XML 数据可以作为文本存储
            default:
                throw new NotSupportedException($"{type} not support for column.");
        }
    }

    public override string Procedure(string name)
    {
        return $"CALL {name}";
    }
}