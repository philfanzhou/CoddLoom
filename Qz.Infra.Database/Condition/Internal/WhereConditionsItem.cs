using Qz.Infra.Database.Params;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsItem : WhereConditionsItemBase
{
    public WhereConditionsItem(ColumnValueParameter parameter,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        Parameter = parameter;
        WhereOperator = whereOperator;
        WhereConnecter = connecter;
    }

    public ColumnValueParameter Parameter { get; }

    public WhereOperator WhereOperator { get; protected set; }

    public override string Column => Parameter.Column;
}