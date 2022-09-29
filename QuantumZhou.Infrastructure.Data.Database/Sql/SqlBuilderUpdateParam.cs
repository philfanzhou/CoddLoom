using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Input;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql.Base;
using System;

namespace QuantumZhou.Infrastructure.Data.Database.Sql
{
    public class SqlBuilderUpdateParam : SqlBuilderWhereParam
    {
        public SqlBuilderUpdateParam(string tableName, InputValues inputValues, WhereConditions where)
            : base(tableName, where)
        {
            if (inputValues == null || inputValues.Items.Count < 1)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            if (where == null || where.Items.Count < 1)
            {
                throw new ArgumentNullException(nameof(where));
            }

            Values = inputValues;
        }

        public SqlBuilderUpdateParam(string tableName, InputValues inputValues, WhereParams whereParams)
            : this(tableName, inputValues, new WhereConditions(whereParams))
        {
        }

        public InputValues Values { get; }
    }

    public class SqlBuilderUpdateParam<T> : SqlBuilderUpdateParam
    {
        public SqlBuilderUpdateParam(InputValues inputValues, WhereConditions where)
            : base(GetTableName<T>(), inputValues, where)
        {
        }

        public SqlBuilderUpdateParam(InputValues inputValues, WhereParams whereParams)
            : this(inputValues, new WhereConditions(whereParams))
        {
        }
    }
}
