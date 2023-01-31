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

        public void Insert(SqlBuilderInsertParam builderParam, IDbTransaction tran)
        {
            var sql = Executor.SqlBuilder.Insert(builderParam);
            Executor.Execute(tran.Connection, sql, tran);
        }

        public void Delete(SqlBuilderDeleteParam builderParam,
            IDbConnection con = null)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Delete(builderParam);
            Executor.Execute(p => Executor.Execute(p, sql, null, builderParam.WhereParams), con);
        }

        public void Delete(SqlBuilderDeleteParam builderParam, IDbTransaction tran)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Delete(builderParam);
            Executor.Execute(tran.Connection, sql, tran, builderParam.WhereParams);
        }

        public void Update(SqlBuilderUpdateParam builderParam,
            IDbConnection con = null)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Update(builderParam);
            Executor.Execute(p => Executor.Execute(p, sql, null, builderParam.WhereParams), con);
        }

        public void Update(SqlBuilderUpdateParam builderParam, IDbTransaction tran)
        {
            if (builderParam.WhereParams == null) throw new ArgumentNullException(nameof(builderParam.WhereParams));
            var sql = Executor.SqlBuilder.Update(builderParam);
            Executor.Execute(tran.Connection, sql, tran, builderParam.WhereParams);
        }

        public int Count(SqlBuilderCountParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.Count(builderParam);
            return Executor.Execute(p => Executor.Count(p, sql, null, builderParam.WhereParams), con);
        }

        public int Count(SqlBuilderCountParam builderParam, IDbTransaction tran)
        {
            var sql = Executor.SqlBuilder.Count(builderParam);
            return Executor.Count(tran.Connection, sql, tran, builderParam.WhereParams);
        }

        public List<T> Select<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.Select(builderParam);
            return Executor.Execute(p => Executor.Select(p, sql, convertor, null, builderParam.WhereParams), con);
        }

        public T First<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            var sql = Executor.SqlBuilder.First(builderParam);
            return Executor.Execute(p => Executor.First(p, sql, convertor, null,builderParam.WhereParams), con);
        }

        public List<T> PageSelect<T>(Func<IDataRecord, T> convertor,
            PageParam pageParam, SqlBuilderSelectParam builderParam, out int totalPages, out int totalCount,
            IDbConnection con = null)
        {
            if (con != null)
            {
                return GetPageResult(con, convertor, pageParam, builderParam, out totalPages, out totalCount);
            }
            else
            {
                using var newConnection = Executor.GetConnection();
                try
                {
                    newConnection.Open();
                    return GetPageResult(newConnection, convertor, pageParam, builderParam, out totalPages,
                        out totalCount);
                }
                finally
                {
                    newConnection.Close();
                }
            }
        }

        #region Private method

        private List<T> GetPageResult<T>(IDbConnection con,
            Func<IDataRecord, T> convertor, PageParam pageParam,
            SqlBuilderSelectParam builderParam, out int totalPages, out int totalCount)
        {
            totalCount = 0;
            totalPages = 0;

            var sql = Executor.SqlBuilder.Take(pageParam.Offset, pageParam.PageCount, builderParam);
            var items = Executor.Select(con, sql, convertor, null, builderParam.WhereParams).ToList();
            if (items.Count > 1)
            {
                totalCount = Count(builderParam, con);
                totalPages = totalCount / pageParam.PageCount;
                if (Math.Abs(totalCount % pageParam.PageCount) > 0)
                {
                    totalPages++;
                }
            }

            return items;
        }

        #endregion
    }
}