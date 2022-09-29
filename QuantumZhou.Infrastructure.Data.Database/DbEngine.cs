using QuantumZhou.Infrastructure.Data.Database.Output;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using QuantumZhou.Infrastructure.Data.Database.Cache;

namespace QuantumZhou.Infrastructure.Data.Database
{
    public partial class DbEngine
    {
        public DbEngine(DbExecutor executor, IEnumerable<TableDefine> tables)
        {
            Executor = executor;

            var tableList = tables.ToList();
            TableColumnsCache.Initialize(tableList);
            Execute(conn =>
            {
                foreach (var table in tableList)
                {
                    Executor.CreateTable(conn, table);
                }
            });
        }

        public DbExecutor Executor { get; }

        public SqlBuilder SqlBuilder => Executor.SqlBuilder;

        #region Execute

        public void Execute(Action<IDbConnection> action)
        {
            using var conn = Executor.GetConnection();
            try
            {
                conn.Open();
                action(conn);
            }
            finally
            {
                conn.Close();
            }
        }

        public T Execute<T>(Func<IDbConnection, T> func)
        {
            using var conn = Executor.GetConnection();
            try
            {
                conn.Open();
                return func(conn);
            }
            finally
            {
                conn.Close();
            }
        }

        #endregion

        #region Transaction

        public void Transaction(Action<IDbConnection> action)
        {
            using var conn = Executor.GetConnection();
            try
            {
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    action(tran.Connection);
                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            finally
            {
                conn.Close();
            }
        }

        #endregion

        public void Insert(SqlBuilderInsertParam builderParam,
            IDbConnection con = null)
        {
            var sql = SqlBuilder.Insert(builderParam);
            if (con != null)
            {
                Executor.Insert(con, sql);
            }
            else
            {
                Execute(conn => Executor.Insert(conn, sql));
            }
        }

        public void Delete(SqlBuilderDeleteParam builderParam,
            IDbConnection con = null)
        {
            var sql = SqlBuilder.Delete(builderParam);
            if (con != null)
            {
                Executor.Delete(con, sql, builderParam.WhereParams);
            }
            else
            {
                Execute(conn => Executor.Delete(conn, sql, builderParam.WhereParams));
            }
        }

        public void Update(SqlBuilderUpdateParam builderParam,
            IDbConnection con = null)
        {
            var sql = SqlBuilder.Update(builderParam);
            if (con != null)
            {
                Executor.Update(con, sql, builderParam.WhereParams);
            }
            else
            {
                Execute(conn => Executor.Update(conn, sql, builderParam.WhereParams));
            }
        }

        public int Count(SqlBuilderCountParam builderParam,
            IDbConnection con = null)
        {
            var sql = SqlBuilder.Count(builderParam);
            return con != null
                ? Executor.Count(con, sql, builderParam.WhereParams)
                : Execute(conn => Executor.Count(conn, sql, builderParam.WhereParams));
        }

        public IEnumerable<T> Select<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            var sql = SqlBuilder.Select(builderParam);
            return con != null
                ? Executor.Select(con, convertor, sql, builderParam.WhereParams)
                : Execute(conn => Executor.Select(conn, convertor, sql, builderParam.WhereParams));
        }

        public T First<T>(Func<IDataRecord, T> convertor, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            var sql = SqlBuilder.First(builderParam);
            return con != null 
                ? Executor.First(con, convertor, sql, builderParam.WhereParams)
                : Execute(conn => Executor.First(conn, convertor, sql, builderParam.WhereParams));
        }

        public PageResult<T> PageSelect<T>(Func<IDataRecord, T> convertor,
            PageParam pageParam, SqlBuilderSelectParam builderParam,
            IDbConnection con = null)
        {
            return con != null
                ? GetPageResult(con, convertor, pageParam, builderParam)
                : Execute(conn => GetPageResult(conn, convertor, pageParam, builderParam));
        }

        private PageResult<T> GetPageResult<T>(IDbConnection con, 
            Func<IDataRecord, T> convertor, PageParam pageParam, 
            SqlBuilderSelectParam builderParam)
        {
            var sql = SqlBuilder.Take(pageParam.Offset, pageParam.PageCount, builderParam);
            var items = Executor.Select(con, convertor, sql, builderParam.WhereParams).ToList();
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
    }
}