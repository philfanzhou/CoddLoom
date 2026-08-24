using CoddLoom.MariaDb;
using CoddLoom.MySql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class MySqlMariaDbDatabaseNameQuoteTest
{
    [TestMethod]
    public void PlainDatabaseName_IsWrappedInBackticks()
    {
        Assert.AreEqual("`appdb`", MySqlIdentifier.QuoteDatabase("appdb"));
        Assert.AreEqual("`appdb`", MariaDbIdentifier.QuoteDatabase("appdb"));
    }

    [TestMethod]
    [DataRow("app-db")]
    [DataRow("app db")]
    [DataRow("select")]
    public void HyphenSpaceAndReservedWordNames_ProduceLegalIdentifier(string database)
    {
        Assert.AreEqual("`" + database + "`", MySqlIdentifier.QuoteDatabase(database));
        Assert.AreEqual("`" + database + "`", MariaDbIdentifier.QuoteDatabase(database));
    }

    [TestMethod]
    public void EmbeddedBacktick_IsDoubledInsideIdentifier()
    {
        Assert.AreEqual("`a``b`", MySqlIdentifier.QuoteDatabase("a`b"));
        Assert.AreEqual("`a``b`", MariaDbIdentifier.QuoteDatabase("a`b"));
    }

    [TestMethod]
    public void BacktickCannotTerminateIdentifierOrAppendStatement()
    {
        var quoted = MySqlIdentifier.QuoteDatabase("x`; DROP DATABASE other; --");
        Assert.AreEqual("`x``; DROP DATABASE other; --`", quoted);
        Assert.IsTrue(quoted.StartsWith("`") && quoted.EndsWith("`"));

        var mariaQuoted = MariaDbIdentifier.QuoteDatabase("x`; DROP DATABASE other; --");
        Assert.AreEqual(quoted, mariaQuoted);
    }

    [TestMethod]
    public void BothProviders_ProduceIdenticalIdentifierForSameInput()
    {
        foreach (var database in new[] { "app", "app-db", "select", "a`b", "  ", "1db" })
        {
            Assert.AreEqual(MySqlIdentifier.QuoteDatabase(database),
                MariaDbIdentifier.QuoteDatabase(database),
                $"Providers diverged for input '{database}'.");
        }
    }
}
