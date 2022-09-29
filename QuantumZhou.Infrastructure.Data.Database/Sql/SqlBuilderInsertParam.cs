using QuantumZhou.Infrastructure.Data.Database.Input;
using QuantumZhou.Infrastructure.Data.Database.Sql.Base;
using System;

namespace QuantumZhou.Infrastructure.Data.Database.Sql
{
    public class SqlBuilderInsertParam : SqlBuilderParam
    {
        public SqlBuilderInsertParam(string tableName, InputValues inputValues)
            : base(tableName)
        {
            if (inputValues == null || inputValues.Items.Count < 1)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            Values = inputValues;
        }

        public InputValues Values { get; }
    }

    public class SqlBuilderInsertParam<T> : SqlBuilderInsertParam
    {
        public SqlBuilderInsertParam(InputValues inputValues)
            : base(GetTableName<T>(), inputValues)
        {
        }
    }
}
