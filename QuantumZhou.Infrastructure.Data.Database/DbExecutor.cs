using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace QuantumZhou.Infrastructure.Data.Database
{
    public abstract class DbExecutor
    {
        private readonly string _connectionString;

        protected DbExecutor(string connectionString, IDbConnection connection)
        {
            try
            {
                connection.Open();
                _connectionString = connectionString;
            }
            finally
            {
                connection.Close();
            }
        }

        public virtual SqlBuilder SqlBuilder { get; } = new();

        public IDbConnection GetConnection()
        {
            return GetConnection(_connectionString);
        }

        protected internal abstract bool ExistTable(IDbConnection con, TableDefine table);

        protected abstract IDbConnection GetConnection(string connectionString);

        protected abstract DbCommand AppendParams(IDbCommand command, WhereParams whereParams);
        
        private DbCommand BuildCommand(IDbConnection con, string sql,
            WhereParams whereParams = null)
        {
            var command = con.CreateCommand();
            command.CommandText = sql;

            if (whereParams == null)
            {
                return command as DbCommand;
            }

            return AppendParams(command, whereParams);
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
