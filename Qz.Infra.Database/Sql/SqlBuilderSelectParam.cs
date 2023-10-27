using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql;

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
}