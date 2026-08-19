using CoddLoom;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// 列改动功能集成测试
    /// </summary>
    public class ColumnMigrationIntegrationTest
    {
        public static void RunTest()
        {
            // 使用SQLite进行测试（无需额外配置）
            var executor = new SqliteExecutor("test_column_migration.db");
            var dbEngine = new DbEngine(executor);

            try
            {
                // 测试1: 创建基础表
                Console.WriteLine("测试1: 创建基础表...");
                var basicTable = new TableDefine(typeof(BasicTestTable));
                dbEngine.InitializeTable(new[] { basicTable });
                Console.WriteLine("✓ 基础表创建成功");

                // 测试2: 添加新列
                Console.WriteLine("测试2: 添加新列...");
                var fullTable = new TableDefine(typeof(TestColumnMigrationTable));
                dbEngine.InitializeTable(new[] { fullTable });
                Console.WriteLine("✓ 新列添加成功");

                // 测试3: 再次运行（应该跳过已存在的列）
                Console.WriteLine("测试3: 重复运行列检查...");
                dbEngine.InitializeTable(new[] { fullTable });
                Console.WriteLine("✓ 重复运行成功（跳过已存在的列）");

                Console.WriteLine("所有测试通过！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                throw;
            }
        }
    }
}
