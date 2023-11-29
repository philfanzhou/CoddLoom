using Qz.Infra.Database.Condition.Internal;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;

namespace Qz.Infra.Database.Condition;

public class WhereConditions
{
    private readonly List<WhereConditionsItemBase> _itemList = new();
    private readonly List<ColumnValueParameter> _paramList = new();
    private readonly List<Tuple<WhereConditions, WhereConnecter>> _partialConditions = new();

    internal IEnumerable<WhereConditionsItemBase> Items => _itemList.AsReadOnly();
    internal IEnumerable<Tuple<WhereConditions, WhereConnecter>> PartialItems => _partialConditions.AsReadOnly();

    public IEnumerable<ColumnValueParameter> Parameters => _paramList.AsReadOnly();

    public void Add(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));

        if (IsValidValue(value, whereOperator) == false)
        {
            return;
        }

        var param = CreateAndAddParameter(column, value);
        _itemList.Add(new WhereConditionsItem(param, whereOperator, connecter));
    }

    public void Add(string column, object value, DbType castType,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnecter connecter = WhereConnecter.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));

        if (IsValidValue(value, whereOperator) == false)
        {
            return;
        }

        var param = CreateAndAddParameter(column, value);
        _itemList.Add(new WhereConditionsItem(param, castType, whereOperator, connecter));
    }

    public void AddIsNull(string column, bool isNull,
        WhereConnecter connecter = WhereConnecter.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));

        var conditionItem = new WhereConditionsNullItem(column, isNull, connecter);
        _itemList.Add(conditionItem);
    }

    public void Add(WhereConditions partialCondition,
        WhereConnecter connecter = WhereConnecter.And)
    {
        _partialConditions.Add(new Tuple<WhereConditions, WhereConnecter>(partialCondition, connecter));
    }

    public bool IsEmpty()
    {
        return _itemList.Count < 1 && _partialConditions.Count < 1;
    }

    #region Private method

    private bool IsValidValue(object value, WhereOperator whereOperator)
    {
        if (value == null)
        {
            return false;
        }

        if (whereOperator == WhereOperator.Like
            && string.IsNullOrEmpty(value.ToString()))
        {
            return false;
        }

        return true;
    }

    private ColumnValueParameter CreateAndAddParameter(string column, object value)
    {
        var param = new ColumnValueParameter
        {
            Column = column,
            Value = value,
            ParamName = GetParamName(column)
        };

        _paramList.Add(param);
        return param;
    }

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