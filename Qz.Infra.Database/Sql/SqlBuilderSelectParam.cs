using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderSelectParam : SqlBuilderWhereParam
{
    public SqlBuilderSelectParam(string tableName, WhereConditions where = null, OrderByCondition orderBy = null)
        : base(tableName, where)
    {
        OrderBy = orderBy;
    }

    public OrderByCondition OrderBy { get; }
}