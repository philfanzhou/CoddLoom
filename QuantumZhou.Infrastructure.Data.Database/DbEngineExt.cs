using QuantumZhou.Infrastructure.Data.Database.Cache;
using QuantumZhou.Infrastructure.Data.Database.Convert;
using QuantumZhou.Infrastructure.Data.Database.Output;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using System;
using System.Collections.Generic;
using System.Data;

namespace QuantumZhou.Infrastructure.Data.Database
{
    public partial class DbEngine
    {
        public void Insert<T>(T entity,
            IDbConnection con = null)
        {
            Insert(DbConverter.ToInsert(entity), con);
        }

        public void Delete<T>(string keyValue,
            IDbConnection con = null)
        {
            var whereParams = GetKeyWhereParam<T>(keyValue);
            Delete(new SqlBuilderDeleteParam<T>(whereParams), con);
        }

        public void Update<T>(T entity,
            IDbConnection con = null)
        {
            Update(DbConverter.ToUpdate(entity), con);
        }

        public bool Exist(SqlBuilderCountParam builderParam,
            IDbConnection con = null)
        {
            return Count(builderParam, con) > 0;
        }

        public IEnumerable<T> Select<T>(SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
            where T : new()
        {
            return Select(DbConverter.ToEntity<T>, builderParam, con);
        }

        public IEnumerable<T> Select<T>(
            IDbConnection con = null)
            where T : new()
        {
            var entityMap = EntityMapCache.Get<T>();
            var builderParam = new SqlBuilderSelectParam(entityMap.Table.Name);
            return Select<T>(builderParam, con);
        }

        public IEnumerable<T> Select<T>(string keyValue,
            IDbConnection con = null)
            where T : new()
        {
            var whereParams = GetKeyWhereParam<T>(keyValue);
            var builderParam = new SqlBuilderSelectParam<T>(whereParams);
            return Select<T>(builderParam, con);
        }

        public T First<T>(SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
            where T : new()
        {
            return First(DbConverter.ToEntity<T>, builderParam, con);
        }

        public PageResult<T> PageSelect<T>(PageParam pageParam, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
            where T : new()
        {
            return PageSelect(DbConverter.ToEntity<T>, pageParam, builderParam, con);
        }

        private static WhereParams GetKeyWhereParam<T>(string keyValue)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                throw new ArgumentNullException(nameof(keyValue));
            }
            var entityMap = EntityMapCache.Get<T>();
            if (string.IsNullOrEmpty(entityMap.PrimaryKey))
            {
                throw new ArgumentException($"{nameof(T)} does not have a primary key");
            }

            var whereParams = new WhereParams(entityMap.PrimaryKey, keyValue);
            return whereParams;
        }
    }
}
