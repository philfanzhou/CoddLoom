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

        protected internal abstract bool ExistTable(IDbConnection con, TableDefine table);

        protected abstract void AppendParams(IDbCommand command, string paramName, string value);
        
        private DbCommand BuildCommand(IDbConnection con, string sql,
            WhereParams whereParams = null)
        {
            var command = con.CreateCommand();
            command.CommandText = sql;

            if (whereParams != null)
            {
                foreach (var item in whereParams.Items)
                {
                    AppendParams(command, $"{SqlBuilder.ParamPrefix}{item.Name}", item.Value);
                }
            }

            return command as DbCommand;
        }

        #region Transaction

        public void Transaction(Action<DbTransaction> action)
        {
            using var conn = GetConnection();
            try
            {
                conn.Open();
                using var tran = conn.BeginTransaction();
                if (tran is DbTransaction dbTran)
                {
                    try
                    {
                        action(dbTran);
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }

        #endregion

        #region Execute

        public void Execute(Action<IDbConnection> action, IDbConnection con = null)
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

        public T Execute<T>(Func<IDbConnection, T> func, IDbConnection con = null)
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

        public void Execute(DbTransaction tran,
            string sql, WhereParams whereParams = null)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

            using var command = BuildCommand(tran.Connection, sql, whereParams);
            command.Transaction = tran;
            command.ExecuteNonQuery();
        }

        public void Execute(IDbConnection con,
            string sql, WhereParams whereParams = null)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

            using var command = BuildCommand(con, sql, whereParams);
            command.ExecuteNonQuery();
        }

        public void Execute(IDbConnection con, Action<IDataReader> readerAction,
            string sql, WhereParams whereParams = null)
        {
            if (readerAction == null) throw new ArgumentNullException(nameof(readerAction));
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

            using var command = BuildCommand(con, sql, whereParams);
            using var reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                return;
            }

            readerAction(reader);
        }

        #endregion

        #region Read operation
        
        public IEnumerable<T> Select<T>(IDbConnection con, 
            string sql, Func<IDataRecord, T> convertor, WhereParams whereParams = null)
        {
            var result = new List<T>();
            Execute(con, reader =>
            {
                while (reader.Read())
                {
                    result.Add(convertor(reader));
                }
            }, sql, whereParams);
            return result;
        }

        public T First<T>(IDbConnection con,
            string sql, Func<IDataRecord, T> convertor, WhereParams whereParams = null)
        {
            T result = default;
            Execute(con, reader =>
            {
                reader.Read();
                result = convertor(reader);
            }, sql, whereParams);
            return result;
        }

        public int Count(IDbConnection con,
            string sql, WhereParams whereParams = null)
        {
            var count = 0;
            Execute(con, reader =>
            {
                reader.Read();
                count = reader.GetInt32(0);
            }, sql, whereParams);
            return count;
        }

        #endregion
    }
}
