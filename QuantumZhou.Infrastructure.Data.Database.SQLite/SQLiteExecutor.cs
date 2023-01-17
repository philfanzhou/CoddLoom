using QuantumZhou.Infrastructure.Data.Database.Condition;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Table;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace QuantumZhou.Infrastructure.Data.Database.SQLite
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteExecutor : DbExecutor
    {
        public SQLiteExecutor(string connectionString)
            : base(connectionString, BuildConnection(connectionString))
        {
        }

        public SQLiteExecutor(string directory, string dbFileName)
            : this(BuildConnectionString(directory, dbFileName))
        {
        }

        public override IDbConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        protected override void AppendParams(IDbCommand command, string paramName, string value)
        {
            if (command is not SQLiteCommand cmd)
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }

            cmd.Parameters.AddWithValue(paramName, value);
        }

        protected override bool ExistTable(IDbConnection con, TableDefine table)
        {
            var whereParams = new WhereParams("type", "table");
            whereParams.Add("name", table.Name.Trim());
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
            CreateFilePath(directory, dbFileName);
            var filePath = Path.Combine(directory, dbFileName);
            var connStrBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = filePath,
                ReadOnly = false
            };
            return connStrBuilder.ConnectionString;
        }

        private static SQLiteConnection BuildConnection(string connectionString)
        {
            var builder = new SQLiteConnectionStringBuilder(connectionString);
            var dir = Path.GetDirectoryName(builder.DataSource);
            var file = Path.GetFileName(builder.DataSource);
            CreateFilePath(dir, file);
            return new SQLiteConnection(builder.ConnectionString);
        }

        private static void CreateFilePath(string directory, string dbFileName)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }
            if (string.IsNullOrEmpty(dbFileName))
            {
                throw new ArgumentNullException(nameof(dbFileName));
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fullPath = Path.Combine(directory, dbFileName);
            if (!File.Exists(fullPath))
            {
                SQLiteConnection.CreateFile(fullPath);
            }
        }
    }
}
