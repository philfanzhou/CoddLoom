using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using Qz.Infra.Database.Table.Base;
using System;
using System.Linq;

namespace Qz.Infra.Database.SqlServer;

public class SqlServerBuilder : SqlBuilder
{
    protected override string AppendLimit(string sql, PageParam pageParam = null)
    {
        if (pageParam == null) return sql;
        return $"{sql} OFFSET {pageParam.Offset} ROW FETCH NEXT {pageParam.PageSize} ROW ONLY";
    }

    public override string Select(string tableName,
        WhereConditions where = null, OrderByCondition orderBy = null, PageParam pageParam = null, ColumnParam select = null)
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

    protected override string GetColumnBinaryType(DbColumnBinaryAttribute binaryColumn)
    {
        var length = binaryColumn.Length > 8000 ? "MAX" : binaryColumn.Length.ToString();
        return $"VARBINARY({length})";
    }

    protected override string GetPrimaryKeySql(DbPrimaryKeyBaseAttribute primaryKey)
    {
        return $"{primaryKey.Name} {GetColumnTypeSql(primaryKey)} PRIMARY KEY NOT NULL";
    }

    protected override string GetColumnIdentitySql(DbPrimaryKeyIdentityAttribute column)
    {
        return "BIGINT IDENTITY(1,1)";
    }
}