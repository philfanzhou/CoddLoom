using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderSelectParam : SqlBuilderCountParam
{
    public SqlBuilderSelectParam(string tableName, WhereConditions where = null, OrderByCondition orderBy = null)
        : base(tableName, where)
    {
        OrderBy = orderBy;
    }

    public OrderByCondition OrderBy { get; }
}