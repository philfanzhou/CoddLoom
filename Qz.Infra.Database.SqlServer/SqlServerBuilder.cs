using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Data;

namespace Qz.Infra.Database.SqlServer;

public class SqlServerBuilder : SqlBuilder
{
    protected override string GetColumnTypeSql(DbColumnBaseAttribute column)
    {
        if (column.Type == DbType.Binary)
        {
            var length = column.Length > 8000 ? "MAX" : column.Length.ToString();
            return $"VARBINARY({length})";
        }
        return base.GetColumnTypeSql(column);
    }

    protected override string GetPrimaryKeySql(DbPrimaryKeyAttribute primaryKey)
    {
        var keySql = primaryKey.IsIdentity ? "BIGINT IDENTITY(1,1)" : GetColumnTypeSql(primaryKey);
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

    //protected override string GetUnicodeStringValue(IDbParam parameter)
    //{
    //    var strValue = string.Empty;
    //    if (parameter.Value != null)
    //    {
    //        strValue = parameter.Value.ToString();
    //    }

    //    return $"N'{strValue}'";
    //}

    //protected override string GetU(IDbParam parameter, bool isUnicode)
    //{
    //    var strValue = string.Empty;
    //    if (parameter.Value != null)
    //    {
    //        strValue = parameter.Value.ToString();
    //    }

    //    if (strValue.Contains("'"))
    //    {
    //        strValue = strValue.Replace("'", "''");
    //    }

    //    if (isUnicode)
    //    {
    //        return $"N'{strValue}'";
    //    }
    //    else
    //    {
    //        return $"'{strValue}'";
    //    }
    //}
}