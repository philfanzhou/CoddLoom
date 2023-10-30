using Qz.Infra.Database.Params;

namespace Qz.Infra.Database.Condition;

internal class WhereConditionsItem : WhereConditionsItemBase
{
    public WhereConditionsItem(WhereParamsItem paramsItem,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        Param = paramsItem;
        WhereOperator = whereOperator;
        WhereConnecter = connecter;
    }

    public WhereParamsItem Param { get; }

    public WhereOperator WhereOperator { get; protected set; }

    public override string Column => Param?.Column;
}