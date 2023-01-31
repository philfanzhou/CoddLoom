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

        protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
        {
            if (command is not SqlCommand cmd)
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }

            return cmd.Parameters.AddWithValue;
        }

        protected override SqlBuilderCountParam GetExistTableParam(TableDefine table)
        {
            var whereParams = new WhereParams("xtype", "U");
            whereParams.Add("name", table.Name);
            return new SqlBuilderCountParam("sysobjects", whereParams);
        }
    }
}
