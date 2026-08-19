using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom;
using CoddLoom.Condition;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CoddLoom.Tests.DbCode;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;
using CoddLoom.Tests.DbTest;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Tests basic DbEngine CRUD operations.
    /// Covers entity-based create, read, update, and delete operations.
    /// </summary>
    [TestClass]
    public class DbEngineBasicCrudTest : TestBase
    {

        /// <summary>
        /// Creates a test user entity.
        /// </summary>
        private static User CreateTestUser(string id, string unionId, int intData, string specialString)
        {
            return new User
            {
                Id = id,
                UnionId = unionId,
                RegistrationDate = DateTime.Now,
                IntData = intData,
                Data = new byte[] { 1, 2, 3, 4, 5 },
                DoubleData = 123.45,
                DecimalData = 123.456m,
                ShortData = (short)123,
                BoolData = true,
                SpecialString = specialString
            };
        }

        /// <summary>
        /// Tests inserting a single entity.
        /// </summary>
        [TestMethod]
        public void Insert_SingleRecord_Should_Succeed()
        {
            // Prepare the test entity.
            var entity = CreateTestUser("1", "TestUser", 123, "SingleTest");

            // Perform the insert.
            var affected = DbEngine.Insert(entity);

            // Verify the result.
            Assert.AreEqual(1, affected, "One record should be inserted.");

            // Verify that the data was inserted correctly.
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "TestUser");
            var count = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(1, count, "One record should be returned.");
        }

        /// <summary>
        /// Tests inserting multiple entities.
        /// </summary>
        [TestMethod]
        public void Insert_BatchRecords_Should_Succeed()
        {
            // Prepare the test entities.
            var entities = new List<User>();
            for (int i = 1; i <= 3; i++)
            {
                entities.Add(CreateTestUser(i.ToString(), $"BatchUser{i}", i * 100, $"Batch{i}"));
            }

            // Perform the batch insert.
            var affected = DbEngine.Insert(entities, 2);

            // Verify the result.
            Assert.AreEqual(3, affected, "Three records should be inserted.");

            // Verify that the data was inserted correctly.
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "BatchUser%", WhereOperator.Like);
            var count = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(3, count, "Three records should be returned.");
        }

        /// <summary>
        /// Tests updating an entity.
        /// </summary>
        [TestMethod]
        public void Update_Record_Should_Succeed()
        {
            // Insert the test data first.
            var originalEntity = CreateTestUser("1", "OriginalUser", 100, "Original");
            DbEngine.Insert(originalEntity);

            // Prepare the updated entity.
            var updatedEntity = CreateTestUser("1", "UpdatedUser", 200, "Updated");
            updatedEntity.RegistrationDate = DateTime.Now.AddDays(1);

            // Perform the update.
            var affected = DbEngine.Update(updatedEntity);

            // Verify the result.
            Assert.AreEqual(1, affected, "One record should be updated.");

            // Verify that the data was updated correctly.
            var retrievedEntity = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(retrievedEntity, "The updated entity should be returned.");
            Assert.AreEqual("UpdatedUser", retrievedEntity.UnionId, "UnionId should be updated.");
            Assert.AreEqual(200, retrievedEntity.IntData, "IntData should be updated.");
            Assert.AreEqual("Updated", retrievedEntity.SpecialString, "SpecialString should be updated.");
        }

        /// <summary>
        /// Tests deleting an entity.
        /// </summary>
        [TestMethod]
        public void Delete_Record_Should_Succeed()
        {
            // Insert the test data first.
            var entity = CreateTestUser("1", "ToBeDeleted", 300, "DeleteTest");
            DbEngine.Insert(entity);

            // Verify that the data was inserted.
            var beforeWhere = new WhereConditions();
            beforeWhere.Add(UserTable.UnionId, "ToBeDeleted");
            var beforeCount = DbEngine.Count(UserTable.TableName, beforeWhere);
            Assert.AreEqual(1, beforeCount, "One record should exist before deletion.");

            // Perform the deletion.
            var affected = DbEngine.Delete<User>("1");

            // Verify the result.
            Assert.AreEqual(1, affected, "One record should be deleted.");

            // Verify that the data was deleted.
            var afterCount = DbEngine.Count(UserTable.TableName, beforeWhere);
            Assert.AreEqual(0, afterCount, "No records should remain after deletion.");
       }

        /// <summary>
        /// Tests querying entities.
        /// </summary>
        [TestMethod]
        public void Select_Records_Should_Succeed()
        {
            // Insert the test data first.
            var entity1 = CreateTestUser("1", "SelectUser1", 100, "Select1");
            var entity2 = CreateTestUser("2", "SelectUser2", 200, "Select2");
            DbEngine.Insert(entity1);
            DbEngine.Insert(entity2);

            // Perform the query.
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "SelectUser%", WhereOperator.Like);
            var orderBy = new OrderByCondition(UserTable.UnionId, false); // ASC
            var columns = new ColumnParam();
            columns.AddSelect(UserTable.UnionId);
            columns.AddSelect(UserTable.IntData);

            var results = DbEngine.Select<User>(where, orderBy, columns);

            // Verify the result.
            Assert.IsNotNull(results, "The query result should not be null.");
            Assert.HasCount(2, results, "Two records should be returned.");
            Assert.AreEqual("SelectUser1", results[0].UnionId);
            Assert.AreEqual(100, results[0].IntData);
            Assert.AreEqual("SelectUser2", results[1].UnionId);
            Assert.AreEqual(200, results[1].IntData);
        }

        /// <summary>
        /// Tests querying an entity by ID.
        /// </summary>
        [TestMethod]
        public void SelectById_Record_Should_Succeed()
        {
            // Insert the test data first.
            var entity = CreateTestUser("1", "SelectByIdUser", 400, "SelectById");
            DbEngine.Insert(entity);

            // Query by ID.
            var result = DbEngine.SelectById<User>("1");

            // Verify the result.
            Assert.IsNotNull(result, "The entity should be returned.");
            Assert.AreEqual("SelectByIdUser", result.UnionId, "UnionId should match.");
            Assert.AreEqual(400, result.IntData, "IntData should match.");
       }

        /// <summary>
        /// Tests counting entities.
        /// </summary>
        [TestMethod]
        public void Count_Records_Should_Succeed()
        {
            // Insert the test data first.
            var entity1 = CreateTestUser("1", "CountUser1", 100, "Count1");
            var entity2 = CreateTestUser("2", "CountUser2", 200, "Count2");
            var entity3 = CreateTestUser("3", "OtherUser", 300, "Other");
            DbEngine.Insert(entity1);
            DbEngine.Insert(entity2);
            DbEngine.Insert(entity3);

            // Test the total count.
            var totalCount = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(3, totalCount, "The total record count should be three.");

            // Test a filtered count.
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "CountUser%", WhereOperator.Like);
            var filteredCount = DbEngine.Count(UserTable.TableName, where);
            Assert.AreEqual(2, filteredCount, "Two records should match the condition.");
       }

        /// <summary>
        /// Tests checking whether an entity exists.
        /// </summary>
        [TestMethod]
        public void Exist_Record_Should_Succeed()
        {
            // Insert the test data first.
            var entity = CreateTestUser("1", "ExistUser", 500, "ExistTest");
            DbEngine.Insert(entity);

            // Test the existence check.
            var existsWhere = new WhereConditions();
            existsWhere.Add(UserTable.UnionId, "ExistUser");
            var exists = DbEngine.Exist(UserTable.TableName, existsWhere);

            // Verify the result.
            Assert.IsTrue(exists, "The record should exist.");

            // Test a nonexistent record.
            var notExistsWhere = new WhereConditions();
            notExistsWhere.Add(UserTable.UnionId, "NonExistentUser");
            var notExists = DbEngine.Exist(UserTable.TableName, notExistsWhere);
            Assert.IsFalse(notExists, "The record should not exist.");
        }

        /// <summary>
        /// Tests dropping an entity table.
        /// </summary>
        [TestMethod]
        public void Drop_Table_Should_Succeed()
        {
            // Insert the test data first.
            var entity = CreateTestUser("1", "DropUser", 600, "DropTest");
            DbEngine.Insert(entity);

            // Verify that the table exists.
            var beforeCount = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(1, beforeCount, "One record should exist before dropping the table.");

            // Drop the table.
            DbEngine.Drop(UserTable.TableName);

            // Provider exception messages and casing differ; the portable contract
            // is that querying the dropped table fails.
            Assert.Throws<Exception>(() =>
                DbEngine.Count(UserTable.TableName, new WhereConditions()));
        }

        /// <summary>
        /// Tests a complex entity query condition.
        /// </summary>
        [TestMethod]
        public void Select_WithComplexConditions_Should_Succeed()
        {
            // Insert the test data first.
            var entity1 = CreateTestUser("1", "ComplexUser1", 100, "Complex1");
            var entity2 = CreateTestUser("2", "ComplexUser2", 200, "Complex2");
            var entity3 = CreateTestUser("3", "OtherUser", 300, "Other");
            DbEngine.Insert(entity1);
            DbEngine.Insert(entity2);
            DbEngine.Insert(entity3);

            // Test the complex query condition.
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "ComplexUser%", WhereOperator.Like);
            where.Add(UserTable.IntData, 150, WhereOperator.GreaterThan);
            var orderBy = new OrderByCondition(UserTable.IntData, true); // DESC

            var results = DbEngine.Select<User>(where, orderBy);

            // Verify the result.
            Assert.IsNotNull(results, "The query result should not be null.");
            Assert.HasCount(1, results, "One record should be returned.");
            Assert.AreEqual("ComplexUser2", results[0].UnionId);
            Assert.AreEqual(200, results[0].IntData);
        }

        
    }
}
