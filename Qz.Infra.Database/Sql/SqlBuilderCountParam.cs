using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderCountParam : SqlBuilderWhereParam
{
    public SqlBuilderCountParam(string tableName, WhereConditions where = null)
        : base(tableName, where)
    {
    }
}