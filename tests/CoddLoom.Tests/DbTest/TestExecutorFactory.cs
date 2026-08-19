using CoddLoom;
using CoddLoom.MariaDb;
using CoddLoom.MySql;
using CoddLoom.Oracle;
using CoddLoom.PostgreSql;
using CoddLoom.Sqlite;
using CoddLoom.SqlServer;
using System;
using System.Collections.Generic;
using System.IO;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Factory for database executors.
    /// Provides consistent executor creation for all supported database types.
    /// </summary>
    public static class TestExecutorFactory
    {
        /// <summary>
        /// Supported database types.
        /// </summary>
        public enum DatabaseType
        {
            SQLite,
            MySql,
            SqlServer,
            MariaDB,
            Oracle,
            PostgreSql
        }

        /// <summary>
        /// Paths of temporary SQLite database files retained for cleanup.
        /// </summary>
        private static readonly Dictionary<DbExecutor, string> SQLiteFilePaths = new Dictionary<DbExecutor, string>();

        /// <summary>
        /// Test configuration that can be populated from a configuration file.
        /// </summary>
        private static readonly Dictionary<DatabaseType, string> ConnectionStrings = new Dictionary<DatabaseType, string>
        {
            { DatabaseType.SQLite, "Data Source=:memory:;Version=3;" },
            { DatabaseType.MySql, "Server=localhost;Database=test_db;Uid=root;Pwd=password;" },
            { DatabaseType.SqlServer, "Server=localhost;Database=test_db;Trusted_Connection=true;" },
            { DatabaseType.MariaDB, "Server=localhost;Database=test_db;Uid=root;Pwd=password;" },
            { DatabaseType.Oracle, "Data Source=localhost:1521/XE;User Id=test;Password=password;" },
            { DatabaseType.PostgreSql, "Host=localhost;Port=5432;Database=coddloom;Username=postgres;Password=postgres;" }
        };

        /// <summary>
        /// The active database type, configurable through an environment variable or configuration file.
        /// </summary>
        public static DatabaseType CurrentDatabaseType { get; set; } = GetConfiguredDatabaseType();

        /// <summary>
        /// Creates a database executor.
        /// </summary>
        /// <returns>A database executor.</returns>
        public static DbExecutor CreateExecutor()
        {
            return CreateExecutor(CurrentDatabaseType);
        }

        /// <summary>
        /// Creates an executor for the specified database type.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        /// <returns>A database executor.</returns>
        public static DbExecutor CreateExecutor(DatabaseType dbType)
        {
            var connectionString = GetConnectionString(dbType);
            DbExecutor executor;
            
            switch (dbType)
            {
                case DatabaseType.SQLite:
                    executor = new SqliteExecutor(connectionString);
                    // Retain the path of a file-backed database for cleanup.
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
                case DatabaseType.PostgreSql:
                    return new PostgreSqlExecutor(connectionString);
                default:
                    throw new ArgumentException($"Unsupported database type: {dbType}");
            }
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        /// <returns>The connection string.</returns>
        public static string GetConnectionString(DatabaseType dbType)
        {
            // Check environment variables first.
            var envKey = $"TEST_DB_CONNECTION_{dbType.ToString().ToUpper()}";
            var envConnectionString = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                return envConnectionString;
            }

            // Then check configuration; this simplified implementation can later read appsettings.json.
            if (ConnectionStrings.ContainsKey(dbType))
            {
                return ConnectionStrings[dbType];
            }

            throw new ArgumentException($"No connection string is configured for database type {dbType}.");
        }

        private static DatabaseType GetConfiguredDatabaseType()
        {
            var configuredType = Environment.GetEnvironmentVariable("TEST_DATABASE_TYPE");
            if (string.IsNullOrWhiteSpace(configuredType))
            {
                return DatabaseType.SQLite;
            }

            if (Enum.TryParse(configuredType, true, out DatabaseType databaseType))
            {
                return databaseType;
            }

            throw new InvalidOperationException(
                $"Unsupported TEST_DATABASE_TYPE value '{configuredType}'.");
        }

        /// <summary>
        /// Gets a database executor suitable for local tests, primarily SQLite.
        /// </summary>
        /// <returns>A database executor.</returns>
        public static DbExecutor CreateInMemoryExecutor()
        {
            if (CurrentDatabaseType == DatabaseType.SQLite)
            {
                // Create a temporary directory to ensure SqliteExecutor receives a non-null directory.
                var tempDir = Path.GetTempPath();
                var connectionString = $"Data Source={Path.Combine(tempDir, "test_memory.db")};Version=3;";
                
                var executor = new SqliteExecutor(connectionString);
                
                // Retain the temporary file path for cleanup.
                SQLiteFilePaths[executor] = Path.Combine(tempDir, "test_memory.db");
                
                return executor;
            }
            
            // Create a temporary database for other providers.
            return CreateExecutor();
        }

        /// <summary>
        /// Cleans test data by using DbEngine.Drop to remove every test table.
        /// </summary>
        /// <param name="executor">The database executor.</param>
        public static void CleanupTestData(DbExecutor executor)
        {
            if (executor == null)
                return;

            try
            {
                // Create a DbEngine instance to access its operations.
                var dbEngine = new DbEngine(executor);
                
                // Remove every test table consistently across database types.
                var tables = new[] { "UserTable" };
                
                foreach (var table in tables)
                {
                    try
                    {
                        // DbEngine.Drop handles provider-specific SQL syntax.
                        dbEngine.Drop(table);
                    }
                    catch (Exception ex)
                    {
                        // Failure to remove one table must not prevent cleanup of the others.
                        // A missing table is acceptable here.
                        Console.WriteLine($"Failed to clean up table {table}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Report cleanup failure without changing the test result.
                Console.WriteLine($"An error occurred while cleaning test data: {ex.Message}");
            }
            finally
            {
                // Clean up file-backed SQLite databases.
                if (CurrentDatabaseType == DatabaseType.SQLite && SQLiteFilePaths.ContainsKey(executor))
                {
                    try
                    {
                        var filePath = SQLiteFilePaths[executor];
                        SQLiteFilePaths.Remove(executor);
                        
                        // Attempt file deletion without requiring it to succeed.
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch
                    {
                        // A file-deletion failure does not invalidate the overall cleanup.
                    }
                }
            }
        }
    }
}
