using Qz.Infra.Database.Condition.Internal;
using Qz.Infra.Database.Params;
using System.Collections.Generic;

namespace Qz.Infra.Database.Condition;

public class WhereConditions
{
    private readonly List<WhereConditionsItemBase> _itemList = new();
    private readonly List<ColumnValueParameter> _paramList = new();
    
    internal IEnumerable<WhereConditionsItemBase> Items => _itemList.AsReadOnly();

    public IEnumerable<ColumnValueParameter> Parameters => _paramList.AsReadOnly();

    public void Add(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        if(string.IsNullOrEmpty(column)) throw new System.ArgumentNullException(nameof(column));

        if (value == null)
        {
            return;
        }

        var param = new ColumnValueParameter
        {
            Column = column,
            Value = value,
            ParamName = GetParamName(column)
        };

        _itemList.Add(new WhereConditionsItem(param, whereOperator, connecter));
        _paramList.Add(param);
    }

    public void AddIsNull(string column, bool isNull,
        WhereConnecter connecter = WhereConnecter.And)
    {
        if (string.IsNullOrEmpty(column)) throw new System.ArgumentNullException(nameof(column));

        var conditionItem = new WhereConditionsNullItem(column, isNull, connecter);
        _itemList.Add(conditionItem);
    }

    public bool IsEmpty()
    {
        return _itemList.Count < 1;
    }

    #region Private method
    private readonly Dictionary<string, int> _paramNameIndex = new();
    private string GetParamName(string paramName)
    {
        if (!_paramNameIndex.ContainsKey(paramName))
        {
            _paramNameIndex.Add(paramName, 0);
        }
        else
        {
            _paramNameIndex[paramName] += 1;
        }

        return $"{paramName}{_paramNameIndex[paramName]}";
    }
    #endregion
}