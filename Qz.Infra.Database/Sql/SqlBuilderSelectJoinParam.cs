using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderSelectJoinParam : SqlBuilderSelectParam
{
    public SqlBuilderSelectJoinParam(SqlBuilder builder, JoinConditions join,
        OrderByCondition orderBy = null)
        : base(builder.GetJoinTable(join), orderBy)
    {
    }

    public SqlBuilderSelectJoinParam(SqlBuilder builder, JoinConditions join, 
        WhereConditions where, OrderByCondition orderBy = null)
        : base(builder.GetJoinTable(join), where, orderBy)
    {
    }
}