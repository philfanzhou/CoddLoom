using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using Qz.Infra.Database.Table.Base;
using System;

namespace Qz.Infra.Database.SqlServer;

public class SqlServerBuilder : SqlBuilder
{
    protected override string GetColumnBinaryType(DbColumnBinaryAttribute binaryColumn)
    {
        var length = binaryColumn.Length > 8000 ? "MAX" : binaryColumn.Length.ToString();
        return $"VARBINARY({length})";
    }

    protected override string GetPrimaryKeySql(DbPrimaryKeyBaseAttribute primaryKey)
    {
        var isIdentity = primaryKey is DbPrimaryKeyIdentityAttribute;
        var keySql = isIdentity ? "BIGINT IDENTITY(1,1)" : GetPrimaryKeyTypeSql(primaryKey);
        return $"{primaryKey.Name} {keySql} PRIMARY KEY NOT NULL";
    }

    protected override string AppendLimit(string sql, int count, int offset = 0)
    {
        if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

        return $"{sql} OFFSET {offset} ROW FETCH NEXT {count} ROW ONLY";
    }

    public override string First(string tableName, WhereConditions where = null, OrderByCondition orderBy = null)
    {
        var selectSql = Select(tableName, where, orderBy);
        return selectSql.Replace(KeyWordSelect, "SELECT TOP 1");
    }

    public override string Take(string tableName, int offset, int count, WhereConditions where = null, OrderByCondition orderBy = null)
    {
        if (orderBy == null)
        {
            throw new ArgumentNullException(nameof(orderBy), "SqlServer can not use 'OFFSET' keyword without order by condition");
        }
        return base.Take(tableName, offset, count, where, orderBy);
    }
}