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
        public string ConnectionString { get; protected set; }

        public virtual SqlBuilder SqlBuilder { get; } = new();

        public abstract IDbConnection GetConnection();

        public virtual DbCommand BuildCommand(IDbConnection con, string sql,
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

        protected abstract DbCommand AppendParams(IDbCommand command, WhereParams whereParams);

        protected internal virtual void CreateTable(IDbConnection con, TableDefine table)
        {
            Execute(con, SqlBuilder.GetCreateTableSql(table));
        }

        #region Execute Sql

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

        public void Insert(IDbConnection con, string sql)
        {
            Execute(con, sql);
        }

        public void Delete(IDbConnection con,
            string sql, WhereParams whereParams)
        {
            if (whereParams == null) throw new ArgumentNullException(nameof(whereParams));
            Execute(con, sql, whereParams);
        }

        public void Update(IDbConnection con,
            string sql, WhereParams whereParams)
        {
            if (whereParams == null) throw new ArgumentNullException(nameof(whereParams));
            Execute(con, sql, whereParams);
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

        public IEnumerable<T> Select<T>(IDbConnection con, Func<IDataRecord, T> convertor,
            string sql, WhereParams whereParams = null)
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

        public T First<T>(IDbConnection con, Func<IDataRecord, T> convertor,
            string sql, WhereParams whereParams = null)
        {
            T result = default;
            Execute(con, reader =>
            {
                reader.Read();
                result = convertor(reader);
            }, sql, whereParams);
            return result;
        }

        #endregion
    }
}
