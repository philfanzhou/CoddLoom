using Qz.Infra.Database.Condition;
using System;
using System.Text;

namespace Qz.Infra.Database.Sql;

partial class SqlBuilder
{
    protected internal virtual string GetJoinTable(JoinConditions joinCondition)
    {
        if (joinCondition?.Columns == null
            || joinCondition.Columns.Count < 1
            || string.IsNullOrEmpty(joinCondition.Table1)
            || string.IsNullOrEmpty(joinCondition.Table2))
        {
            throw new ArgumentOutOfRangeException(nameof(joinCondition));
        }

        var columnBuilder = new StringBuilder();
        foreach (var item in joinCondition.Columns)
        {
            if (columnBuilder.Length > 1)
            {
                columnBuilder.Append(" AND ");
            }

            columnBuilder.Append($"{joinCondition.Table1}.{item.Column1} = {joinCondition.Table2}.{item.Column2}");
        }

        var joinType = joinCondition.Type.ToString().ToUpper();
        return $"{joinCondition.Table1} {joinType} JOIN {joinCondition.Table2} ON {columnBuilder}";
    }
}