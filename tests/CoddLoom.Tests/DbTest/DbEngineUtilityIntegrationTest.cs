using CoddLoom.Input;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;

namespace CoddLoom.Tests.DbTest;

[TestClass]
public class DbEngineUtilityIntegrationTest : TestBase
{
    [TestMethod]
    public void GetUtcTimeStamp_ReturnsUnixEpochMilliseconds()
    {
        var before = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        var timestamp = DbEngine.GetUtcTimeStamp();
        var after = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        Assert.IsGreaterThanOrEqualTo(before, timestamp);
        Assert.IsLessThanOrEqualTo(after, timestamp);
    }

    [TestMethod]
    public void ExecutorConvenienceApis_HandleDataSetsConnectionsAndFailures()
    {
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("1", "adapter"));

        var dataSet = Executor.Adapter($"SELECT {UserTable.Id} FROM {UserTable.TableName}");
        Assert.HasCount(1, dataSet.Tables);
        Assert.HasCount(1, dataSet.Tables[0].Rows);

        var connectionState = Executor.Execute(connection => connection.State);
        Assert.AreEqual(ConnectionState.Open, connectionState);
        Assert.AreEqual(0, Executor.TryExecute<int>(_ => throw new InvalidOperationException("expected")));
        var nullSql = TestExecutorFactory.CurrentDatabaseType == TestExecutorFactory.DatabaseType.Oracle
            ? "SELECT NULL FROM DUAL"
            : "SELECT NULL";
        Assert.AreEqual(0, Executor.Scalar(nullSql, _ => 1));
    }

    [TestMethod]
    public void SelectById_ReturnsDefaultForMissingRows()
    {
        Assert.IsNull(DbEngine.SelectById<DbCode.Entity.User>("missing"));
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
}
