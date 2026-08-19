using CoddLoom.Sql;
using CoddLoom.Table;
using System;
using System.Data;

namespace CoddLoom.Oracle;

public class OracleBuilder : SqlBuilder
{
    protected override string GetTableColumnsSql(string tableName)
    {
        return $"SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = '{tableName.ToUpper()}'";
    }
}
