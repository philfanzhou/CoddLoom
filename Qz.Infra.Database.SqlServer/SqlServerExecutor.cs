using Qz.Infra.Database;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Qz.Infra.Database.SqlServer
{
    public class SqlServerExecutor : DbExecutor
    {
        public SqlServerExecutor(string connectionString)
            : base(connectionString, new SqlConnection(connectionString))
        {
        }

        public override SqlBuilder SqlBuilder { get; } = new SqlServerBuilder();

        public override IDbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
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
            var whereParams = new WhereParams("xtype", "U");
            whereParams.Add("name", table.Name);

            var builderParam = new SqlBuilderCountParam("sysobjects", whereParams);
            var count = Count(con, SqlBuilder.Count(builderParam), whereParams);
            return count > 0;
        }
    }
}
