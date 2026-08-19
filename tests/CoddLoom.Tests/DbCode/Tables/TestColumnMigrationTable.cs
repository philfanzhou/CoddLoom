using CoddLoom.Table;
using System.Data;

namespace CoddLoom.Tests.DbCode.Tables
{
    internal static class TestColumnMigrationTable
    {
        [DbTableName]
        internal const string TableName = "TestColumnMigrationTable";

        [DbPrimaryKey(Type = DbType.String)]
        internal const string Id = "id";

        [DbColumnString(AllowEmpty = false)]
        internal const string Name = "name";

        [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
        public const string CreatedDate = "createdDate";

        // These columns are added later to test column migration.
        [DbColumn(Type = DbType.String, AllowEmpty = true)]
        public const string NewColumn1 = "newColumn1";

        [DbColumn(Type = DbType.Int32, AllowEmpty = true)]
        public const string NewColumn2 = "newColumn2";

        [DbColumn(Type = DbType.Boolean, AllowEmpty = true)]
        public const string NewColumn3 = "newColumn3";
    }
}
