using Qz.Infra.Database.Params;
using System.Collections.Generic;

namespace Qz.Infra.Database.Condition;

public class WhereConditions
{
    private readonly List<WhereConditionsItemBase> _items = new();
    private readonly Dictionary<string, int> _paramNameIndex = new();

    public WhereConditions(WhereConditionsNullItem item)
    {
        Add(item);
    }

    public WhereConditions(WhereParamsItem whereParamsItem,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        WhereParams = new WhereParams(whereParamsItem);
        _items.Add(new WhereConditionsItem(whereParamsItem, whereOperator, connecter));
    }

    public WhereConditions(WhereParams whereParams,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        WhereParams = whereParams;
        foreach (var item in whereParams.Items)
        {
            Add(new WhereConditionsItem(item, whereOperator, connecter));
        }
    }

    public IReadOnlyList<WhereConditionsItemBase> Items => _items.AsReadOnly();

    internal WhereParams WhereParams { get; }

    public void Add(WhereParamsItem whereParamsItem,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        WhereParams.Add(whereParamsItem);
        Add(new WhereConditionsItem(whereParamsItem, whereOperator, connecter));
    }

    public void Add(WhereConditionsNullItem nullItem)
    {
        _items.Add(nullItem);
        // no need add to WhereParams because it's special condition
    }

    private void Add(WhereConditionsItem item)
    {
        if (!_paramNameIndex.ContainsKey(item.Param.Name))
        {
            _paramNameIndex.Add(item.Param.Name, 0);
        }
        else
        {
            _paramNameIndex[item.Param.Name] += 1;
        }

        var index = _paramNameIndex[item.Param.Name];
        if (index > 0)
        {

            item.Param.Name = $"{item.Param.Name}{index}";
        }

        _items.Add(item);
    }
}