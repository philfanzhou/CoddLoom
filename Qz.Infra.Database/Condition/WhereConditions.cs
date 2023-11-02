using Qz.Infra.Database.Condition.Internal;
using Qz.Infra.Database.Params;
using System.Collections.Generic;

namespace Qz.Infra.Database.Condition;

public class WhereConditions
{
    private readonly List<WhereConditionsItemBase> _items = new();
    private readonly List<ColumnValueParameter> _whereParams = new();
    
    internal IEnumerable<WhereConditionsItemBase> Items => _items.AsReadOnly();
    
    public IEnumerable<ColumnValueParameter> Parameters => _whereParams.AsReadOnly();

    public void Add(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        var whereParamsItem = new ColumnValueParameter
        {
            Column = column,
            Value = value,
            ParamName = column
        };
        _whereParams.Add(whereParamsItem);
        Add(new WhereConditionsItem(whereParamsItem, whereOperator, connecter));
    }

    public void AddIsNull(string column, bool isNull,
        WhereConnecter whereConnecter = WhereConnecter.And)
    {
        var nullCondition = new WhereConditionsNullItem(column, isNull, whereConnecter);
        _items.Add(nullCondition);
        // no need add to WhereParams because it's special condition
    }

    internal bool IsEmpty()
    {
        return _items.Count < 1;
    }

    #region Private method
    private readonly Dictionary<string, int> _paramNameIndex = new();
    private void Add(WhereConditionsItem item)
    {
        if (!_paramNameIndex.ContainsKey(item.Parameter.ParamName))
        {
            _paramNameIndex.Add(item.Parameter.ParamName, 0);
        }
        else
        {
            _paramNameIndex[item.Parameter.ParamName] += 1;
        }

        var index = _paramNameIndex[item.Parameter.ParamName];
        if (index > 0)
        {
            item.Parameter.ParamName = $"{item.Parameter.ParamName}{index}";
        }

        _items.Add(item);
    }
    #endregion
}