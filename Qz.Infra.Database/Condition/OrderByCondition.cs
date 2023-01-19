namespace Qz.Infra.Database.Condition
{
    public class OrderByCondition
    {
        public OrderByCondition(string column, bool descending = false)
        {
            Column = column;
            Descending = descending;
        }

        public string Column { get; }

        public bool Descending { get; }
    }
}
