using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Linq;
using CoddLoom.Table;

namespace CoddLoom.Tests.DbTest;

[TestClass]
public class DbEngineUtilityIntegrationTest : TestBase
{
    [TestMethod]
    public void GenerateId_RetriesCollisionsAndReportsExhaustion()
    {
        DbEngine.Insert(UserTable.TableName, CreateRequiredRow("1", "existing"));

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID API.
        var generated = DbEngine.GenerateId<string>(UserTable.TableName, UserTable.Id,
            current => current == null ? "1" : (int.Parse(current) + 1).ToString());

        Assert.AreEqual("2", generated);
        var exception = Assert.ThrowsExactly<Exception>(() =>
            DbEngine.GenerateId<string>(UserTable.TableName, UserTable.Id, _ => "1", tryCount: 2));
#pragma warning restore CS0618
        StringAssert.Contains(exception.Message, "Generate new UserTable.id ID failed");
    }

    [TestMethod]
    public void GenerateMaxAndTimeIds_ReturnExpectedShapes()
    {
        var numericTable = new TableDefine(typeof(NumericIdTable));
        DbEngine.InitializeTable([numericTable]);
        try
        {
            DbEngine.Insert(NumericIdTable.TableName, new InputValues().Add(NumericIdTable.Id, 3L));
#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID APIs.
            Assert.AreEqual(4L, DbEngine.GenerateMaxId(NumericIdTable.TableName, NumericIdTable.Id));
#pragma warning restore CS0618
        }
        finally
        {
            DbEngine.Drop(NumericIdTable.TableName);
        }

#pragma warning disable CS0618 // Exercise the retained behavior of the obsolete ID APIs.
        var timeId = DbEngine.GenerateTimeId(UserTable.TableName, UserTable.Id,
            () => new DateTime(2024, 2, 3, 4, 5, 6));
        StringAssert.StartsWith(timeId, "240203040506");
        Assert.HasCount(15, timeId);

        var utcId = DbEngine.GenerateUtcTimeId(UserTable.TableName, UserTable.Id);
#pragma warning restore CS0618
        Assert.HasCount(15, utcId);

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

    private static class NumericIdTable
    {
        [DbTableName] internal const string TableName = "NumericIdTable";
        [DbPrimaryKey(Type = DbType.Int64)] internal const string Id = "id";
    }
}
