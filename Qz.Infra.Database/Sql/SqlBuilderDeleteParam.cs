using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderDeleteParam : SqlBuilderWhereParam
{
    public SqlBuilderDeleteParam(string tableName, WhereConditions where)
        : base(tableName, where)
    {
        if (where == null || where.IsEmpty())
        {
            throw new System.ArgumentNullException(nameof(where));
        }
    }
}