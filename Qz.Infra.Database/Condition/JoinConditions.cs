using System.Collections.Generic;

namespace Qz.Infra.Database.Condition;

public class JoinConditions
{
    private readonly List<JoinConditionsItem> _columns = new();

    public JoinConditions(string tableName1, string column1, string tableName2, string column2, 
        JoinType type = JoinType.Inner)
    {
        Table1 = tableName1;
        Table2 = tableName2;
        Type = type;
        Add(column1, column2);
    }

    public string Table1 { get; }

    public string Table2 { get; }

    public JoinType Type { get; }

    public IReadOnlyList<JoinConditionsItem> Columns => _columns.AsReadOnly();

    public void Add(string column1, string column2)
    {
        _columns.Add(new JoinConditionsItem(column1, column2));
    }
}