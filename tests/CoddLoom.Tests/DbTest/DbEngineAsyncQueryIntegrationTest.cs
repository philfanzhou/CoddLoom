using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Model;
using CoddLoom.Params;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode;
using CoddLoom.Tests.DbCode.Entity;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// End-to-end coverage for the issue #87 asynchronous query family of
    /// <see cref="DbEngine"/> against the real database selected by
    /// <see cref="TestExecutorFactory"/>: SQLite in the default local matrix and
    /// the CI integration providers. Every test is the asynchronous twin of an
    /// existing synchronous query test; the byte-level sync/async SQL parity
    /// contract is asserted separately by the fake-based
    /// <c>DbEngineAsyncQuerySqlParityTest</c>.
    /// </summary>
    [TestClass]
    public class DbEngineAsyncQueryIntegrationTest : TestBase
    {
        private const string JoinProfileTableName = "AsyncJoinProfileTable";

        /// <summary>
        /// Creates a test user entity with unique values per call.
        /// </summary>
        private static User CreateUser(string id, string unionId, int intData)
        {
            return new User
            {
                Id = id,
                UnionId = unionId,
                RegistrationDate = DateTime.Now,
                IntData = intData,
                Data = new byte[] { 1, 2, 3 },
                DoubleData = 123.45,
                DecimalData = 123.456m,
                ShortData = (short)123,
                BoolData = true,
                SpecialString = null
            };
        }

        private static WhereConditions MarkerWhere(string marker)
        {
            var where = new WhereConditions();
            where.Add(UserTable.UnionId, marker + "%", WhereOperator.Like);
            return where;
        }

        /// <summary>
        /// Inserts <paramref name="count"/> users whose UnionId embeds the marker and
        /// whose IntData runs from 1 to <paramref name="count"/>, then returns the
        /// shared inputs of the dataset.
        /// </summary>
        private async Task<int> InsertUsersAsync(int count, string marker)
        {
            var entities = new List<User>();
            for (var i = 1; i <= count; i++)
            {
                entities.Add(CreateUser(i.ToString(), $"{marker}{i}", i));
            }

            Assert.AreEqual(count, await DbEngine.InsertAsync(entities, 5),
                "The dataset must be fully inserted before the queries run.");
            return count;
        }

        [TestMethod]
        public async Task AsyncQueryFamily_AllFourteenMethods_RunEndToEndOnTheRealDatabase()
        {
            const string marker = "AsyncQueryUser";
            await InsertUsersAsync(7, marker);
            var where = MarkerWhere(marker);
            var orderByIntData = new OrderByCondition(UserTable.IntData);

            // SelectAsync 1/6: convertor + table + where + orderBy + columns.
            var columns = new ColumnParam();
            columns.AddSelect(UserTable.UnionId);
            columns.AddSelect(UserTable.IntData);
            var unionIds = await DbEngine.SelectAsync(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderByIntData, columns);
            Assert.HasCount(7, unionIds, "All seven rows should be selected.");
            Assert.AreEqual(marker + "1", unionIds[0], "Ordering by IntData should put row 1 first.");

            // SelectAsync 2/6: convertor + table + where + orderBy (no columns).
            var intDataValues = await DbEngine.SelectAsync(
                record => System.Convert.ToInt32(record[UserTable.IntData]), UserTable.TableName, where,
                orderByIntData);
            Assert.HasCount(7, intDataValues);
            Assert.AreEqual(7, intDataValues[6], "The last row should carry IntData 7.");

            // SelectAsync 3/6: entity + where + orderBy + columns.
            var partialEntities = await DbEngine.SelectAsync<User>(where, orderByIntData, columns);
            Assert.HasCount(7, partialEntities);
            Assert.AreEqual(marker + "1", partialEntities[0].UnionId);
            Assert.AreEqual(1, partialEntities[0].IntData);

            // SelectAsync 4/6: entity + where + orderBy (no columns).
            var entities = await DbEngine.SelectAsync<User>(where, orderByIntData);
            Assert.HasCount(7, entities);
            Assert.AreEqual(marker + "7", entities[6].UnionId);

            // SelectAsync 5/6 and 6/6: the JOIN overloads, executed as a real join
            // against a second physical table.
            try
            {
                await DbEngine.InitializeTableAsync([new TableDefine(typeof(JoinProfileTable))]);
                var profileRows = new List<InputValues>();
                for (var i = 1; i <= 7; i++)
                {
                    profileRows.Add(new InputValues()
                        .Add(JoinProfileTable.Id, Guid.NewGuid().ToString())
                        .Add(JoinProfileTable.UserId, i.ToString())
                        .Add(JoinProfileTable.Value, $"Profile{i}"));
                }

                Assert.AreEqual(7, await DbEngine.InsertAsync(JoinProfileTable.TableName, profileRows, 5));

                var join = new JoinConditions(
                    UserTable.TableName, UserTable.Id,
                    JoinProfileTable.TableName, JoinProfileTable.UserId);
                var joinOrderBy = new OrderByCondition(UserTable.IntData);

                // 5/6: convertor + join + where + orderBy + columns.
                var joinColumns = new ColumnParam();
                joinColumns.AddSelect($"{UserTable.TableName}.{UserTable.UnionId}");
                joinColumns.AddSelect($"{JoinProfileTable.TableName}.{JoinProfileTable.Value}");
                var joined = await DbEngine.SelectAsync(
                    record => (UnionId: record[0].ToString(), Profile: record[1].ToString()),
                    join, where, joinOrderBy, joinColumns);
                Assert.HasCount(7, joined, "The join should match all seven profile rows.");
                Assert.AreEqual((marker + "3", "Profile3"), joined[2],
                    "The joined row should pair the user with its profile.");

                // 6/6: convertor + join + where + orderBy (no columns).
                var joinedCount = await DbEngine.SelectAsync(
                    record => record[0].ToString(), join, where, joinOrderBy);
                Assert.HasCount(7, joinedCount);
            }
            finally
            {
                await DbEngine.DropAsync(JoinProfileTable.TableName);
            }

            // FirstAsync 1/3: convertor + table + where + orderBy + columns.
            Assert.AreEqual(marker + "1", await DbEngine.FirstAsync(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderByIntData, columns));

            // FirstAsync 2/3: convertor + table + where + orderBy (no columns).
            Assert.AreEqual(marker + "1", await DbEngine.FirstAsync(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderByIntData));

            // FirstAsync 3/3: entity + where + orderBy.
            var firstEntity = await DbEngine.FirstAsync<User>(where, orderByIntData);
            Assert.IsNotNull(firstEntity);
            Assert.AreEqual(marker + "1", firstEntity.UnionId);

            // FirstAsync without a match returns default(T), like First.
            var absentWhere = new WhereConditions();
            absentWhere.Add(UserTable.UnionId, "AsyncQueryAbsent%");
            Assert.IsNull(await DbEngine.FirstAsync<User>(absentWhere, orderByIntData),
                "A non-matching condition should map to null.");

            // SelectByIdAsync: present and absent primary keys.
            var byId = await DbEngine.SelectByIdAsync<User>("3");
            Assert.IsNotNull(byId);
            Assert.AreEqual(marker + "3", byId.UnionId);
            Assert.IsNull(await DbEngine.SelectByIdAsync<User>("absent-id"),
                "An absent primary key should map to null.");

            // PageSelectAsync 1/4: convertor + table + where + orderBy + columns + page.
            var page2 = await DbEngine.PageSelectAsync(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderByIntData, columns, new PageParam { PageNumber = 2, PageSize = 3 });
            Assert.AreEqual(2, page2.PageNumber, "The result should echo the requested page number.");
            Assert.AreEqual(7, page2.TotalCount, "The total count should be seven.");
            Assert.AreEqual(3, page2.TotalPage, "Seven rows at three per page should produce three pages.");
            CollectionAssert.AreEqual(new[] { marker + "4", marker + "5", marker + "6" }, page2.Items,
                "The second page should carry rows 4 through 6.");

            // PageSelectAsync 2/4: convertor + table + where + orderBy + page (no columns).
            var page1 = await DbEngine.PageSelectAsync(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderByIntData, new PageParam { PageNumber = 1, PageSize = 3 });
            CollectionAssert.AreEqual(new[] { marker + "1", marker + "2", marker + "3" }, page1.Items);

            // PageSelectAsync 3/4: entity + where + orderBy + columns + page (final page).
            var lastPage = await DbEngine.PageSelectAsync<User>(where, orderByIntData, columns,
                new PageParam { PageNumber = 3, PageSize = 3 });
            Assert.HasCount(1, lastPage.Items, "The final page should carry the single remainder row.");
            Assert.AreEqual(marker + "7", lastPage.Items[0].UnionId);

            // PageSelectAsync 4/4: entity + where + orderBy + page (no columns, empty result).
            var emptyPage = await DbEngine.PageSelectAsync<User>(absentWhere, orderByIntData,
                new PageParam { PageNumber = 1, PageSize = 3 });
            Assert.AreEqual(0, emptyPage.TotalCount);
            Assert.AreEqual(0, emptyPage.TotalPage);
            Assert.IsEmpty(emptyPage.Items);
        }

        [TestMethod]
        public async Task PageSelectAsync_MetadataMatchesSyncOutResults_ForEveryOverloadAndBoundary()
        {
            const string marker = "AsyncParityUser";
            await InsertUsersAsync(10, marker);
            var where = MarkerWhere(marker);
            var ascending = new OrderByCondition(UserTable.IntData);
            var descending = new OrderByCondition(UserTable.IntData, true);
            var columns = new ColumnParam();
            columns.AddSelect(UserTable.UnionId);

            // Every overload, on page boundaries that exercise the carry rule
            // (10 rows at 3 per page -> 4 pages) and the exact fit (10 at 5 -> 2).
            var cases = new (int PageNumber, int PageSize)[]
            {
                (1, 3), (2, 3), (4, 3), (1, 5), (2, 5), (1, 10), (3, 4)
            };

            foreach (var (pageNumber, pageSize) in cases)
            {
                var page = new PageParam { PageNumber = pageNumber, PageSize = pageSize };

                // Overload 1: convertor + table + orderBy + columns + page.
                var syncByConvertor = DbEngine.PageSelect(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    ascending, columns, page, out var syncTotalPages, out var syncTotalCount);
                var asyncByConvertor = await DbEngine.PageSelectAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    ascending, columns, page);
                Assert.AreEqual(syncTotalCount, asyncByConvertor.TotalCount,
                    $"Convertor overload total count must match on page {pageNumber}/{pageSize}.");
                Assert.AreEqual(syncTotalPages, asyncByConvertor.TotalPage,
                    $"Convertor overload total pages must match on page {pageNumber}/{pageSize}.");
                CollectionAssert.AreEqual(syncByConvertor, asyncByConvertor.Items,
                    $"Convertor overload items must match on page {pageNumber}/{pageSize}.");

                // Overload 2: convertor + table + orderBy + page (no columns).
                var syncNoColumns = DbEngine.PageSelect(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    ascending, page, out var syncNoColumnsPages, out var syncNoColumnsCount);
                var asyncNoColumns = await DbEngine.PageSelectAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    ascending, page);
                Assert.AreEqual(syncNoColumnsCount, asyncNoColumns.TotalCount);
                Assert.AreEqual(syncNoColumnsPages, asyncNoColumns.TotalPage);
                CollectionAssert.AreEqual(syncNoColumns, asyncNoColumns.Items);

                // Overload 3: entity + orderBy + columns + page.
                var syncEntity = DbEngine.PageSelect<User>(where, ascending, columns,
                    page, out var syncEntityPages, out var syncEntityCount);
                var asyncEntity = await DbEngine.PageSelectAsync<User>(where, ascending, columns, page);
                Assert.AreEqual(syncEntityCount, asyncEntity.TotalCount);
                Assert.AreEqual(syncEntityPages, asyncEntity.TotalPage);
                CollectionAssert.AreEqual(
                    syncEntity.Select(user => user.UnionId).ToList(),
                    asyncEntity.Items.Select(user => user.UnionId).ToList(),
                    "Entity overload items must match; entities have reference equality.");

                // Overload 4: entity + orderBy + page (no columns), descending order.
                var syncDescending = DbEngine.PageSelect<User>(where, descending,
                    page, out var syncDescendingPages, out var syncDescendingCount);
                var asyncDescending = await DbEngine.PageSelectAsync<User>(where, descending, page);
                Assert.AreEqual(syncDescendingCount, asyncDescending.TotalCount);
                Assert.AreEqual(syncDescendingPages, asyncDescending.TotalPage);
                CollectionAssert.AreEqual(
                    syncDescending.Select(user => user.UnionId).ToList(),
                    asyncDescending.Items.Select(user => user.UnionId).ToList(),
                    "Descending entity overload items must match; entities have reference equality.");
            }

            // The carry rule stays identical: 10 rows at 3 per page is 4 pages, and
            // the exact fit of 10 at 10 is a single page.
            var carryPage = await DbEngine.PageSelectAsync<User>(where, ascending,
                new PageParam { PageNumber = 1, PageSize = 3 });
            Assert.AreEqual(4, carryPage.TotalPage,
                "Ten rows at three per page should carry into a fourth page.");
            var exactFitPage = await DbEngine.PageSelectAsync<User>(where, ascending,
                new PageParam { PageNumber = 1, PageSize = 10 });
            Assert.AreEqual(1, exactFitPage.TotalPage,
                "An exact-fit page size should produce exactly one page.");
        }

        [TestMethod]
        public async Task PageSelectAsync_WithoutPageParam_SkipsCountLikeSyncVersion()
        {
            const string marker = "AsyncUnpaged";
            await InsertUsersAsync(5, marker);
            var where = MarkerWhere(marker);
            var orderBy = new OrderByCondition(UserTable.IntData);

            // A null PageParam runs no COUNT and returns unpaged rows; the sync
            // version reports zeros through its out parameters, the async version
            // through the zeroed PageResult metadata.
            var syncRows = DbEngine.PageSelect(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderBy, null, null, out var syncTotalPages, out var syncTotalCount);
            var asyncResult = await DbEngine.PageSelectAsync(
                record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                orderBy, columns: null, pageParam: null);

            Assert.AreEqual(0, syncTotalCount);
            Assert.AreEqual(0, syncTotalPages);
            Assert.AreEqual(0, asyncResult.TotalCount,
                "A null page param must skip the COUNT, so the total count stays zero.");
            Assert.AreEqual(0, asyncResult.TotalPage,
                "A null page param must leave the total page count at zero.");
            Assert.AreEqual(0, asyncResult.PageNumber,
                "A null page param has no page number to echo.");
            CollectionAssert.AreEqual(syncRows, asyncResult.Items,
                "The unpaged rows must match the synchronous version.");
            Assert.HasCount(5, asyncResult.Items);
        }

        [TestMethod]
        public async Task PageSelectAsync_GroupedResults_CountsGroupsInsteadOfFirstGroupSize()
        {
            // Async twin of PageSelect_GroupedResults_CountsGroupsInsteadOfFirstGroupSize.
            var marker = $"AsyncGrouped{Guid.NewGuid():N}";
            for (var i = 1; i <= 3; i++)
            {
                await DbEngine.InsertAsync(UserTable.TableName, CreateRequiredValues(marker + i, 1));
            }

            await DbEngine.InsertAsync(UserTable.TableName, CreateRequiredValues(marker + "4", 2));

            var where = new WhereConditions(UserTable.UnionId, marker + "%", WhereOperator.Like);
            var columns = new ColumnParam().AddSelect(UserTable.IntData, groupBy: true);
            var page = new PageParam { PageNumber = 1, PageSize = 1 };
            var result = await DbEngine.PageSelectAsync(
                record => System.Convert.ToInt32(record[0]), UserTable.TableName, where,
                new OrderByCondition(UserTable.IntData), columns, page);

            Assert.AreEqual(2, result.TotalCount,
                "The COUNT must count the two groups, not the first group's size.");
            Assert.AreEqual(2, result.TotalPage);
            Assert.HasCount(1, result.Items);
        }

        [TestMethod]
        public async Task PreCancelledToken_ThrowsBeforeAnyDatabaseCall_ForEveryQueryMethod()
        {
            const string marker = "AsyncQueryCancel";
            await InsertUsersAsync(2, marker);
            var cancelled = new CancellationToken(canceled: true);
            var where = MarkerWhere(marker);
            var orderBy = new OrderByCondition(UserTable.IntData);
            var columns = new ColumnParam();
            columns.AddSelect(UserTable.UnionId);
            var page = new PageParam { PageNumber = 1, PageSize = 1 };
            var join = new JoinConditions(
                UserTable.TableName, UserTable.Id, UserTable.TableName, UserTable.Id);

            // SelectAsync, all six overloads.
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    orderBy, columns, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    orderBy, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectAsync<User>(where, orderBy, columns, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectAsync<User>(where, orderBy, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectAsync(
                    record => record[0].ToString(), join, where, orderBy, columns,
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectAsync(
                    record => record[0].ToString(), join, where, orderBy,
                    cancellationToken: cancelled));

            // FirstAsync, all three overloads.
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.FirstAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    orderBy, columns, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.FirstAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    orderBy, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.FirstAsync<User>(where, orderBy, cancellationToken: cancelled));

            // SelectByIdAsync.
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.SelectByIdAsync<User>("1", cancellationToken: cancelled));

            // PageSelectAsync, all four overloads.
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.PageSelectAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    orderBy, columns, page, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.PageSelectAsync(
                    record => record[UserTable.UnionId].ToString(), UserTable.TableName, where,
                    orderBy, page, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.PageSelectAsync<User>(where, orderBy, columns, page,
                    cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => DbEngine.PageSelectAsync<User>(where, orderBy, page,
                    cancellationToken: cancelled));

            // Nothing was disturbed: the two rows are still readable and the
            // table is still queryable.
            Assert.AreEqual(2, await DbEngine.CountAsync(UserTable.TableName, where),
                "Canceled queries must leave the data untouched.");
        }

        private static InputValues CreateRequiredValues(string unionId, int intData)
        {
            return new InputValues()
                .Add(UserTable.Id, Guid.NewGuid().ToString())
                .Add(UserTable.UnionId, unionId)
                .Add(UserTable.DoubleData, 1d)
                .Add(UserTable.DecimalData, 1m)
                .Add(UserTable.ShortData, (short)1)
                .Add(UserTable.IntData, intData)
                .Add(UserTable.BoolData, false);
        }

        /// <summary>
        /// A second physical table so the JOIN overloads execute a real join
        /// instead of a self-join over the shared user table.
        /// </summary>
        private static class JoinProfileTable
        {
            [DbTableName]
            public const string TableName = JoinProfileTableName;

            [DbPrimaryKey(Type = DbType.String)]
            public const string Id = "id";

            [DbColumnString(AllowEmpty = false)]
            public const string UserId = "userId";

            [DbColumnString(AllowEmpty = false)]
            public const string Value = "value";
        }
    }
}
