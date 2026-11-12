using CoddLoom.MariaDb;
using CoddLoom.MySql;
using CoddLoom.Oracle;
using CoddLoom.PostgreSql;
using CoddLoom.SqlServer;
using CoddLoom.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.UnitTest;

/// <summary>
/// Acceptance tests for issue #79: executor constructors perform no I/O.
/// Construction must not open connections, issue DDL, or create files or
/// directories; the removed behaviors are available through explicit calls.
/// </summary>
[TestClass]
public class ExecutorConstructionTest
{
    private const string UnreachableServer = "192.0.2.1";

    [TestMethod]
    public void Constructors_Succeed_WhenServersAreUnreachable()
    {
        // Every constructor below would have thrown a driver connection
        // exception at construction time in 0.1.0, because the base executor
        // probed the server and the MySQL/MariaDB convenience constructors
        // executed CREATE DATABASE before returning.
        new SqlServerExecutor($"Server={UnreachableServer};Database=test_db;User Id=sa;Password=pw;");
        new OracleExecutor($"Data Source={UnreachableServer}:1521/XE;User Id=test;Password=pw;");
        new PostgreSqlExecutor($"Host={UnreachableServer};Port=5432;Database=coddloom;Username=postgres;Password=postgres;");
        new MySqlExecutor($"Server={UnreachableServer};Database=test_db;Uid=root;Pwd=pw;");
        new MariaDbExecutor($"Server={UnreachableServer};Database=test_db;Uid=root;Pwd=pw;");

        new MySqlExecutor(UnreachableServer, "test_db", "root", "pw");
        new MariaDbExecutor(UnreachableServer, "test_db", "root", "pw");
        new PostgreSqlExecutor(UnreachableServer, "coddloom", "postgres", "postgres");
    }

    [TestMethod]
    public void Constructors_RejectNullOrEmptyConnectionString()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SqliteExecutor((string)null));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SqlServerExecutor(string.Empty));
    }

    [TestMethod]
    public void SqliteConstructor_CreatesNeitherDirectoryNorFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"coddloom-missing-{Guid.NewGuid():N}");
        try
        {
            new SqliteExecutor(directory, "app.db");

            Assert.IsFalse(Directory.Exists(directory), "The directory must not be created at construction.");
            Assert.IsFalse(File.Exists(Path.Combine(directory, "app.db")), "The database file must not be created at construction.");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SqliteConstructor_RejectsNullOrEmptyArguments()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SqliteExecutor("", "app.db"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SqliteExecutor("data", ""));
    }

    [TestMethod]
    public void ValidateConnection_OpensReachableDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"coddloom-probe-{Guid.NewGuid():N}.db");
        try
        {
            var executor = new SqliteExecutor($"Data Source={path};Version=3;Pooling=False;");

            executor.ValidateConnection();

            Assert.IsTrue(File.Exists(path), "Opening the connection must create the database file lazily.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void ValidateConnection_ThrowsDriverException_WhenUnreachable()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"coddloom-absent-{Guid.NewGuid():N}");
        var executor = new SqliteExecutor($"Data Source={Path.Combine(missingDirectory, "app.db")};Version=3;");

        Assert.ThrowsExactly<SQLiteException>(() => executor.ValidateConnection());
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_OpensReachableDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"coddloom-probe-async-{Guid.NewGuid():N}.db");
        try
        {
            var executor = new SqliteExecutor($"Data Source={path};Version=3;Pooling=False;");

            await executor.ValidateConnectionAsync();

            Assert.IsTrue(File.Exists(path), "Opening the connection must create the database file lazily.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public async Task ValidateConnectionAsync_ThrowsDriverException_WhenUnreachable()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"coddloom-absent-async-{Guid.NewGuid():N}");
        var executor = new SqliteExecutor($"Data Source={Path.Combine(missingDirectory, "app.db")};Version=3;");

        await Assert.ThrowsExactlyAsync<SQLiteException>(() => executor.ValidateConnectionAsync());
    }

    [TestMethod]
    public void EnsureDatabase_RejectsConnectionStringWithoutDatabase_BeforeConnecting()
    {
        // The validation must fail before any network access, so an unreachable
        // server in the connection string keeps this test deterministic.
        var mySql = new MySqlExecutor($"Server={UnreachableServer};Uid=root;Pwd=pw;");
        var mariaDb = new MariaDbExecutor($"Server={UnreachableServer};Uid=root;Pwd=pw;");

        Assert.ThrowsExactly<ArgumentException>(() => mySql.EnsureDatabase());
        Assert.ThrowsExactly<ArgumentException>(() => mariaDb.EnsureDatabase());
    }

    [TestMethod]
    public async Task EnsureDatabaseAsync_RejectsConnectionStringWithoutDatabase_BeforeConnecting()
    {
        // The validation must fail before any network access, so an unreachable
        // server in the connection string keeps this test deterministic.
        var mySql = new MySqlExecutor($"Server={UnreachableServer};Uid=root;Pwd=pw;");
        var mariaDb = new MariaDbExecutor($"Server={UnreachableServer};Uid=root;Pwd=pw;");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => mySql.EnsureDatabaseAsync());
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => mariaDb.EnsureDatabaseAsync());
    }

    [TestMethod]
    public async Task EnsureDatabaseAsync_ThrowsOperationCanceledException_BeforeConnecting()
    {
        // The token is observed before the connection attempt: against an
        // unreachable server, any real attempt would surface a driver
        // exception instead of the cancellation.
        var mySql = new MySqlExecutor($"Server={UnreachableServer};Database=test_db;Uid=root;Pwd=pw;");
        var mariaDb = new MariaDbExecutor($"Server={UnreachableServer};Database=test_db;Uid=root;Pwd=pw;");
        var cancelled = new System.Threading.CancellationToken(canceled: true);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => mySql.EnsureDatabaseAsync(cancelled));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => mariaDb.EnsureDatabaseAsync(cancelled));
    }

    [TestMethod]
    public void EnsureDatabase_SafelyQuotesTheDatabaseName()
    {
        Assert.AreEqual(
            "CREATE DATABASE IF NOT EXISTS `safe_db`",
            MySqlExecutor.BuildCreateDatabaseSql("safe_db"));
        Assert.AreEqual(
            "CREATE DATABASE IF NOT EXISTS `back``tick`",
            MySqlExecutor.BuildCreateDatabaseSql("back`tick"));

        Assert.AreEqual(
            "CREATE DATABASE IF NOT EXISTS `safe_db`",
            MariaDbExecutor.BuildCreateDatabaseSql("safe_db"));
        Assert.AreEqual(
            "CREATE DATABASE IF NOT EXISTS `back``tick`",
            MariaDbExecutor.BuildCreateDatabaseSql("back`tick"));
    }
}
