using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;

namespace Qz.Infra.Database.MySql;

public class MySqlBuilder : SqlBuilder
{
    protected override string GetCreateTableSql(TableDefine table)
    {
        return $"CREATE TABLE IF NOT EXISTS {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
    }
}