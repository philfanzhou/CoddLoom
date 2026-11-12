using CoddLoom.MariaDb;
using CoddLoom.MySql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoddLoom.Tests.DbTest;

/// <summary>
/// Runs <c>EnsureDatabase()</c> against the real server selected by
/// <c>TestExecutorFactory</c>. The MySQL and MariaDB integration jobs execute
/// this end to end (server-only connection, DDL round trip); other providers
/// report inconclusive because the API does not apply to them.
/// </summary>
[TestClass]
[TestCategory("Database")]
public class EnsureDatabaseIntegrationTest
{
    [TestMethod]
    public void EnsureDatabase_SucceedsAgainstConfiguredServer()
    {
        switch (TestExecutorFactory.CurrentDatabaseType)
        {
            case TestExecutorFactory.DatabaseType.MySql:
                new MySqlExecutor(
                        TestExecutorFactory.GetConnectionString(TestExecutorFactory.DatabaseType.MySql))
                    .EnsureDatabase();
                break;
            case TestExecutorFactory.DatabaseType.MariaDB:
                new MariaDbExecutor(
                        TestExecutorFactory.GetConnectionString(TestExecutorFactory.DatabaseType.MariaDB))
                    .EnsureDatabase();
                break;
            default:
                Assert.Inconclusive("EnsureDatabase applies to the MySQL and MariaDB providers only.");
                return;
        }
    }
}
