using CoddLoom;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Integration tests for column migration.
    /// </summary>
    public class ColumnMigrationIntegrationTest
    {
        public static void RunTest()
        {
            // Use SQLite so the test requires no additional configuration.
            var executor = new SqliteExecutor("test_column_migration.db");
            var dbEngine = new DbEngine(executor);

            try
            {
                // Test 1: Create the base table.
                Console.WriteLine("Test 1: Creating the base table...");
                var basicTable = new TableDefine(typeof(BasicTestTable));
                dbEngine.InitializeTable(new[] { basicTable });
                Console.WriteLine("Base table created successfully.");

                // Test 2: Add new columns.
                Console.WriteLine("Test 2: Adding new columns...");
                var fullTable = new TableDefine(typeof(TestColumnMigrationTable));
                dbEngine.InitializeTable(new[] { fullTable });
                Console.WriteLine("New columns added successfully.");

                // Test 3: Run again and skip existing columns.
                Console.WriteLine("Test 3: Repeating the column check...");
                dbEngine.InitializeTable(new[] { fullTable });
                Console.WriteLine("Repeated run succeeded and skipped existing columns.");

                Console.WriteLine("All tests passed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                throw;
            }
        }
    }
}
