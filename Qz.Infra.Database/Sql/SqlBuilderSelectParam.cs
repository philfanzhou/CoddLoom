using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;

namespace Qz.Infra.Database.Sql
{
    public class SqlBuilderSelectParam : SqlBuilderCountParam
    {
        public SqlBuilderSelectParam(string tableName, OrderByCondition orderBy = null)
            : base(tableName)
        {
            OrderBy = orderBy;
        }

        public SqlBuilderSelectParam(string tableName, WhereConditions where, OrderByCondition orderBy = null)
            : base(tableName, where)
        {
            OrderBy = orderBy;
        }

        public SqlBuilderSelectParam(string tableName, WhereParams whereParams, OrderByCondition orderBy = null)
            : this(tableName, new WhereConditions(whereParams), orderBy)
        {
        }

        public OrderByCondition OrderBy { get; }
    }

    public class SqlBuilderSelectParam<T> : SqlBuilderSelectParam
    {
        public SqlBuilderSelectParam(OrderByCondition orderBy = null)
            : base(GetTableName<T>(), orderBy)
        {
        }

        public SqlBuilderSelectParam(WhereConditions where, OrderByCondition orderBy = null)
            : base(GetTableName<T>(), where, orderBy)
        {
        }

        public SqlBuilderSelectParam(WhereParams whereParams, OrderByCondition orderBy = null)
            : this(new WhereConditions(whereParams), orderBy)
        {
        }
    }
}
