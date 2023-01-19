using Qz.Infra.Database.Params;
using System.Collections.Generic;

namespace Qz.Infra.Database.Condition
{
    public class WhereConditions
    {
        private readonly List<WhereConditionsItem> _items = new();
        
        public WhereConditions(WhereParamsItem whereParamsItem,
            WhereOperator whereOperator = WhereOperator.Equal,
            WhereConnecter connecter = WhereConnecter.And)
        {
            _items.Add(new WhereConditionsItem(whereParamsItem, whereOperator, connecter));
            WhereParams = new WhereParams(whereParamsItem);
        }

        public WhereConditions(WhereParams whereParams,
            WhereOperator whereOperator = WhereOperator.Equal,
            WhereConnecter connecter = WhereConnecter.And)
        {
            foreach (var item in whereParams.Items)
            {
                _items.Add(new WhereConditionsItem(item, whereOperator, connecter));
            }
            WhereParams = whereParams;
        }

        public IReadOnlyList<WhereConditionsItem> Items => _items.AsReadOnly();

        internal WhereParams WhereParams { get; }

        public void Add(WhereParamsItem whereParamsItem,
            WhereOperator whereOperator = WhereOperator.Equal,
            WhereConnecter connecter = WhereConnecter.And)
        {
            _items.Add(new WhereConditionsItem(whereParamsItem, whereOperator, connecter));
            WhereParams.Add(whereParamsItem);
        }

        public void Add(WhereConditionsIsItem item)
        {
            _items.Add(item);
            // no need add to WhereParams because it's special condition
        }
    }
}
