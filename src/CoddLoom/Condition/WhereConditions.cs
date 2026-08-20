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

    private readonly List<WhereItemBase> _conditionsItemList = [];

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
            var parameters = new List<ValueParam>();
            CollectParameters(this, parameters, new HashSet<ValueParam>());
            RefreshParamNames(parameters);
            return parameters.AsReadOnly();
        }
    }

    internal IEnumerable<WhereItemBase> Items => _conditionsItemList.AsReadOnly();

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
        return _conditionsItemList.Count < 1;
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

        if (ReferenceEquals(where, this) || Contains(where, this))
        {
            throw new ArgumentException("Nested where conditions cannot contain their parent.", nameof(where));
        }

        _conditionsItemList.Add(new PartialWhereConditions(where, connector));
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

        _conditionsItemList.Add(new WhereConditionsInItem(column, paramList, connector));
        return this;
    }

    internal void RefreshParamNames()
    {
        var parameters = new List<ValueParam>();
        CollectParameters(this, parameters, new HashSet<ValueParam>());
        RefreshParamNames(parameters);
    }

    private static void RefreshParamNames(IEnumerable<ValueParam> parameters)
    {
        var nameGenerator = new ParameterNameGenerator();
        foreach (var parameter in parameters)
        {
            parameter.ParamName = nameGenerator.Get(parameter.Column);
        }
    }

    private static void CollectParameters(WhereConditions where, ICollection<ValueParam> parameters,
        ISet<ValueParam> visited)
    {
        foreach (var item in where._conditionsItemList)
        {
            switch (item)
            {
                case WhereConditionsNormalItem normalItem:
                    AddParameter(normalItem.Parameter, parameters, visited);
                    break;
                case WhereConditionsInItem inItem:
                    foreach (var parameter in inItem.Parameters)
                    {
                        AddParameter(parameter, parameters, visited);
                    }
                    break;
                case PartialWhereConditions partialItem:
                    CollectParameters(partialItem.WhereConditions, parameters, visited);
                    break;
            }
        }
    }

    private static void AddParameter(ValueParam parameter, ICollection<ValueParam> parameters,
        ISet<ValueParam> visited)
    {
        if (visited.Add(parameter))
        {
            parameters.Add(parameter);
        }
    }

    private static bool Contains(WhereConditions where, WhereConditions target)
    {
        if (ReferenceEquals(where, target))
        {
            return true;
        }

        return where._conditionsItemList
            .OfType<PartialWhereConditions>()
            .Any(item => Contains(item.WhereConditions, target));
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
