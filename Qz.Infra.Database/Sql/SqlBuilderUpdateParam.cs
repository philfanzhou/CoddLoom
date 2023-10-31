using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Sql.Base;
using System;

namespace Qz.Infra.Database.Sql;

public class SqlBuilderUpdateParam : SqlBuilderWhereParam
{
    public SqlBuilderUpdateParam(string tableName, InputValues inputValues, WhereConditions where)
        : base(tableName, where)
    {
        if (inputValues == null 
            || inputValues.IsEmpty())
        {
            throw new ArgumentNullException(nameof(inputValues));
        }

        if (where == null || where.IsEmpty())
        {
            throw new ArgumentNullException(nameof(where));
        }

        Values = inputValues;
    }

    public InputValues Values { get; }
}

public class SqlBuilderUpdateParam<T> : SqlBuilderUpdateParam
{
    public SqlBuilderUpdateParam(InputValues inputValues, WhereConditions where)
        : base(GetTableName<T>(), inputValues, where)
    {
    }
}