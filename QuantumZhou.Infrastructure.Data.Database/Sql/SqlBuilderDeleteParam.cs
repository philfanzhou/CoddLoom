using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql.Base;

namespace QuantumZhou.Infrastructure.Data.Database.Sql
{
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

        public SqlBuilderDeleteParam(string tableName, WhereParams whereParams)
            : this(tableName, new WhereConditions(whereParams))
        {
        }
    }

    public class SqlBuilderDeleteParam<T> : SqlBuilderDeleteParam
    {
        public SqlBuilderDeleteParam(WhereConditions where)
            : base(GetTableName<T>(), where)
        {
        }

        public SqlBuilderDeleteParam(WhereParams whereParams)
            : this(new WhereConditions(whereParams))
        {
        }
    }
}
