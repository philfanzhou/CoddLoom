using CoddLoom.Sql;
using CoddLoom.Params;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoddLoom.Oracle;

public class OracleBuilder : SqlBuilder
{
    protected override string GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
    {
        var tableNameParam = new ValueParam(table.Name.ToUpperInvariant(), "schema_table_name");
        dbParams = new List<ValueParam> { tableNameParam };
        return $"SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = {GetParamName(tableNameParam)}";
    }

    protected override string GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
    {
        var tableNameParam = new ValueParam(table.Name.ToUpperInvariant(), "schema_table_name");
        dbParams = new List<ValueParam> { tableNameParam };
        return "SELECT COLUMN_NAME FROM USER_TAB_COLUMNS "
            + $"WHERE TABLE_NAME = {GetParamName(tableNameParam)} ORDER BY COLUMN_ID";
    }
}
