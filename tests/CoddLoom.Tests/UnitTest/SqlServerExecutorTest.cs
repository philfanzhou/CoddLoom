using CoddLoom.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class SqlServerExecutorTest
{
    [TestMethod]
    public void Constructor_DoesNotTrustServerCertificateByDefault()
    {
        var parameter = typeof(SqlServerExecutor).GetConstructors().Single()
            .GetParameters().Single(item => item.Name == "trustServer");

        Assert.IsFalse((bool)parameter.DefaultValue);
        Assert.IsFalse(Parse(BuildConnectionString(
            "Server=example;Database=sample;Encrypt=True", trustServer: false)).TrustServerCertificate);
    }

    [TestMethod]
    public void ConnectionStringBuilder_HandlesAliasesAndExplicitTrustSetting()
    {
        var explicitFalse = BuildConnectionString(
            "Server=example;Database=sample;Trust Server Certificate=False", trustServer: false);
        Assert.IsFalse(Parse(explicitFalse).TrustServerCertificate);

        var forcedTrue = BuildConnectionString(
            "Server=example;Database=sample;Trust Server Certificate=False", trustServer: true);
        Assert.IsTrue(Parse(forcedTrue).TrustServerCertificate);

        var explicitTrue = BuildConnectionString(
            "Server=example;Database=sample;TrustServerCertificate=True", trustServer: false);
        Assert.IsTrue(Parse(explicitTrue).TrustServerCertificate);
    }

    private static SqlConnectionStringBuilder Parse(string connectionString) => new(connectionString);

    private static string BuildConnectionString(string connectionString, bool trustServer)
    {
        var method = typeof(SqlServerExecutor).GetMethod(
            "BuildConnectionString", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (string)method.Invoke(null, new object[] { connectionString, trustServer });
    }
}
