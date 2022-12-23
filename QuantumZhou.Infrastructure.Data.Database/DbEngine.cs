using QuantumZhou.Infrastructure.Data.Database.Cache;
using QuantumZhou.Infrastructure.Data.Database.Output;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace QuantumZhou.Infrastructure.Data.Database
{
    public class DbEngine
    {
        public DbEngine(DbExecutor executor, IEnumerable<TableDefine> tables)
        {
            Executor = executor;

            var tableList = tables.ToList();
            TableColumnsCache.Initialize(tableList);
            Executor.Execute(conn =>
            {
                foreach (var table in tableList.Where(table => !Executor.ExistTable(conn, table)))
                {
                    Executor.Execute(conn, Executor.SqlBuilder.GetCreateTableSql(table));
                }
            });
        }

        public DbExecutor Executor { get; }

        public void Insert(SqlBuilderInsertParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.Insert(builderParam);
            Executor.Execute(p => Executor.Execute(p, sql), con);
        }

        public void Insert(SqlBuilderInsertParam builderParam, DbTransaction tran)
        {
            var sql = Executor.SqlBuilder.Insert(builderParam);
            Executor.Execute(tran, sql);
        }

        public void Delete(SqlBuilderDeleteParam builderParam,
            IDbConnection con = null)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Delete(builderParam);
            Executor.Execute(p => Executor.Execute(p, sql, builderParam.WhereParams), con);
        }

        public void Delete(SqlBuilderDeleteParam builderParam, DbTransaction tran)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Delete(builderParam);
            Executor.Execute(tran, sql, builderParam.WhereParams);
        }

        public void Update(SqlBuilderUpdateParam builderParam,
            IDbConnection con = null)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Update(builderParam);
            Executor.Execute(p => Executor.Execute(p, sql, builderParam.WhereParams), con);
        }

        public void Update(SqlBuilderUpdateParam builderParam, DbTransaction tran)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Update(builderParam);
            Executor.Execute(tran, sql, builderParam.WhereParams);
        }

        public int Count(SqlBuilderCountParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.Count(builderParam);
            return Executor.Execute(p => Executor.Count(p, sql, builderParam.WhereParams), con);
        }

        public IEnumerable<T> Select<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.Select(builderParam);
            return Executor.Execute(p => Executor.Select(p, sql, convertor, builderParam.WhereParams), con);
        }

        public T First<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.First(builderParam);
            return Executor.Execute(p => Executor.First(p, sql, convertor, builderParam.WhereParams), con);
        }

        public PageResult<T> PageSelect<T>(Func<IDataRecord, T> convertor,
            PageParam pageParam, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {

            return Executor.Execute(p => GetPageResult(p, convertor, pageParam, builderParam), con);
        }

        #region Private method

        private PageResult<T> GetPageResult<T>(IDbConnection con, 
            Func<IDataRecord, T> convertor, PageParam pageParam, 
            SqlBuilderSelectParam builderParam)
        {
            var sql = Executor.SqlBuilder.Take(pageParam.Offset, pageParam.PageCount, builderParam);
            var items = Executor.Select(con, sql, convertor, builderParam.WhereParams).ToList();
            if (items.Count < 1)
            {
                return new PageResult<T>();
            }

            var totalCount = Count(builderParam, con);
            var totalPages = totalCount / pageParam.PageCount;
            if (Math.Abs(totalCount % pageParam.PageCount) > 0)
            {
                totalPages++;
            }

            return new PageResult<T>(items, pageParam.PageIndex, totalPages, totalCount);
        }

        #endregion
    }
}