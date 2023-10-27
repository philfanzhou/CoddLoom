using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderDeleteParam : SqlBuilderWhereParam
{
    public SqlBuilderDeleteParam(string tableName, WhereConditions where)
        : base(tableName, where)
    {
        if (where == null || where.Items.Count < 1)
        {
            throw new System.ArgumentNullException(nameof(where));
        }
    }
}

public class SqlBuilderDeleteParam<T> : SqlBuilderDeleteParam
{
    public SqlBuilderDeleteParam(WhereConditions where)
        : base(GetTableName<T>(), where)
    {
    }
}