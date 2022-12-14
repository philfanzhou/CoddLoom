using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace QuantumZhou.Infrastructure.Data.Database.SQLite
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteExecutor : DbExecutor
    {
        public SQLiteExecutor(string directory, string dbFileName)
            : base(BuildConnectionString(directory, dbFileName))
        {
        }

        public SQLiteExecutor(string dbFileName)
            : this(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), dbFileName)
        {
        }

        protected override IDbConnection GetConnection(string connectionString)
        {
            var connection = new SQLiteConnection(connectionString);
            return connection;
        }

        protected override DbCommand AppendParams(IDbCommand command, WhereParams whereParams)
        {
            if (command is not SQLiteCommand cmd)
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
            var whereParams = new WhereParams("type", "table");
            whereParams.Add("name", $"{table.Name.Trim()}");
            var conditions = new WhereConditions(whereParams);

            var count = -1;
            Execute(con, reader =>
            {
                reader.Read();
                count = reader.GetInt32(0);
            }, SqlBuilder.Count("sqlite_master", conditions), whereParams);
            return count > 0;
        }

        private static string BuildConnectionString(string directory, string dbFileName)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filePath = Path.Combine(directory, dbFileName);
            if (!File.Exists(filePath))
            {
                SQLiteConnection.CreateFile(filePath);
            }

            var connStrBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = filePath,
                ReadOnly = false
            };

            return connStrBuilder.ConnectionString;
        }
    }
}
