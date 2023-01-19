using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;

namespace Qz.Infra.Database.Sql.Base
{
    public abstract class SqlBuilderWhereParam : SqlBuilderParam
    {
        protected SqlBuilderWhereParam(string tableName)
            : base(tableName)
        {
        }

        protected SqlBuilderWhereParam(string tableName, WhereConditions where)
            : base(tableName)
        {
            WhereConditions = where;
            WhereParams = where.WhereParams;
        }

        public WhereConditions WhereConditions { get; }

        public WhereParams WhereParams { get; }
    }
}
