using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database
{
    public class DbEngine
    {
        public DbEngine(DbExecutor executor, IEnumerable<TableDefine> tables)
        {
            Executor = executor;

            var tableList = tables.ToList();
            TableColumnsCache.Initialize(tableList);
            Executor.Execute(con =>
            {
                foreach (var table in tableList.Where(table => !Executor.ExistTable(table, con)))
                {
                    Executor.Execute(Executor.SqlBuilder.GetCreateTableSql(table), null, null, con);
                }
            });
        }

        public DbExecutor Executor { get; }

        public void Insert(SqlBuilderInsertParam builderParam,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            var sql = Executor.SqlBuilder.Insert(builderParam);
            Executor.Execute(sql, null, null, con, tran);
        }

        public void Delete(SqlBuilderDeleteParam builderParam,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Delete(builderParam);
            Executor.Execute(sql, builderParam.WhereParams, null, con, tran);
        }

        public void Update(SqlBuilderUpdateParam builderParam,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Update(builderParam);
            Executor.Execute(sql, builderParam.WhereParams, null, con, tran);
        }

        public int Count(SqlBuilderCountParam builderParam,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            var sql = Executor.SqlBuilder.Count(builderParam);
            return Executor.Count(sql, builderParam.WhereParams, con, tran);
        }

        public List<T> Select<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            var sql = Executor.SqlBuilder.Select(builderParam);
            return Executor.Select(sql, convertor, builderParam.WhereParams, con, tran);
        }

        public T First<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            var sql = Executor.SqlBuilder.First(builderParam);
            return Executor.First(sql, convertor, builderParam.WhereParams, con, tran);
        }

        public List<T> PageSelect<T>(Func<IDataRecord, T> convertor,
            PageParam pageParam, SqlBuilderSelectParam builderParam, out int totalPages, out int totalCount,
            IDbConnection con = null, IDbTransaction tran = null)
        {
            totalCount = 0;
            totalPages = 0;

            var sql = Executor.SqlBuilder.Take(pageParam.Offset, pageParam.PageCount, builderParam);
            var items = Executor.Select(sql, convertor, builderParam.WhereParams, con, tran).ToList();
            
            if (items.Count <= 1)
            {
                return items;
            }

            totalCount = Count(builderParam, con, tran);
            totalPages = totalCount / pageParam.PageCount;
            if (Math.Abs(totalCount % pageParam.PageCount) > 0)
            {
                totalPages++;
            }

            return items;
        }
    }
}