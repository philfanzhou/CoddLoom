using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;

namespace QuantumZhou.Infrastructure.Data.Database.Sql
{
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

        public SqlBuilderSelectJoinParam(SqlBuilder builder, JoinConditions join, 
            WhereParams whereParams, OrderByCondition orderBy = null)
            : this(builder, join, new WhereConditions(whereParams), orderBy)
        {
        }
    }
}
