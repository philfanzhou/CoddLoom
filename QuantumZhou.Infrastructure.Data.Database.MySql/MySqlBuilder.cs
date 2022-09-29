using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;

namespace QuantumZhou.Infrastructure.Data.Database.MySql
{
    public class MySqlBuilder : SqlBuilder
    {
        public override string GetCreateTableSql(TableDefine table)
        {
            return $"CREATE TABLE IF NOT EXISTS {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
        }
    }
}
