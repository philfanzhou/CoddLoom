using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;

namespace QuantumZhou.Infrastructure.Data.Database.Sql
{
    public class SqlBuilderCountJoinParam : SqlBuilderCountParam
    {
        public SqlBuilderCountJoinParam(SqlBuilder builder, JoinConditions join)
            : base(builder.GetJoinTable(join))
        {
        }

        public SqlBuilderCountJoinParam(SqlBuilder builder, JoinConditions join, WhereConditions where)
            : base(builder.GetJoinTable(join), where)
        {
        }

        public SqlBuilderCountJoinParam(SqlBuilder builder, JoinConditions join, WhereParams whereParams)
            : this(builder, join, new WhereConditions(whereParams))
        {
        }
    }
}
