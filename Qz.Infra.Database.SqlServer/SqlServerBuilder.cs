using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using Qz.Infra.Database.Table.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database.SqlServer;

public class SqlServerBuilder : SqlBuilder
{
    protected override string GetPrimaryKeySql(DbPrimaryKeyBaseAttribute primaryKey)
    {
        return $"{primaryKey.Name} {GetColumnTypeSql(primaryKey)} PRIMARY KEY NOT NULL";
    }

    protected override string AppendLimit(string sql, PageParam pageParam = null)
    {
        if (pageParam == null) return sql;
        return $"{sql} OFFSET {pageParam.Offset} ROW FETCH NEXT {pageParam.PageSize} ROW ONLY";
    }

    public override string Select(string tableName,
        WhereConditions where = null, OrderByCondition orderBy = null, 
        PageParam pageParam = null, ColumnParam select = null)
    {
        if (pageParam != null && orderBy == null)
        {
            if (where == null)
            {
                throw new ArgumentNullException("", "SqlServer can not use 'OFFSET' keyword without order by condition");
            }

            orderBy = new OrderByCondition(where.Parameters.First().Column);
        }
        return base.Select(tableName, where, orderBy, pageParam, select);
    }

    public override string Procedure(string name, IEnumerable<ValueParam> parameters = null)
    {
        var sql = $"EXECUTE {name}";
        if (parameters != null)
        {
            sql += string.Join(", ", parameters.Select(GetParamName));
        }
        return sql;
    }

    #region ColumnType

    protected override string GetColumnType(DbType type)
    {
        switch (type)
        {
            case DbType.Object:
                return "VARBINARY(MAX)"; // 或 BINARY, IMAGE
            case DbType.Boolean:
                return "BIT";
            case DbType.Byte:
            case DbType.Int16:
            case DbType.Int32:
            case DbType.Int64:
            case DbType.SByte:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
                return "BIGINT"; // SQL Server 支持 BIGINT, INT, SMALLINT, TINYINT 等
            case DbType.Currency:
                return "MONEY"; // 或者使用 DECIMAL/PRECISION
            case DbType.Double:
                return "FLOAT";
            case DbType.Single:
                return "REAL";
            case DbType.Date:
                return "DATE";
            case DbType.DateTime:
                return "DATETIME";
            case DbType.DateTime2:
                return "DATETIME2";
            case DbType.DateTimeOffset:
                return "DATETIMEOFFSET";
            case DbType.Time:
                return "TIME";
            case DbType.Guid:
                return "UNIQUEIDENTIFIER";
            case DbType.Xml:
                return "XML";
            default:
                throw new NotSupportedException($"{type} not support for column.");
        }
    }

    protected override string GetStringColumnType(IStringColumn column)
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

    protected override string GetBinaryColumnType(DbColumnBinaryAttribute binaryColumn)
    {
        var length = binaryColumn.Length > 8000 ? "MAX" : binaryColumn.Length.ToString();
        return $"VARBINARY({length})";
    }

    protected override string GetIdentityColumnType(DbPrimaryKeyIdentityAttribute column)
    {
        return "BIGINT IDENTITY(1,1)";
    }

    protected override string GetDecimalColumnType(DbColumnDecimalAttribute decimalColumn)
    {
        return $"DECIMAL({decimalColumn.Length},{decimalColumn.PointLength})";
    }

    #endregion
}