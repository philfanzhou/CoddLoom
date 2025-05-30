using Qz.Infra.Database.Cache;
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
    #region Field

    private readonly ParameterNameGenerator _parameterNameGenerator = new();

    private readonly List<PartialWhereConditions> _partialConditions = [];

    private readonly List<ValueParam> _valueParamList = [];
    private readonly List<WhereConditionsItemBase> _conditionsItemList = [];

    #endregion

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

    #endregion

    public static WhereConditions Create<TEntity>(object id, 
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
            ? new WhereConditionsItem(param, castType.Value, whereOperator, connector)
            : new WhereConditionsItem(param, whereOperator, connector));
        return this;
    }

    [Obsolete]
    public void AddIsNull(string column, 
        WhereConnector connector = WhereConnector.And, bool isNull = true)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));

        var conditionItem = new WhereConditionsNullItem(column, isNull, connector);
        _conditionsItemList.Add(conditionItem);
    }

    public WhereConditions IsNull(string column,
        WhereConnector connector = WhereConnector.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        var conditionItem = new WhereConditionsNullItem(column, true, connector);
        _conditionsItemList.Add(conditionItem);
        return this;
    }

    [Obsolete]
    public void AddIsNotNull(string column, 
        WhereConnector connector = WhereConnector.And)
    {
        AddIsNull(column, connector, false);
    }

    public WhereConditions IsNotNull(string column,
        WhereConnector connector = WhereConnector.And)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        var conditionItem = new WhereConditionsNullItem(column, false, connector);
        _conditionsItemList.Add(conditionItem);
        return this;
    }

    public WhereConditions Add(WhereConditions where,
        WhereConnector connector = WhereConnector.And)
    {
        RefreshParamName(where);
        _partialConditions.Add(new PartialWhereConditions
        {
            WhereConditions = where,
            WhereConnector = connector
        });
        return this;
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
            whereBuilder.Append(item.ToSql(builder));
        }

        foreach (var partialItem in _partialConditions)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(builder.GetConnector(partialItem.WhereConnector));
            }
            whereBuilder.Append(builder.GetPartialCondition(partialItem.WhereConditions));
        }

        return whereBuilder.ToString();
    }

    private void RefreshParamName(WhereConditions where)
    {
        foreach (var item in where._conditionsItemList)
        {
            if (item is WhereConditionsItem condition)
            {
                condition.Parameter.ParamName = _parameterNameGenerator.Get(condition.Column);
            }
        }

        foreach(var item in where._partialConditions)
        {
            RefreshParamName(item.WhereConditions);
        }
    }

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