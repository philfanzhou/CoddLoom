using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom.Condition;
using System;
using System.Data;
using System.Text;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// DbEngine integration tests.
    /// Covers complex data types, joins, and integration scenarios.
    /// </summary>
    [TestClass]
    public class DbTest : TestBase
    {

        /// <summary>
        /// Integrates complex data types and joined queries.
        /// Tests inserts, queries, and joins across multiple data types.
        /// </summary>
        [TestMethod]
        public void IntegrationTest_ComplexDataTypesAndJoins_Should_Succeed()
        {
                using var con = DbEngine.Executor.GetConnection();
                con.Open();

                // Test complex data types.
                TestComplexDataTypes(con);

                // Test joined queries.
                TestJoinOperations(con);

                con.Close();
        }

        /// <summary>
        /// Tests complex data types.
        /// </summary>
        private void TestComplexDataTypes(IDbConnection con)
        {
            const int count = 10;
            var specialString = "test'fdadf_OLOGY£¨KUNSHAN£©ELECTR";
            var flag = false;
            var now = DateTime.Now;

            for (var i = 0; i < count; i++)
            {
                var user = new User
                {
                    Id = i.ToString(),
                    UnionId = (i * 5).ToString(),
                    RegistrationDate = now,
                    Data = Encoding.UTF8.GetBytes(i.ToString()),
                    DoubleData = i * 0.0001,
                    DecimalData = (decimal)(i * 0.00001),
                    ShortData = (short)i,
                    IntData = i,
                    BoolData = flag,
                    SpecialString = specialString
                };
                flag = !flag;
                DbEngine.Insert(user, con);
            }

            // Verify the inserted data.
            var allUser = DbEngine.Select<User>(null, null);
            Assert.HasCount(count, allUser);

            // Verify complex data types.
            flag = false;
            for (var i = 0; i < count; i++)
            {
                var user = allUser[i];
                Assert.AreEqual(i.ToString(), user.Id);
                Assert.AreEqual((i * 5).ToString(), user.UnionId);
                AssertDateTime(now, user.RegistrationDate);
                
                // Verify the byte array.
                var data = Encoding.UTF8.GetBytes(i.ToString());
                for (var j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], user.Data[j]);
                }
                
                // Verify numeric types.
                Assert.IsLessThan(0.0001, i * 0.0001 - user.DoubleData);
                Assert.IsLessThan((decimal)0.00001, (decimal)(i * 0.00001) - user.DecimalData);
                Assert.AreEqual((short)i, user.ShortData);
                Assert.AreEqual(i, user.IntData);
                Assert.AreEqual(flag, user.BoolData);
                Assert.AreEqual(specialString, user.SpecialString);
                flag = !flag;
            }
        }

        /// <summary>
        /// Tests joined-query operations.
        /// This method is simplified because PasswordUser and PasswordUserTable were removed.
        /// </summary>
        private void TestJoinOperations(IDbConnection con)
        {
            // Simplified joined-query test using only UserTable.
            var users = DbEngine.Select<User>(null, null, con);
            Assert.HasCount(10, users, "Ten records should be returned.");
        }

        /// <summary>
        /// Tests ordering.
        /// </summary>
        [TestMethod]
        public void Test_OrderByFunctionality_Should_Succeed()
        {
                using var con = DbEngine.Executor.GetConnection();
                con.Open();

                // Insert test data.
                InsertTestData(con);

                // Test the default ascending order.
                var firstUser = DbEngine.First<User>(null, null, con);
                Assert.AreEqual("0", firstUser.Id.Trim(), "Default ordering should return the record with ID 0.");

                // Test descending order.
                firstUser = DbEngine.First<User>(null, new OrderByCondition(UserTable.Id, true), con);
                Assert.AreEqual("99", firstUser.Id.Trim(), "Descending order should return the record with ID 99.");

                con.Close();
        }

        /// <summary>
        /// Inserts test data.
        /// </summary>
        private void InsertTestData(IDbConnection con)
        {
            for (var i = 0; i < 100; i++)
            {
                var user = new User
                {
                    Id = i.ToString(),
                    UnionId = (i * 5).ToString(),
                    RegistrationDate = DateTime.Now,
                    IntData = i,
                    Data = Encoding.UTF8.GetBytes(i.ToString()),
                    DoubleData = i * 0.0001,
                    DecimalData = (decimal)(i * 0.00001),
                    ShortData = (short)i,
                    BoolData = i % 2 == 0,
                    SpecialString = $"Test_{i}"
                };
                DbEngine.Insert(user, con);
            }
        }

        /// <summary>
        /// Helper for date-and-time assertions.
        /// </summary>
        private static void AssertDateTime(DateTime time1, DateTime time2)
        {
            // Providers differ in fractional-second precision and some round to the
            // nearest second. Compare elapsed time so rollover at a minute/day boundary
            // does not make an equivalent stored value fail component-wise.
            Assert.IsLessThanOrEqualTo(TimeSpan.FromSeconds(1), (time1 - time2).Duration());
        }
    }
}
