namespace Qz.Infra.Database.Condition.Internal;

internal class JoinConditionsItem(string column1, string column2)
{
    public string Column1 { get; } = column1;

    public string Column2 { get; } = column2;
}