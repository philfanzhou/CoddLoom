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
            // 1. Create the base table with only the essential columns.
            var basicTable = new TableDefine(typeof(BasicTestTable));
            _dbEngine.InitializeTable(new[] { basicTable });

            // 2. Call InitializeTable again to detect and add missing columns.
            var fullTable = new TableDefine(typeof(TestColumnMigrationTable));
            _dbEngine.InitializeTable(new[] { fullTable });

            // 3. Verify that the columns were added successfully.
            // Additional verification can be added here.
        }
    }

    // Base table definition containing only essential columns.
    internal static class BasicTestTable
    {
        [DbTableName]
        internal const string TableName = "TestColumnMigrationTable"; // Use the same table name.

        [DbPrimaryKey(Type = DbType.String)]
        internal const string Id = "id";

        [DbColumnString(AllowEmpty = false)]
        internal const string Name = "name";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string CreatedDate = "createdDate";
    }
}
