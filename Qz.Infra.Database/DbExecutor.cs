using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Qz.Infra.Database
{
    public abstract class DbExecutor
    {
        protected DbExecutor(string connectionString, IDbConnection connection)
        {
            try
            {
                connection.Open();
                ConnectionString = connectionString;
            }
            finally
            {
                connection.Close();
            }
        }

        public string ConnectionString { get; }

        public virtual SqlBuilder SqlBuilder { get; } = new();

        public abstract IDbConnection GetConnection();

        internal bool ExistTable(IDbConnection con, TableDefine table)
        {
            var builderParam = GetExistTableParam(table);
            if (builderParam == null)
            {
                return false;
            }

            var count = Count(con, SqlBuilder.Count(builderParam), null, builderParam.WhereParams);
            return count > 0;
        }

        protected abstract SqlBuilderCountParam GetExistTableParam(TableDefine table);

        protected abstract Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command);

        private IDbCommand BuildCommand(IDbConnection con, string sql,
            WhereParams whereParams = null)
        {
            var command = con.CreateCommand();
            command.CommandText = sql;

            if (whereParams != null)
            {
                var func = GetAddParameterFunc(command);
                foreach (var item in whereParams.Items)
                {
                    func($"{SqlBuilder.ParamPrefix}{item.Name}", item.Value);
                }
            }

            return command;
        }

        #region Transaction

        public void Transaction(Action<IDbTransaction> action)
        {
            using var conn = GetConnection();
            try
            {
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    action(tran);
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

        #region Execute

        public void Execute(Action<IDbConnection> action, 
            IDbConnection con = null)
        {
            if (con != null)
            {
                action(con);
            }
            else
            {
                using var newConnection = GetConnection();
                try
                {
                    newConnection.Open();
                    action(newConnection);
                }
                finally
                {
                    newConnection.Close();
                }
            }
        }

        public T Execute<T>(Func<IDbConnection, T> func, 
            IDbConnection con = null)
        {
            if (con != null)
            {
                return func(con);
            }
            else
            {
                using var newConnection = GetConnection();
                try
                {
                    newConnection.Open();
                    return func(newConnection);
                }
                finally
                {
                    newConnection.Close();
                }
            }
        }

        #endregion

        #region Execute Sql

        public void Execute(IDbConnection con, string sql,
            IDbTransaction tran = null, WhereParams whereParams = null)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

            using var command = BuildCommand(con, sql, whereParams);
            if (tran != null)
            {
                command.Transaction = tran;
            }

            command.ExecuteNonQuery();
        }

        public void Execute(IDbConnection con, Action<IDataReader> readerAction, string sql,
            IDbTransaction tran = null, WhereParams whereParams = null)
        {
            if (readerAction == null) throw new ArgumentNullException(nameof(readerAction));
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

            using var command = BuildCommand(con, sql, whereParams);
            if (tran != null)
            {
                command.Transaction = tran;
            }

            using var reader = command.ExecuteReader();
            if (reader is not DbDataReader { HasRows: true })
            {
                return;
            }
            readerAction(reader);
        }

        #endregion

        #region Read operation

        public List<T> Select<T>(IDbConnection con, string sql, Func<IDataRecord, T> convertor,
            IDbTransaction tran = null, WhereParams whereParams = null)
        {
            var result = new List<T>();
            Execute(con, reader =>
            {
                while (reader.Read())
                {
                    result.Add(convertor(reader));
                }
            }, sql, tran, whereParams);
            return result;
        }

        public T First<T>(IDbConnection con, string sql, Func<IDataRecord, T> convertor, 
            IDbTransaction tran = null, WhereParams whereParams = null)
        {
            T result = default;
            Execute(con, reader =>
            {
                reader.Read();
                result = convertor(reader);
            }, sql, tran, whereParams);
            return result;
        }

        public int Count(IDbConnection con, string sql,
            IDbTransaction tran = null, WhereParams whereParams = null)
        {
            var count = 0;
            Execute(con, reader =>
            {
                reader.Read();
                count = reader.GetInt32(0);
            }, sql, tran, whereParams);
            return count;
        }

        #endregion
    }
}
