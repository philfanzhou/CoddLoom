using CoddLoom.Params;
using CoddLoom.Sql;
using System.Collections.Generic;
using System.Linq;

namespace CoddLoom.Condition.Internal;

internal class WhereConditionsInItem(string column, IEnumerable<ValueParam> valueParams, WhereConnector connector)
    : WhereItemBase(column, connector)
{
    private readonly List<ValueParam> _valueParams = valueParams.ToList();

    public IEnumerable<ValueParam> Parameters => _valueParams.AsReadOnly();

    protected internal override string ToSql(SqlBuilder builder)
    {
        return builder.GetInCondition(Column, Parameters);
    }
}