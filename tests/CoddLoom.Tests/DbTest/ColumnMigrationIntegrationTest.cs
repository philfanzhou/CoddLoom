using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Input;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Integration tests for column migration.
    /// </summary>
    [TestClass]
    public class ColumnMigrationIntegrationTest : TestBase
    {
        [TestMethod]
        public void InitializeTable_AddsMissingColumns_AndRemainsIdempotent()
        {
            var dbEngine = new DbEngine(Executor);

            try
            {
                var basicTable = new TableDefine(typeof(BasicTestTable));
                dbEngine.InitializeTable(new[] { basicTable });

                var fullTable = new TableDefine(typeof(TestColumnMigrationTable));
                dbEngine.InitializeTable(new[] { fullTable });
                dbEngine.InitializeTable(new[] { fullTable });

                var values = new InputValues()
                    .Add(TestColumnMigrationTable.Id, "migration-1")
                    .Add(TestColumnMigrationTable.Name, "migration")
                    .Add(TestColumnMigrationTable.NewColumn1, "added")
                    .Add(TestColumnMigrationTable.NewColumn2, 2)
                    .Add(TestColumnMigrationTable.NewColumn3, true);

                Assert.AreEqual(1, dbEngine.Insert(TestColumnMigrationTable.TableName, values));
                var addedValue = Executor.Scalar(
                    $"SELECT {TestColumnMigrationTable.NewColumn1} FROM {TestColumnMigrationTable.TableName}",
                    value => value.ToString());
                Assert.AreEqual("added", addedValue);
            }
            finally
            {
                dbEngine.Drop(TestColumnMigrationTable.TableName);
            }
        }
    }
}
