using MySql.Data.MySqlClient;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Data;
using System.Data.Common;

namespace QuantumZhou.Infrastructure.Data.Database.MySql
{
    public class MySqlExecutor : DbExecutor
    {
        public MySqlExecutor(string connectionString)
            : base(connectionString, new MySqlConnection(connectionString))
        {
        }

        public MySqlExecutor(string server, string database, string user, string password, uint port = 3306)
            : this(BuildConnectionString(server, database, user, password, port))
        {
        }

        public override SqlBuilder SqlBuilder { get; } = new MySqlBuilder();

        protected override IDbConnection GetConnection(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);
            return connection;
        }

        protected override DbCommand AppendParams(IDbCommand command, WhereParams whereParams)
        {
            if (command is not MySqlCommand cmd)
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }

            foreach (var item in whereParams.Items)
            {
                cmd.Parameters.AddWithValue($"{SqlBuilder.ParamPrefix}{item.Name}", item.Value);
            }

            return cmd;
        }

        protected override bool ExistTable(IDbConnection con, TableDefine table)
        {
            // use create table sql to check exist, not here
            return false;
        }

        private static string BuildConnectionString(string server, string database,
            string user, string password, uint port)
        {
            var connStrBuilder = new MySqlConnectionStringBuilder
            {
                Server = server,
                UserID = user,
                Password = password,
                Port = port
            };

            var checkDbSql = $"CREATE DATABASE IF NOT EXISTS {database}";
            using var connection = new MySqlConnection(connStrBuilder.ConnectionString);
            try
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = checkDbSql;
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }

            connStrBuilder.Database = database;
            return connStrBuilder.ConnectionString;
        }
    }
}
