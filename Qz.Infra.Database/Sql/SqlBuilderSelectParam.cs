using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderSelectParam : SqlBuilderCountParam
{
    public SqlBuilderSelectParam(string tableName, WhereConditions where, OrderByCondition orderBy = null)
        : base(tableName, where)
    {
        OrderBy = orderBy;
    }

    public SqlBuilderSelectParam(string tableName, OrderByCondition orderBy = null)
        : this(tableName, null, orderBy)
    {
    }

    public OrderByCondition OrderBy { get; }
}

public class SqlBuilderSelectParam<T> : SqlBuilderSelectParam
{
    public SqlBuilderSelectParam(WhereConditions where, OrderByCondition orderBy = null)
        : base(GetTableName<T>(), where, orderBy)
    {
    }

    public SqlBuilderSelectParam(OrderByCondition orderBy = null)
        : this(null, orderBy)
    {
    }
}