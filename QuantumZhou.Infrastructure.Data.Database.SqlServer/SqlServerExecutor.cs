using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
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
            : base(connectionString, new SqlConnection(connectionString))
        {
        }

        public override SqlBuilder SqlBuilder { get; } = new SqlServerBuilder();

        protected override IDbConnection GetConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
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

        protected override bool ExistTable(IDbConnection con, TableDefine table)
        {
            var count = Count(con, $"SELECT COUNT(*) FROM sysobjects WHERE name='{table.Name}' and xtype='U'");
            return count > 0;
        }
    }
}
