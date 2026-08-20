using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Input;
using CoddLoom.Entity;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode.Tables;
using System;
using System.Data;

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
                Assert.AreEqual(1, dbEngine.Insert(BasicTestTable.TableName,
                    new InputValues()
                        .Add(BasicTestTable.Id, "pre-migration")
                        .Add(BasicTestTable.Name, "existing row")));

                var fullTable = new TableDefine(typeof(TestColumnMigrationTable));
                dbEngine.InitializeTable(new[] { fullTable });
                dbEngine.InitializeTable(new[] { fullTable });

                var migratedExistingValue = Executor.Scalar(
                    $"SELECT {TestColumnMigrationTable.NewColumn1} FROM {TestColumnMigrationTable.TableName} "
                    + $"WHERE {TestColumnMigrationTable.Id} = 'pre-migration'",
                    value => value.ToString());
                Assert.IsNull(migratedExistingValue);

                var values = new InputValues()
                    .Add(TestColumnMigrationTable.Id, "migration-1")
                    .Add(TestColumnMigrationTable.Name, "migration")
                    .Add(TestColumnMigrationTable.NewColumn1, "added")
                    .Add(TestColumnMigrationTable.NewColumn2, 2)
                    .Add(TestColumnMigrationTable.NewColumn3, true);

                Assert.AreEqual(1, dbEngine.Insert(TestColumnMigrationTable.TableName, values));
                var addedValue = Executor.Scalar(
                    $"SELECT {TestColumnMigrationTable.NewColumn1} FROM {TestColumnMigrationTable.TableName} "
                    + $"WHERE {TestColumnMigrationTable.Id} = 'migration-1'",
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

        [TestMethod]
        public void InitializeTable_AddsRequiredColumnToEmptyTable()
        {
            var dbEngine = new DbEngine(Executor);

            try
            {
                dbEngine.InitializeTable([new TableDefine(typeof(BasicTestTable))]);
                var requiredTable = new TableDefine(typeof(RequiredColumnMigrationTable));

                dbEngine.InitializeTable([requiredTable]);
                dbEngine.InitializeTable([requiredTable]);

                Assert.AreEqual(1, dbEngine.Insert(RequiredColumnMigrationTable.TableName,
                    new InputValues()
                        .Add(RequiredColumnMigrationTable.Id, "required-1")
                        .Add(RequiredColumnMigrationTable.Name, "required migration")
                        .Add(RequiredColumnMigrationTable.RequiredColumn, 42)));
                Assert.AreEqual(42, Executor.Scalar(
                    $"SELECT {RequiredColumnMigrationTable.RequiredColumn} "
                    + $"FROM {RequiredColumnMigrationTable.TableName}",
                    System.Convert.ToInt32));
            }
            finally
            {
                dbEngine.Drop(RequiredColumnMigrationTable.TableName);
            }
        }

        [TestMethod]
        public void InitializeTable_RejectsRequiredColumnOnPopulatedTableBeforeAnyDdl()
        {
            var dbEngine = new DbEngine(Executor);

            try
            {
                dbEngine.InitializeTable([new TableDefine(typeof(BasicTestTable))]);
                dbEngine.Insert(BasicTestTable.TableName,
                    new InputValues()
                        .Add(BasicTestTable.Id, "existing-1")
                        .Add(BasicTestTable.Name, "existing row"));

                var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
                    dbEngine.InitializeTable([
                        new TableDefine(typeof(UnrelatedNewTable)),
                        new TableDefine(typeof(RequiredColumnMigrationTable))
                    ]));

                StringAssert.Contains(exception.Message, RequiredColumnMigrationTable.TableName);
                StringAssert.Contains(exception.Message, RequiredColumnMigrationTable.RequiredColumn);
                Assert.IsFalse(CanSelectColumn(RequiredColumnMigrationTable.TableName,
                    RequiredColumnMigrationTable.NullableColumn));
                Assert.IsFalse(CanSelectColumn(RequiredColumnMigrationTable.TableName,
                    RequiredColumnMigrationTable.RequiredColumn));
                Assert.IsFalse(CanSelectColumn(UnrelatedNewTable.TableName, UnrelatedNewTable.Id));
            }
            finally
            {
                dbEngine.Drop(RequiredColumnMigrationTable.TableName);
                dbEngine.Drop(UnrelatedNewTable.TableName);
            }
        }

        private bool CanSelectColumn(string tableName, string columnName)
        {
            try
            {
                Executor.Reader($"SELECT {columnName} FROM {tableName}", _ => true);
                return true;
            }
            catch
            {
                return false;
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

        private static class RequiredColumnMigrationTable
        {
            [DbTableName]
            public const string TableName = BasicTestTable.TableName;

            [DbPrimaryKey(Type = DbType.String)]
            public const string Id = BasicTestTable.Id;

            [DbColumnString(AllowEmpty = false)]
            public const string Name = BasicTestTable.Name;

            [DbColumn(Type = DbType.DateTime, AllowEmpty = true)]
            public const string CreatedDate = BasicTestTable.CreatedDate;

            [DbColumn(Type = DbType.String, AllowEmpty = true)]
            public const string NullableColumn = "nullableColumn";

            [DbColumn(Type = DbType.Int32, AllowEmpty = false)]
            public const string RequiredColumn = "requiredColumn";
        }

        private static class UnrelatedNewTable
        {
            [DbTableName]
            public const string TableName = "UnrelatedNewMigrationTable";

            [DbPrimaryKey(Type = DbType.String)]
            public const string Id = "id";
        }
    }
}
