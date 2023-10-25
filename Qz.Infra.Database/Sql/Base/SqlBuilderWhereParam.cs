using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using System.Collections.Generic;

namespace Qz.Infra.Database.Sql.Base;

public abstract class SqlBuilderWhereParam : SqlBuilderParam
{
    protected SqlBuilderWhereParam(string tableName)
        : base(tableName)
    {
    }

    protected SqlBuilderWhereParam(string tableName, WhereConditions where)
        : base(tableName)
    {
        WhereConditions = where;
    }

    public WhereConditions WhereConditions { get; }

    internal IEnumerable<IDbParam> WhereParams => WhereConditions?.WhereParams?.Items;
}