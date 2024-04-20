using Qz.Infra.Database.Condition.Internal;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Qz.Infra.Database.Condition;

public class WhereConditions
{
    private readonly ParameterNameGenerator _parameterNameGenerator = new();
    private readonly List<ColumnValueParameter> _valueParamList = new();
    private readonly List<WhereConditionsItemBase> _conditionsItemList = new();
    private readonly List<PartialWhereConditions> _partialConditions = new();

    #region Property

    public IEnumerable<ColumnValueParameter> Parameters
    {
        get
        {
            var paramList = new List<ColumnValueParameter>();
            paramList.AddRange(_valueParamList);

            foreach (var condition in _partialConditions)
            {
                paramList.AddRange(condition.WhereConditions.Parameters);
            }

            return paramList;
        }
    }

    #endregion

    public void Add(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal, WhereConnector connector = WhereConnector.And, bool allowEmptyValue = false)
    {
        Add(column, value, whereOperator, connector, null, allowEmptyValue);
    }

    public void Add(string column, object value, DbType castType,
        WhereOperator whereOperator = WhereOperator.Equal, WhereConnector connector = WhereConnector.And, bool allowEmptyValue = false)
    {
        Add(column, value, whereOperator, connector, castType, allowEmptyValue);
    }

    public void AddIsNull(string column, 
        bool isNull = true, WhereConnector connector = WhereConnector.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));

        var conditionItem = new WhereConditionsNullItem(column, isNull, connector);
        _conditionsItemList.Add(conditionItem);
    }

    public void Add(WhereConditions partialCondition,
        WhereConnector connector = WhereConnector.And)
    {
        foreach (var item in partialCondition._conditionsItemList)
        {
            if (item is WhereConditionsItem condition)
            {
                condition.Parameter.ParamName = _parameterNameGenerator.Get(condition.Parameter.ParamName);
            }
        }

        _partialConditions.Add(new PartialWhereConditions
        {
            WhereConditions = partialCondition,
            WhereConnector = connector
        });
    }

    public bool IsEmpty()
    {
        return _conditionsItemList.Count < 1 && _partialConditions.Count < 1;
    }

    internal string GetWhereString(SqlBuilder builder)
    {
        var whereBuilder = new StringBuilder();
        foreach (var item in _conditionsItemList)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(builder.GetConnector(item.WhereConnector));
            }
            whereBuilder.Append(item.GetWhereString(builder));
        }

        foreach (var partialItem in _partialConditions)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(builder.GetConnector(partialItem.WhereConnector));
            }
            whereBuilder.Append(builder.GetNestedWhere(partialItem.WhereConditions.GetWhereString(builder)));
        }

        return whereBuilder.ToString();
    }

    #region Private method

    private void Add(string column, object value, WhereOperator whereOperator, WhereConnector connector,
        DbType? castType = null, bool allowEmptyValue = false)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        if (value == null) return; // use AddIsNull condition to instead null value.
        if (!allowEmptyValue && string.IsNullOrEmpty(value.ToString())) return;

        var param = new ColumnValueParameter
        {
            Column = column,
            Value = value,
            ParamName = _parameterNameGenerator.Get(column)
        };

        _valueParamList.Add(param);
        _conditionsItemList.Add(castType != null
            ? new WhereConditionsItem(param, castType.Value, whereOperator, connector)
            : new WhereConditionsItem(param, whereOperator, connector));
    }

    #endregion

    private class ParameterNameGenerator
    {
        private readonly Dictionary<string, int> _paramNameIndex = new();

        public string Get(string paramName)
        {
            if (paramName.Contains("."))
            {
                paramName = paramName.Replace('.', '_');
            }

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
    }
}