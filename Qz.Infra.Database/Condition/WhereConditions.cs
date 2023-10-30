using Qz.Infra.Database.Params;
using System.Collections.Generic;

namespace Qz.Infra.Database.Condition;

public class WhereConditions
{
    private readonly List<WhereConditionsItemBase> _items = new();
    private readonly Dictionary<string, int> _paramNameIndex = new();
    
    internal IReadOnlyList<WhereConditionsItemBase> Items => _items.AsReadOnly();

    internal WhereParams WhereParams { get; private set; }

    public IEnumerable<ISqlParameter> Parameters => WhereParams?.Items;

    public void Add(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        var whereParamsItem = new WhereParamsItem(column, value);
        if (WhereParams == null)
        {
            WhereParams = new WhereParams(whereParamsItem);
        }
        else
        {
            WhereParams.Add(whereParamsItem);
        }
        Add(new WhereConditionsItem(whereParamsItem, whereOperator, connecter));
    }

    public void AddIsNull(string column, bool isNull,
        WhereConnecter whereConnecter = WhereConnecter.And)
    {
        var nullCondition = new WhereConditionsNullItem(column, isNull, whereConnecter);
        _items.Add(nullCondition);
        // no need add to WhereParams because it's special condition
    }

    #region Private method
    private void Add(WhereConditionsItem item)
    {
        if (!_paramNameIndex.ContainsKey(item.Param.ParamName))
        {
            _paramNameIndex.Add(item.Param.ParamName, 0);
        }
        else
        {
            _paramNameIndex[item.Param.ParamName] += 1;
        }

        var index = _paramNameIndex[item.Param.ParamName];
        if (index > 0)
        {
            item.Param.ParamName = $"{item.Param.ParamName}{index}";
        }

        _items.Add(item);
    }
    #endregion
}