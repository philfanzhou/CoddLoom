namespace QuantumZhou.Infrastructure.Data.Database.Condition
{
    public class WhereConditionsIsItem : WhereConditionsItem
    {
        public WhereConditionsIsItem(string column, bool isNull,
            WhereConnecter whereConnecter = WhereConnecter.And)
        {
            Column = column;
            IsNull = isNull;
            WhereConnecter = whereConnecter;
            WhereOperator = WhereOperator.Equal;
        }

        public bool IsNull { get; }
    }
}
