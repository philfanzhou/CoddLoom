using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Data;

namespace Qz.Infra.Database.Oracle;

public class OracleBuilder : SqlBuilder
{
    protected override string GetTableColumnsSql(string tableName)
    {
        return $"SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = '{tableName.ToUpper()}'";
    }
}
