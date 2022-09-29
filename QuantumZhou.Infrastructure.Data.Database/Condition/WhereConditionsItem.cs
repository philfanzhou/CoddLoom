using QuantumZhou.Infrastructure.Data.Database.Params;

namespace QuantumZhou.Infrastructure.Data.Database.Condition
{
    public class WhereConditionsItem
    {
        protected WhereConditionsItem()
        {
        }

        public WhereConditionsItem(WhereParamsItem paramsItem, 
            WhereOperator whereOperator = WhereOperator.Equal,
            WhereConnecter connecter = WhereConnecter.And)
        {
            Column = paramsItem.Column;
            WhereOperator = whereOperator;
            ParamName = paramsItem.Name;
            WhereConnecter = connecter;
        }

        public string Column { get; protected set; }

        public WhereOperator WhereOperator { get; protected set; }

        public string ParamName { get; protected set; }

        public WhereConnecter WhereConnecter { get; protected set; }
    }
}
