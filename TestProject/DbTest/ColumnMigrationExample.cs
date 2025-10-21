using Qz.Infra.Database;
using Qz.Infra.Database.MySql;
using Qz.Infra.Database.SqlServer;
using Qz.Infra.Database.SQLite;
using Qz.Infra.Database.Table;
using System.Collections.Generic;
using TestProject.DbCode.Tables;

namespace TestProject.DbTest
{
    /// <summary>
    /// 列改动功能使用示例
    /// 演示如何使用DbEngine的InitializeTableColumns方法来自动添加缺失的列
    /// </summary>
    public class ColumnMigrationExample
    {
        /// <summary>
        /// 演示SQLite数据库的列改动功能
        /// </summary>
        public static void SqliteExample()
        {
            // 创建SQLite执行器
            var executor = new SQLiteExecutor("test.db");
            var dbEngine = new DbEngine(executor);

            // 定义表结构（包含所有列）
            var tables = new List<TableDefine>
            {
                new(typeof(TestColumnMigrationTable))
            };

            // 1. 初始化表（如果表不存在则创建）
            dbEngine.InitializeTable(tables);

            // 2. 再次调用InitializeTable会自动检查并添加缺失的列
            dbEngine.InitializeTable(tables);
        }

        /// <summary>
        /// 演示SQL Server数据库的列改动功能
        /// </summary>
        public static void SqlServerExample()
        {
            var connectionString = "Server=localhost;Database=TestDb;Integrated Security=true;";
            var executor = new SqlServerExecutor(connectionString);
            var dbEngine = new DbEngine(executor);

            var tables = new List<TableDefine>
            {
                new(typeof(TestColumnMigrationTable))
            };

            // 初始化表（会自动检查并添加缺失的列）
            dbEngine.InitializeTable(tables);
        }

        /// <summary>
        /// 演示MySQL数据库的列改动功能
        /// </summary>
        public static void MySqlExample()
        {
            var connectionString = "Server=localhost;Database=TestDb;Uid=root;Pwd=password;";
            var executor = new MySqlExecutor(connectionString);
            var dbEngine = new DbEngine(executor);

            var tables = new List<TableDefine>
            {
                new(typeof(TestColumnMigrationTable))
            };

            // 初始化表（会自动检查并添加缺失的列）
            dbEngine.InitializeTable(tables);
        }
    }
}
