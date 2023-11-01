using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql.Base;

public class SqlBuilderWhereParam : SqlBuilderParam
{
    public SqlBuilderWhereParam(string tableName, WhereConditions where = null)
        : base(tableName)
    {
        WhereConditions = where;
    }

    public WhereConditions WhereConditions { get; }
}