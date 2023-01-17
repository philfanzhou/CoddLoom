using QuantumZhou.Infrastructure.Data.Database.Sql;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Data;
using System.Data.SqlClient;

namespace QuantumZhou.Infrastructure.Data.Database.SqlServer
{
    public class SqlServerExecutor : DbExecutor
    {
        public SqlServerExecutor(string connectionString)
            : base(connectionString, new SqlConnection(connectionString))
        {
        }

        public override SqlBuilder SqlBuilder { get; } = new SqlServerBuilder();

        protected override IDbConnection GetConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            return connection;
        }

        protected override void AppendParams(IDbCommand command, string paramName, string value)
        {
            if (command is not SqlCommand cmd)
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }

            cmd.Parameters.AddWithValue(paramName, value);
        }

        protected override bool ExistTable(IDbConnection con, TableDefine table)
        {
            var count = Count(con, $"SELECT COUNT(*) FROM sysobjects WHERE name='{table.Name}' and xtype='U'");
            return count > 0;
        }
    }
}
