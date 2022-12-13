using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace QuantumZhou.Infrastructure.Data.Database.SqlServer
{
    public class SqlServerExecutor : DbExecutor
    {
        public SqlServerExecutor(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
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

        protected override void CreateTable(IDbConnection con, TableDefine table)
        {
            var count = Count(con, $"SELECT COUNT(*) FROM sysobjects WHERE name='{table.Name}' and xtype='U'");
            if (count == 0)
            {
                base.CreateTable(con, table);
            }
        }

        public override IDbConnection GetConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            return connection;
        }

        protected override DbCommand AppendParams(IDbCommand command, WhereParams whereParams)
        {
            if (command is not SqlCommand cmd)
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }

            foreach (var item in whereParams.Items)
            {
                cmd.Parameters.AddWithValue($"{SqlBuilder.ParamPrefix}{item.Name}", item.Value);
            }

            return cmd;
        }
    }
}
