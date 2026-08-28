using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class DbEngineObsoleteApiTest
{
    private const string ExpectedWarning =
        "This check-then-return API cannot guarantee uniqueness under concurrency. " +
        "Prefer a database identity/sequence, UUID, or unique-constraint-protected inserts with retry handling.";

    [TestMethod]
    [DataRow(0, "000")]
    [DataRow(999, "999")]
    public void GenerateTimeIdSuffix_FormatsBoundaryAsThreeAsciiDigits(int suffix, string expected)
    {
        Assert.AreEqual(expected, DbEngine.FormatTimeIdSuffix(suffix));
    }

    [TestMethod]
    [DataRow("GenerateId")]
    [DataRow("GenerateMaxId")]
    [DataRow("GenerateTimeId")]
    [DataRow("GenerateUtcTimeId")]
    public void ClientIdGenerationApi_HasActionableObsoleteWarning(string methodName)
    {
        var methods = typeof(DbEngine).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == methodName)
            .ToArray();

        Assert.IsNotEmpty(methods);
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<ObsoleteAttribute>();
            Assert.IsNotNull(attribute, $"{methodName} overload is missing [Obsolete].");
            Assert.AreEqual(ExpectedWarning, attribute.Message);
            Assert.IsFalse(attribute.IsError);
        }
    }
}
