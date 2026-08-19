using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom;
using CoddLoom.Condition;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CoddLoom.Tests.DbCode;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;
using CoddLoom.Tests.DbTest;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Tests DbEngine paginated queries.
    /// Covers pagination behavior and result metadata.
    /// </summary>
    [TestClass]
    public class DbEnginePaginationTest : TestBase
    {
        

        /// <summary>
        /// Tests a basic paginated query.
        /// </summary>
        [TestMethod]
        public void PageSelect_BasicPagination_Should_Succeed()
        {

                // Prepare ten test records.
                var entities = new List<User>();
                for (int i = 1; i <= 10; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"PaginationUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"Pagination{i}"
                    });
                }
                DbEngine.Insert(entities, 5); // Batch insert.

                // Query the first page with three records per page.
                var pageParam = new PageParam { PageNumber = 1, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "PaginationUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false); // ASC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(10, totalCount, "The total record count should be ten.");
                Assert.AreEqual(4, totalPages, "Ten records at three per page should produce four pages.");
                Assert.HasCount(3, result, "The first page should contain three records.");
                Assert.AreEqual("PaginationUser1", result[0].UnionId, "The first record should be PaginationUser1.");
                Assert.AreEqual("PaginationUser2", result[1].UnionId, "The second record should be PaginationUser2.");
                Assert.AreEqual("PaginationUser3", result[2].UnionId, "The third record should be PaginationUser3.");
        }

        /// <summary>
        /// Tests the second page of a paginated query.
        /// </summary>
        [TestMethod]
        public void PageSelect_SecondPage_Should_Succeed()
        {

                // Prepare ten test records.
                var entities = new List<User>();
                for (int i = 1; i <= 10; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"SecondPageUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"SecondPage{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // Query the second page with three records per page.
                var pageParam = new PageParam { PageNumber = 2, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "SecondPageUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false); // ASC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(10, totalCount, "The total record count should be ten.");
                Assert.AreEqual(4, totalPages, "The total page count should be four.");
                Assert.HasCount(3, result, "The second page should contain three records.");
                Assert.AreEqual("SecondPageUser4", result[0].UnionId, "The first record should be SecondPageUser4.");
                Assert.AreEqual("SecondPageUser5", result[1].UnionId, "The second record should be SecondPageUser5.");
                Assert.AreEqual("SecondPageUser6", result[2].UnionId, "The third record should be SecondPageUser6.");
        }

        /// <summary>
        /// Tests the final page of a paginated query.
        /// </summary>
        [TestMethod]
        public void PageSelect_LastPage_Should_Succeed()
        {

                // Prepare ten test records.
                var entities = new List<User>();
                for (int i = 1; i <= 10; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"LastPageUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"LastPage{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // Query the fourth and final page with three records per page.
                var pageParam = new PageParam { PageNumber = 4, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "LastPageUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false); // ASC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(10, totalCount, "The total record count should be ten.");
                Assert.AreEqual(4, totalPages, "The total page count should be four.");
                Assert.HasCount(1, result, "The final page should contain one record.");
                Assert.AreEqual("LastPageUser10", result[0].UnionId, "The final record should be LastPageUser10.");
        }

        /// <summary>
        /// Tests a paginated query with no results.
        /// </summary>
        [TestMethod]
        public void PageSelect_EmptyResult_Should_Succeed()
        {

                // Insert no data so the paginated query is empty.
                var pageParam = new PageParam { PageNumber = 1, PageSize = 5 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "NonExistentUser");
                var orderBy = new OrderByCondition(UserTable.IntData, false);

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(0, totalCount, "The total record count should be zero.");
                Assert.AreEqual(0, totalPages, "The total page count should be zero.");
                Assert.IsEmpty(result, "The result should be empty.");
        }

        /// <summary>
        /// Tests a paginated query with a large page size.
        /// </summary>
        [TestMethod]
        public void PageSelect_LargePageSize_Should_Succeed()
        {

                // Prepare five test records.
                var entities = new List<User>();
                for (int i = 1; i <= 5; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"LargePageUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"LargePage{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // Use a page size larger than the total record count.
                var pageParam = new PageParam { PageNumber = 1, PageSize = 10 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "LargePageUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false);

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(5, totalCount, "The total record count should be five.");
                Assert.AreEqual(1, totalPages, "The total page count should be one.");
                Assert.HasCount(5, result, "All five records should be returned.");
        }

        /// <summary>
        /// Tests a paginated query in descending order.
        /// </summary>
        [TestMethod]
        public void PageSelect_DescendingOrder_Should_Succeed()
        {

                // Prepare five test records.
                var entities = new List<User>();
                for (int i = 1; i <= 5; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"DescUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"Desc{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // Query in descending order.
                var pageParam = new PageParam { PageNumber = 1, PageSize = 3 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "DescUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, true); // DESC

                var result = DbEngine.PageSelect<User>(where, orderBy, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(5, totalCount, "The total record count should be five.");
                Assert.AreEqual(2, totalPages, "The total page count should be two.");
                Assert.HasCount(3, result, "The first page should contain three records.");
                Assert.AreEqual("DescUser5", result[0].UnionId, "The first record should be DescUser5 in descending order.");
                Assert.AreEqual("DescUser4", result[1].UnionId, "The second record should be DescUser4.");
                Assert.AreEqual("DescUser3", result[2].UnionId, "The third record should be DescUser3.");
        }

        /// <summary>
        /// Tests a paginated query with column selection.
        /// </summary>
        [TestMethod]
        public void PageSelect_WithColumnSelection_Should_Succeed()
        {

                // Prepare three test records.
                var entities = new List<User>();
                for (int i = 1; i <= 3; i++)
                {
                    entities.Add(new User
                    {
                        Id = i.ToString(),
                        UnionId = $"ColumnUser{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        IntData = i,
                        Data = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) },
                        DoubleData = i * 10.5,
                        DecimalData = i * 1.23m,
                        ShortData = (short)i,
                        BoolData = i % 2 == 0,
                        SpecialString = $"Column{i}"
                    });
                }
                DbEngine.Insert(entities, 5);

                // Query only selected columns.
                var pageParam = new PageParam { PageNumber = 1, PageSize = 2 };
                var where = new WhereConditions();
                where.Add(UserTable.UnionId, "ColumnUser%", WhereOperator.Like);
                var orderBy = new OrderByCondition(UserTable.IntData, false);
                var columns = new ColumnParam();
                columns.AddSelect(UserTable.UnionId);
                columns.AddSelect(UserTable.IntData);

                var result = DbEngine.PageSelect<User>(where, orderBy, columns, pageParam, out var totalPages, out var totalCount);

                // Verify the result.
                Assert.AreEqual(3, totalCount, "The total record count should be three.");
                Assert.AreEqual(2, totalPages, "The total page count should be two.");
                Assert.HasCount(2, result, "The first page should contain two records.");
                Assert.AreEqual("ColumnUser1", result[0].UnionId, "The first record's UnionId should be ColumnUser1.");
                Assert.AreEqual(1, result[0].IntData, "The first record's IntData should be one.");
                Assert.AreEqual("ColumnUser2", result[1].UnionId, "The second record's UnionId should be ColumnUser2.");
                Assert.AreEqual(2, result[1].IntData, "The second record's IntData should be two.");
        }
    }
}
