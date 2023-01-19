namespace Qz.Infra.Database.Condition
{
    public class JoinConditionsItem
    {
        public JoinConditionsItem(string column1, string column2)
        {
            Column1 = column1;
            Column2 = column2;
        }

        public string Column1 { get; }

        public string Column2 { get; }
    }
}
