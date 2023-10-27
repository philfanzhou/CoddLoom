using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql.Base;

public abstract class SqlBuilderWhereParam : SqlBuilderParam
{
    protected SqlBuilderWhereParam(string tableName, WhereConditions where = null)
        : base(tableName)
    {
        WhereConditions = where;
    }

    public WhereConditions WhereConditions { get; }
}