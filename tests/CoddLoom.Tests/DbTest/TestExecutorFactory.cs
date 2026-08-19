using CoddLoom;
using CoddLoom.MariaDb;
using CoddLoom.MySql;
using CoddLoom.Oracle;
using CoddLoom.Sqlite;
using CoddLoom.SqlServer;
using System;
using System.Collections.Generic;
using System.IO;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// 数据库执行器工厂类
    /// 统一处理不同数据库类型的Executor创建
    /// </summary>
    public static class TestExecutorFactory
    {
        /// <summary>
        /// 支持的数据库类型
        /// </summary>
        public enum DatabaseType
        {
            SQLite,
            MySql,
            SqlServer,
            MariaDB,
            Oracle
        }

        /// <summary>
        /// 存储临时SQLite数据库文件路径，用于清理
        /// </summary>
        private static readonly Dictionary<DbExecutor, string> SQLiteFilePaths = new Dictionary<DbExecutor, string>();

        /// <summary>
        /// 测试配置，可以从配置文件读取
        /// </summary>
        private static readonly Dictionary<DatabaseType, string> ConnectionStrings = new Dictionary<DatabaseType, string>
        {
            { DatabaseType.SQLite, "Data Source=:memory:;Version=3;" },
            { DatabaseType.MySql, "Server=localhost;Database=test_db;Uid=root;Pwd=password;" },
            { DatabaseType.SqlServer, "Server=localhost;Database=test_db;Trusted_Connection=true;" },
            { DatabaseType.MariaDB, "Server=localhost;Database=test_db;Uid=root;Pwd=password;" },
            { DatabaseType.Oracle, "Data Source=localhost:1521/XE;User Id=test;Password=password;" }
        };

        /// <summary>
        /// 当前使用的数据库类型，可以通过环境变量或配置文件设置
        /// </summary>
        public static DatabaseType CurrentDatabaseType { get; set; } = DatabaseType.SQLite;

        /// <summary>
        /// 创建数据库执行器
        /// </summary>
        /// <returns>数据库执行器实例</returns>
        public static DbExecutor CreateExecutor()
        {
            return CreateExecutor(CurrentDatabaseType);
        }

        /// <summary>
        /// 创建指定类型的数据库执行器
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <returns>数据库执行器实例</returns>
        public static DbExecutor CreateExecutor(DatabaseType dbType)
        {
            var connectionString = GetConnectionString(dbType);
            DbExecutor executor;
            
            switch (dbType)
            {
                case DatabaseType.SQLite:
                    executor = new SqliteExecutor(connectionString);
                    // 如果是文件数据库（非内存），记录文件路径用于清理
                    if (!connectionString.Contains(":memory:"))
                    {
                        var dbPath = connectionString.Split(';')[0].Replace("Data Source=", "").Trim();
                        SQLiteFilePaths[executor] = dbPath;
                    }
                    return executor;
                case DatabaseType.MySql:
                    return new MySqlExecutor(connectionString);
                case DatabaseType.SqlServer:
                    return new SqlServerExecutor(connectionString);
                case DatabaseType.MariaDB:
                    return new MariaDbExecutor(connectionString);
                case DatabaseType.Oracle:
                    return new OracleExecutor(connectionString);
                default:
                    throw new ArgumentException($"不支持的数据库类型: {dbType}");
            }
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <returns>连接字符串</returns>
        public static string GetConnectionString(DatabaseType dbType)
        {
            // 首先检查环境变量
            var envKey = $"TEST_DB_CONNECTION_{dbType.ToString().ToUpper()}";
            var envConnectionString = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                return envConnectionString;
            }

            // 然后检查配置文件（这里简化处理，实际可以从appsettings.json读取）
            if (ConnectionStrings.ContainsKey(dbType))
            {
                return ConnectionStrings[dbType];
            }

            throw new ArgumentException($"找不到数据库类型 {dbType} 的连接字符串配置");
        }

        /// <summary>
        /// 获取适合内存测试的数据库执行器（主要用于SQLite）
        /// </summary>
        /// <returns>数据库执行器实例</returns>
        public static DbExecutor CreateInMemoryExecutor()
        {
            if (CurrentDatabaseType == DatabaseType.SQLite)
            {
                // 为内存数据库创建一个临时目录，避免SQLiteExecutor的directory为null错误
                var tempDir = Path.GetTempPath();
                var connectionString = $"Data Source={Path.Combine(tempDir, "test_memory.db")};Version=3;";
                
                var executor = new SqliteExecutor(connectionString);
                
                // 记录这个临时文件路径用于后续清理
                SQLiteFilePaths[executor] = Path.Combine(tempDir, "test_memory.db");
                
                return executor;
            }
            
            // 对于其他数据库，创建临时数据库
            return CreateExecutor();
        }

        /// <summary>
        /// 清理测试数据 - 使用DbEngine的Drop方法删除所有测试表
        /// </summary>
        /// <param name="executor">数据库执行器</param>
        public static void CleanupTestData(DbExecutor executor)
        {
            if (executor == null)
                return;

            try
            {
                // 创建DbEngine实例来使用方法
                var dbEngine = new DbEngine(executor);
                
                // 统一删除所有测试表，适用于所有数据库类型
                var tables = new[] { "UserTable" };
                
                foreach (var table in tables)
                {
                    try
                    {
                        // 使用DbEngine的Drop方法，它会处理不同数据库的SQL语法差异
                        dbEngine.Drop(table);
                    }
                    catch (Exception ex)
                    {
                        // 单个表删除失败不影响其他表的清理
                        // 可能是表不存在，这是可以接受的
                        Console.WriteLine($"清理表 {table} 失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录清理失败，但不抛出异常，避免影响测试结果
                Console.WriteLine($"清理测试数据时发生错误: {ex.Message}");
            }
            finally
            {
                // 清理SQLite文件数据库
                if (CurrentDatabaseType == DatabaseType.SQLite && SQLiteFilePaths.ContainsKey(executor))
                {
                    try
                    {
                        var filePath = SQLiteFilePaths[executor];
                        SQLiteFilePaths.Remove(executor);
                        
                        // 尝试删除文件，但不强制要求成功
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch
                    {
                        // 文件删除失败不影响整体清理
                    }
                }
            }
        }
    }
}
