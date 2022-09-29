using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql.Base;

namespace QuantumZhou.Infrastructure.Data.Database.Sql
{
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

        public SqlBuilderCountParam(string tableName, WhereParams whereParams)
            : this(tableName, new WhereConditions(whereParams))
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

        public SqlBuilderCountParam(WhereParams whereParams)
            : this(new WhereConditions(whereParams))
        {
        }
    }
}
