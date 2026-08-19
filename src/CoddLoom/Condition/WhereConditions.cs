using CoddLoom.Cache;
using CoddLoom.Condition.Internal;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoddLoom.Condition;

public class WhereConditions
{
    #region Field

    private readonly ParameterNameGenerator _parameterNameGenerator = new();

    private readonly List<ValueParam> _valueParamList = [];
    private readonly List<WhereItemBase> _conditionsItemList = [];
    private readonly List<PartialWhereConditions> _partialConditions = [];

    #endregion

    public WhereConditions() { }

    public WhereConditions(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal,
        WhereConnector connector = WhereConnector.And,
        bool allowEmptyValue = false)
    {
        Add(column, value, whereOperator, connector, allowEmptyValue);
    }

    #region Property

    public IEnumerable<ValueParam> Parameters
    {
        get
        {
            var paramList = new List<ValueParam>();
            paramList.AddRange(_valueParamList);

            foreach (var condition in _partialConditions)
            {
                paramList.AddRange(condition.WhereConditions.Parameters);
            }

            return paramList;
        }
    }

    internal IEnumerable<WhereItemBase> Items => _conditionsItemList.AsReadOnly();

    internal IEnumerable<PartialWhereConditions> InnerConditions => _partialConditions.AsReadOnly();

    #endregion

    public static WhereConditions ById<TEntity>(object id, 
        out string tableName)
    {
        if (id is null 
            || (id is string strId && string.IsNullOrEmpty(strId)))
        {
            throw new ArgumentNullException(nameof(id));
        }

        var entityMap = EntityMapCache.Get<TEntity>();
        if (string.IsNullOrEmpty(entityMap.PrimaryKey))
        {
            throw new ArgumentException($"{nameof(TEntity)} does not have a primary key");
        }
        tableName = entityMap.Table.Name;
        var where = new WhereConditions();

        where.Add(entityMap.PrimaryKey, id);
        return where;
    }

    public bool IsEmpty()
    {
        return _conditionsItemList.Count < 1 && _partialConditions.Count < 1;
    }

    public WhereConditions Add(string column, object value,
        WhereOperator whereOperator = WhereOperator.Equal, 
        WhereConnector connector = WhereConnector.And, 
        bool allowEmptyValue = false)
    {
        return Add(column, value, null, whereOperator, connector, allowEmptyValue);
    }

    public WhereConditions Add(string column, object value, DbType? castType,
        WhereOperator whereOperator = WhereOperator.Equal, 
        WhereConnector connector = WhereConnector.And, 
        bool allowEmptyValue = false)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        if (value == null) return this; // use AddIsNull condition to instead null value.
        if (allowEmptyValue == false && string.IsNullOrEmpty(value.ToString())) return this;

        var param = new ValueParam(column, value, _parameterNameGenerator.Get(column));
        _valueParamList.Add(param);
        _conditionsItemList.Add(castType != null
            ? new WhereConditionsNormalItem(param, castType.Value, whereOperator, connector)
            : new WhereConditionsNormalItem(param, whereOperator, connector));
        return this;
    }

    public WhereConditions Add(WhereConditions where,
        WhereConnector connector = WhereConnector.And)
    {
        if (where == null || where.IsEmpty())
        {
            return this;
        }

        RefreshParamName(where, _parameterNameGenerator);
        _partialConditions.Add(new PartialWhereConditions
        {
            WhereConditions = where,
            WhereConnector = connector
        });
        return this;
    }

    public WhereConditions IsNull(string column,
        WhereConnector connector = WhereConnector.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        var conditionItem = new WhereConditionsNullItem(column, true, connector);
        _conditionsItemList.Add(conditionItem);
        return this;
    }

    public WhereConditions IsNotNull(string column,
        WhereConnector connector = WhereConnector.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        var conditionItem = new WhereConditionsNullItem(column, false, connector);
        _conditionsItemList.Add(conditionItem);
        return this;
    }

    public WhereConditions In<T>(string column, IEnumerable<T> ranges,
        WhereConnector connector = WhereConnector.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        if (ranges == null) return this;
        var valueList = ranges.Where(v => v != null).ToList();
        if(valueList.Count < 1) return this;

        var paramList = valueList
            .Select(v => new ValueParam(column, v, _parameterNameGenerator.Get(column)))
            .ToList();

        _valueParamList.AddRange(paramList);
        _conditionsItemList.Add(new WhereConditionsInItem(column, paramList, connector));
        return this;
    }

    private static void RefreshParamName(WhereConditions where, ParameterNameGenerator nameGenerator)
    {
        // Rename the values rather than only normal-condition items. IN conditions
        // keep their own ValueParam list and previously retained colliding names
        // when nested under a condition using the same column.
        foreach (var parameter in where._valueParamList)
        {
            parameter.ParamName = nameGenerator.Get(parameter.Column);
        }

        foreach(var item in where._partialConditions)
        {
            RefreshParamName(item.WhereConditions, nameGenerator);
        }
    }

    #region Obsolete

    [Obsolete]
    public void AddIsNull(string column,
        WhereConnector connector = WhereConnector.And, bool isNull = true)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));

        var conditionItem = new WhereConditionsNullItem(column, isNull, connector);
        _conditionsItemList.Add(conditionItem);
    }

    [Obsolete]
    public void AddIsNotNull(string column,
        WhereConnector connector = WhereConnector.And)
    {
        AddIsNull(column, connector, false);
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

public enum WhereConnector
{
    And,
    Or
}

public enum WhereOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterEqual,
    LessThan,
    LessEqual,
    Like
}
