using CoddLoom.Input;
using CoddLoom.Tests.DbCode.Tables;
using CoddLoom.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.DbTest;

/// <summary>
/// End-to-end coverage for the issue #85 true-async execution layer against
/// the real database selected by <see cref="TestExecutorFactory"/>: SQLite in
/// the default local matrix and the CI integration providers. The stored-
/// procedure methods have no portable cross-provider surface; their command
/// equivalence with the synchronous path is asserted by the fake-based
/// <c>DbExecutorAsyncTest</c>, and <c>EnsureDatabaseAsync</c> runs against the
/// real MySQL and MariaDB servers in <c>EnsureDatabaseIntegrationTest</c>.
/// </summary>
[TestClass]
[TestCategory("Database")]
public class DbExecutorAsyncIntegrationTest : TestBase
{
    private const string CountSql = $"SELECT COUNT(*) FROM {UserTable.TableName}";

    private const string OrderedIdsSql =
        $"SELECT {UserTable.Id} FROM {UserTable.TableName} ORDER BY {UserTable.Id}";

    [TestMethod]
    public async Task AsyncCoreMethods_RunEndToEndOnTheRealDatabase()
    {
        DbEngine.Drop(UserTable.TableName);
        DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("1", "asyncOne"));
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("2", "asyncTwo"));

        await Executor.ValidateConnectionAsync();

        Assert.AreEqual(2, await Executor.ScalarAsync(CountSql, System.Convert.ToInt32));
        CollectionAssert.AreEqual(new[] { "1", "2" },
            await Executor.ReaderAsync(OrderedIdsSql, record => record[UserTable.Id].ToString()));

        // A query without a matching row maps the database null to the default.
        Assert.AreEqual(0, await Executor.ScalarAsync(
            $"SELECT {UserTable.IntData} FROM {UserTable.TableName} WHERE {UserTable.Id} = 'absent'",
            System.Convert.ToInt32));

        // TransactionAsync commits the work of its awaited callback.
        await Executor.TransactionAsync(tran =>
        {
            DbEngine.Insert(CreateUser("3", "asyncThree"), null, tran);
            DbEngine.Insert(CreateUser("4", "asyncFour"), null, tran);
            return Task.CompletedTask;
        });
        Assert.AreEqual(4, await Executor.ScalarAsync(CountSql, System.Convert.ToInt32));

        // TransactionAsync<T> flows the transaction into an async command.
        Assert.AreEqual(4, await Executor.TransactionAsync(
            tran => Executor.ScalarAsync(CountSql, System.Convert.ToInt32, tran: tran)));

        // A failing callback rolls the transaction back and rethrows.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Executor.TransactionAsync(tran =>
        {
            DbEngine.Insert(CreateUser("5", "rolledBack"), null, tran);
            throw new InvalidOperationException("intentional rollback");
        }));
        Assert.AreEqual(4, await Executor.ScalarAsync(CountSql, System.Convert.ToInt32));

        // ExecuteAsync hands an open connection to the callback, and command
        // methods reuse it instead of opening their own.
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4" },
            await Executor.ExecuteAsync(
                connection => Executor.ReaderAsync(
                    OrderedIdsSql, record => record[UserTable.Id].ToString(), con: connection)));

        Assert.AreEqual(4, await Executor.NonQueryAsync($"DELETE FROM {UserTable.TableName}"));
        Assert.AreEqual(0, await Executor.ScalarAsync(CountSql, System.Convert.ToInt32));

        // A canceled token throws before the driver is contacted, so the SQL
        // text never executes on any provider.
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => Executor.ScalarAsync("SELECT 1", _ => 1, cancellationToken: new CancellationToken(true)));
    }

    [TestMethod]
    public async Task AsyncMethods_RunConcurrently_OnTheSameExecutorInstance()
    {
        DbEngine.Drop(UserTable.TableName);
        DbEngine.InitializeTable(new List<TableDefine> { new(typeof(UserTable)) });
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("1", "concurrent"));

        var scalarTasks = Enumerable.Range(0, 4)
            .Select(_ => Executor.ScalarAsync(CountSql, System.Convert.ToInt32))
            .ToArray();
        var readerTasks = Enumerable.Range(0, 4)
            .Select(_ => Executor.ReaderAsync(OrderedIdsSql, record => record[UserTable.Id].ToString()))
            .ToArray();

        // All eight calls are already running on the same executor instance;
        // one barrier proves they complete together without shared state.
        await Task.WhenAll(scalarTasks.Cast<Task>().Concat(readerTasks.Cast<Task>()).ToArray());

        Assert.HasCount(4, scalarTasks);
        foreach (var scalar in scalarTasks)
        {
            Assert.AreEqual(1, scalar.Result, "Every concurrent scalar read must observe the committed row.");
        }
        foreach (var reader in readerTasks)
        {
            CollectionAssert.AreEqual(new[] { "1" }, reader.Result,
                "Every concurrent reader must observe the committed row.");
        }
    }

    private static InputValues CreateRequiredRow(string id, string unionId)
    {
        return new InputValues()
            .Add(UserTable.Id, id)
            .Add(UserTable.UnionId, unionId)
            .Add(UserTable.DoubleData, 0d)
            .Add(UserTable.DecimalData, 0m)
            .Add(UserTable.ShortData, (short)0)
            .Add(UserTable.IntData, 0)
            .Add(UserTable.BoolData, false);
    }

    private static DbCode.Entity.User CreateUser(string id, string unionId)
    {
        return new DbCode.Entity.User
        {
            Id = id,
            UnionId = unionId,
            RegistrationDate = DateTime.Now,
            IntData = 0,
            Data = [1, 2, 3],
            DoubleData = 0d,
            DecimalData = 0m,
            ShortData = 0,
            BoolData = false,
            SpecialString = null
        };
    }
}
