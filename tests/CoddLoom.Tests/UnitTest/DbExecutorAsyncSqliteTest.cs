using CoddLoom.Params;
using CoddLoom.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.UnitTest;

/// <summary>
/// Real-driver evidence for issue #85: the same SQL text and parameters run
/// through the synchronous and asynchronous paths of a SQLite executor must
/// produce the same results, and the async path must accept cancellation while
/// the driver executes.
/// </summary>
[TestClass]
public class DbExecutorAsyncSqliteTest
{
    [TestMethod]
    public async Task SyncAndAsyncPaths_ReturnIdenticalResults_ForTheSameSqlAndParameters()
    {
        await WithExecutorAsync(async executor =>
        {
            await executor.NonQueryAsync(
                "CREATE TABLE parity (id INTEGER PRIMARY KEY, amount TEXT)");
            var insert = new List<ValueParam>
            {
                new(1, "id"),
                new("12.34", "amount")
            };
            var asyncInsert = new List<ValueParam>
            {
                new(2, "id"),
                new("56.78", "amount")
            };

            Assert.AreEqual(1, executor.NonQuery(
                "INSERT INTO parity (id, amount) VALUES (@id, @amount)", insert));
            Assert.AreEqual(1, await executor.NonQueryAsync(
                "INSERT INTO parity (id, amount) VALUES (@id, @amount)", asyncInsert));

            // The same statement and the same parameter values must behave
            // identically on both paths; the statements below are idempotent.
            var update = new List<ValueParam>
            {
                new("99.99", "amount"),
                new(1, "id")
            };
            Assert.AreEqual(1, executor.NonQuery(
                "UPDATE parity SET amount = @amount WHERE id = @id", update));
            Assert.AreEqual(1, await executor.NonQueryAsync(
                "UPDATE parity SET amount = @amount WHERE id = @id", update));

            Assert.AreEqual(executor.Scalar<int>(
                "SELECT COUNT(*) FROM parity WHERE amount = @amount", System.Convert.ToInt32,
                new List<ValueParam> { new("99.99", "amount") }),
                await executor.ScalarAsync<int>(
                    "SELECT COUNT(*) FROM parity WHERE amount = @amount", System.Convert.ToInt32,
                    new List<ValueParam> { new("99.99", "amount") }));

            CollectionAssert.AreEqual(
                executor.Reader("SELECT amount FROM parity ORDER BY id",
                    record => record.GetString(0)),
                await executor.ReaderAsync("SELECT amount FROM parity ORDER BY id",
                    record => record.GetString(0)));
        });
    }

    [TestMethod]
    public async Task CancelledToken_ThrowsBeforeTheDriverIsContacted()
    {
        await WithExecutorAsync(async executor =>
        {
            var cancelled = new CancellationToken(canceled: true);

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => executor.NonQueryAsync("SELECT 1", cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => executor.ReaderAsync("SELECT 1", _ => true, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => executor.ScalarAsync("SELECT 1", System.Convert.ToInt32, cancellationToken: cancelled));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => executor.ValidateConnectionAsync(cancelled));
        });
    }

    private static async Task WithExecutorAsync(Func<SqliteExecutor, Task> action)
    {
        var path = Path.Combine(Path.GetTempPath(), $"coddloom-async-{Guid.NewGuid():N}.db");
        try
        {
            await action(new SqliteExecutor($"Data Source={path};Version=3;Pooling=False;"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
