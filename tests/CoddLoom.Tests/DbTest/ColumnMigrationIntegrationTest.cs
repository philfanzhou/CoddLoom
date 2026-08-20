using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Input;
using CoddLoom.Entity;
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

                var entity = new MigratedEntity
                {
                    Id = "migration-2",
                    Name = "entity migration",
                    NewColumn1 = "written-through-entity",
                    NewColumn2 = 3,
                    NewColumn3 = true
                };
                Assert.AreEqual(1, dbEngine.Insert(entity));
                var entityValue = Executor.Scalar(
                    $"SELECT {TestColumnMigrationTable.NewColumn1} FROM {TestColumnMigrationTable.TableName} "
                    + $"WHERE {TestColumnMigrationTable.Id} = 'migration-2'",
                    value => value.ToString());
                Assert.AreEqual("written-through-entity", entityValue);
            }
            finally
            {
                dbEngine.Drop(TestColumnMigrationTable.TableName);
            }
        }

        [MapTable(Name = TestColumnMigrationTable.TableName)]
        private sealed class MigratedEntity
        {
            [MapColumn(Name = TestColumnMigrationTable.Id, PrimaryKey = true)]
            public string Id { get; set; }

            [MapColumn(Name = TestColumnMigrationTable.Name)]
            public string Name { get; set; }

            [MapColumn(Name = TestColumnMigrationTable.NewColumn1)]
            public string NewColumn1 { get; set; }

            [MapColumn(Name = TestColumnMigrationTable.NewColumn2)]
            public int NewColumn2 { get; set; }

            [MapColumn(Name = TestColumnMigrationTable.NewColumn3)]
            public bool NewColumn3 { get; set; }
        }
    }
}
