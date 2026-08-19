using CoddLoom;
using CoddLoom.Table;
using System.Collections.Generic;
using System.Data;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    public class ColumnMigrationTest
    {
        private readonly DbEngine _dbEngine;

        public ColumnMigrationTest(DbEngine dbEngine)
        {
            _dbEngine = dbEngine;
        }

        public void TestColumnMigration()
        {
            // 1. 首先创建基础表（只包含基本列）
            var basicTable = new TableDefine(typeof(BasicTestTable));
            _dbEngine.InitializeTable(new[] { basicTable });

            // 2. 再次调用InitializeTable，会自动检查并添加缺失的列
            var fullTable = new TableDefine(typeof(TestColumnMigrationTable));
            _dbEngine.InitializeTable(new[] { fullTable });

            // 3. 验证列是否成功添加
            // 这里可以添加验证逻辑
        }
    }

    // 基础表定义（只包含基本列）
    internal static class BasicTestTable
    {
        [DbTableName]
        internal const string TableName = "TestColumnMigrationTable"; // 使用相同的表名

        [DbPrimaryKey(Type = DbType.String)]
        internal const string Id = "id";

        [DbColumnString(AllowEmpty = false)]
        internal const string Name = "name";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string CreatedDate = "createdDate";
    }
}
