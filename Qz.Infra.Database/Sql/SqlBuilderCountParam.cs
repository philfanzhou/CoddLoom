using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderCountParam : SqlBuilderWhereParam
{
    public SqlBuilderCountParam(string tableName)
        : base(tableName)
    {
    }

    public SqlBuilderCountParam(string tableName, WhereConditions where)
        : base(tableName, where)
    {
    }
}

public class SqlBuilderCountParam<T> : SqlBuilderCountParam
{
    public SqlBuilderCountParam()
        : base(GetTableName<T>())
    {
    }

    public SqlBuilderCountParam(WhereConditions where)
        : base(GetTableName<T>(), where)
    {
    }
}