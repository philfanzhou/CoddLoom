using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderCountJoinParam : SqlBuilderCountParam
{
    public SqlBuilderCountJoinParam(SqlBuilder builder, JoinConditions join, 
        WhereConditions where = null)
        : base(builder.GetJoinTable(join), where)
    {
    }
}