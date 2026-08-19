using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Condition;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Tests DbEngine transaction operations.
    /// Covers entity-based commit and rollback behavior.
    /// </summary>
    [TestClass]
    public class DbEngineTransactionTest : TestBase
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
        /// Tests a successful entity transaction commit.
        /// </summary>
        [TestMethod]
        public void Transaction_SuccessfulCommit_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user1 = CreateTestUser("1", "TransactionUser1", 100, "Tran1");
            var user2 = CreateTestUser("2", "TransactionUser2", 200, "Tran2");

            Executor.Transaction(tran =>
            {
                DbEngine.Insert(user1, null, tran);
                DbEngine.Insert(user2, null, tran);
            });

            // Verify that the data was committed.
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(2, count, "Two records should exist after commit.");

            var retrievedUser1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(retrievedUser1);
            Assert.AreEqual("TransactionUser1", retrievedUser1.UnionId);
        }

        /// <summary>
        /// Tests rollback after an exception in an entity transaction.
        /// </summary>
        [TestMethod]
        public void Transaction_RollbackOnException_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user1 = CreateTestUser("1", "RollbackUser1", 100, "Rollback1");
            var user2 = CreateTestUser("2", "RollbackUser2", 200, "Rollback2");

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                Executor.Transaction(tran =>
                {
                    DbEngine.Insert(user1, null, tran);
                    DbEngine.Insert(user2, null, tran);
                    throw new InvalidOperationException("Intentional exception for rollback test");
                });
            });

            // Verify that the data was rolled back.
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(0, count, "No records should remain after rollback.");
        }

        /// <summary>
        /// Tests successful entity operations with TryTransaction.
        /// </summary>
        [TestMethod]
        public void TryTransaction_SuccessfulOperation_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user = CreateTestUser("1", "TryUser", 100, "TryTest");

            var result = Executor.TryTransaction(tran =>
            {
                DbEngine.Insert(user, null, tran);
                return true; // Return a value that indicates success.
            });

            Assert.IsTrue(result, "TryTransaction should succeed and return true.");

            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(1, count, "One record should exist after TryTransaction succeeds.");
        }

        /// <summary>
        /// Tests exception handling for entity operations with TryTransaction.
        /// </summary>
        [TestMethod]
        public void TryTransaction_ExceptionHandling_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user = CreateTestUser("1", "TryUserFail", 100, "TryTestFail");

            var result = Executor.TryTransaction<bool>(tran =>
            {
                DbEngine.Insert(user, null, tran);
                throw new InvalidOperationException("Intentional exception for TryTransaction rollback test");
            });

            Assert.IsFalse(result, "TryTransaction should return the default value false after an exception.");

            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(0, count, "No records should remain after TryTransaction throws.");
        }

        /// <summary>
        /// Tests DbEngine's generic transaction operation with entities.
        /// </summary>
        [TestMethod]
        public void Transaction_GenericOperations_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            var user1 = CreateTestUser("1", "GenericTranUser1", 100, "GT1");
            var user2 = CreateTestUser("2", "GenericTranUser2", 200, "GT2");

            Executor.Transaction(tran =>
            {
                DbEngine.Insert(user1, null, tran);
                DbEngine.Update(user2, null, tran); // user2 does not exist, so no rows are updated.
            });

            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(1, count, "Only user1 should be inserted.");

            var retrievedUser1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(retrievedUser1);
            Assert.AreEqual("GenericTranUser1", retrievedUser1.UnionId);

            var retrievedUser2 = DbEngine.SelectById<User>("2");
            Assert.IsNull(retrievedUser2, "user2 should not exist.");
        }

        /// <summary>
        /// Tests complex entity transactions with inserts, updates, and deletes.
        /// </summary>
        [TestMethod]
        public void Transaction_ComplexOperations_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            // Insert initial data.
            DbEngine.Insert(CreateTestUser("1", "ComplexUser1", 100, "Original1"));
            DbEngine.Insert(CreateTestUser("2", "ComplexUser2", 200, "Original2"));
            DbEngine.Insert(CreateTestUser("3", "ComplexUser3", 300, "Original3"));

            Executor.Transaction(tran =>
            {
                // 1. Insert a new record.
                DbEngine.Insert(CreateTestUser("4", "ComplexUser4", 400, "New4"), null, tran);

                // 2. Update an existing record.
                var updateEntity = CreateTestUser("1", "ComplexUser1", 900, "ComplexUpdated");
                DbEngine.Update(updateEntity, null, tran);

                // 3. Delete a record.
                DbEngine.Delete<User>("2", null, tran);
            });

            // Verify the state after commit.
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(3, count, "There should be three records (3 original + 1 new - 1 deleted).");

            var user1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(user1);
            Assert.AreEqual(900, user1.IntData, "User1's IntData should be updated.");
            Assert.AreEqual("ComplexUpdated", user1.SpecialString);

            var user2 = DbEngine.SelectById<User>("2");
            Assert.IsNull(user2, "User2 should be deleted.");

            var user3 = DbEngine.SelectById<User>("3");
            Assert.IsNotNull(user3);
            Assert.AreEqual(300, user3.IntData, "User3 should remain unchanged.");

            var user4 = DbEngine.SelectById<User>("4");
            Assert.IsNotNull(user4);
            Assert.AreEqual(400, user4.IntData, "User4 should be inserted.");
        }

        


        /// <summary>
        /// Tests entity batch operations within a transaction.
        /// </summary>
        [TestMethod]
        public void Transaction_BatchOperations_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            // Prepare the batch data.
            var entities = new List<User>();
            for (int i = 1; i <= 5; i++)
            {
                entities.Add(CreateTestUser(i.ToString(), $"BatchTranUser{i}", i * 100, $"Batch{i}"));
            }

            Executor.Transaction(tran =>
            {
                // Perform the batch insert within the transaction.
                DbEngine.Insert(entities, 3, tran);
            });

            // Verify that every record was inserted.
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(5, count, "The transactional batch operation should insert five records.");

            // Verify the individual records.
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, "BatchTranUser%", WhereOperator.Like);
            var results = DbEngine.Select<User>(where, null);
            Assert.HasCount(5, results, "Five batch-inserted records should be returned.");
        }

        /// <summary>
        /// Tests mixed entity operations within a transaction.
        /// </summary>
        [TestMethod]
        public void Transaction_MixedOperations_Should_Succeed()
        {
            // Ensure that the table exists.
            DbEngine.Drop(UserTable.TableName);
            DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });

            // Insert initial data.
            DbEngine.Insert(CreateTestUser("1", "MixedUser1", 100, "Original1"));
            DbEngine.Insert(CreateTestUser("2", "MixedUser2", 200, "Original2"));

            Executor.Transaction(tran =>
            {
                // 1. Insert a new record.
                DbEngine.Insert(CreateTestUser("3", "MixedUser3", 300, "New3"), null, tran);

                // 2. Update an existing record.
                var updateEntity = CreateTestUser("1", "MixedUser1", 1000, "Updated1");
                DbEngine.Update(updateEntity, null, tran);

                // 3. Delete a record.
                DbEngine.Delete<User>("2", null, tran);

                // 4. Perform a batch insert.
                var batchEntities = new List<User>
                {
                    CreateTestUser("4", "MixedUser4", 400, "Batch4"),
                    CreateTestUser("5", "MixedUser5", 500, "Batch5")
                };
                DbEngine.Insert(batchEntities, 2, tran);
            });

            // Verify the final state.
            var count = DbEngine.Count(UserTable.TableName, new WhereConditions());
            Assert.AreEqual(4, count, "There should be four records (1 original + 1 new + 2 batch - 1 deleted).");

            // Verify the individual records.
            var user1 = DbEngine.SelectById<User>("1");
            Assert.IsNotNull(user1);
            Assert.AreEqual(1000, user1.IntData, "User1 should be updated.");
            Assert.AreEqual("Updated1", user1.SpecialString);

            var user2 = DbEngine.SelectById<User>("2");
            Assert.IsNull(user2, "User2 should be deleted.");

            var user3 = DbEngine.SelectById<User>("3");
            Assert.IsNotNull(user3);
            Assert.AreEqual(300, user3.IntData, "User3 should be inserted.");

            var user4 = DbEngine.SelectById<User>("4");
            Assert.IsNotNull(user4);
            Assert.AreEqual(400, user4.IntData, "User4 should be batch-inserted.");

            var user5 = DbEngine.SelectById<User>("5");
            Assert.IsNotNull(user5);
            Assert.AreEqual(500, user5.IntData, "User5 should be batch-inserted.");
        }
    }
}
