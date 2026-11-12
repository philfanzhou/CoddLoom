using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Sql;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoddLoom.Tests.UnitTest;

/// <summary>
/// Acceptance tests for issue #81: rendering WHERE conditions never mutates the
/// caller's objects. SQL building and command parameter materialization are
/// deterministic snapshots, so a condition tree can be rendered repeatedly and
/// concurrently.
/// </summary>
[TestClass]
public class WhereConditionsSnapshotTest
{
    private readonly SqlBuilder _builder = new();

    [TestMethod]
    public void Rendering_Twice_ProducesIdenticalSql_AndNeverRewritesTheConditions()
    {
        var where = BuildSampleConditions();

        var first = _builder.Select("sample", where);
        var second = _builder.Select("sample", where);

        Assert.AreEqual(first, second);
        Assert.AreEqual("abc", where.Parameters.Single(parameter => parameter.Column == "name").Value);
        Assert.AreEqual(1, where.Parameters.Single(parameter => parameter.Column == "a").Value);
    }

    [TestMethod]
    public void Getter_IsRepeatable_AndReturnsFreshInstances()
    {
        var where = BuildSampleConditions();

        var first = where.Parameters.ToList();
        var second = where.Parameters.ToList();

        CollectionAssert.AreEqual(
            first.Select(parameter => parameter.ParamName).ToArray(),
            second.Select(parameter => parameter.ParamName).ToArray());
        CollectionAssert.AreEqual(
            first.Select(parameter => parameter.Value).ToArray(),
            second.Select(parameter => parameter.Value).ToArray());
        Assert.IsFalse(ReferenceEquals(first[0], second[0]), "Snapshots must not be shared instances.");
    }

    [TestMethod]
    public void ConcurrentRendering_IsConsistent()
    {
        var where = BuildSampleConditions();
        var sqlResults = new ConcurrentBag<string>();
        var nameResults = new ConcurrentBag<string[]>();

        Parallel.For(0, 16, _ =>
        {
            var sql = _builder.Select("sample", where);
            sqlResults.Add(sql);
            nameResults.Add(where.Parameters.Select(parameter => parameter.ParamName).ToArray());
        });

        Assert.AreEqual(1, sqlResults.Distinct().Count(), "All renders must produce identical SQL.");
        Assert.AreEqual(1, nameResults.Select(names => string.Join(",", names)).Distinct().Count(),
            "All snapshot walks must produce identical names.");
    }

    [TestMethod]
    public void MaterializedParameters_CarryWrappedLikeValues_AndNamesMatchTheSql()
    {
        var where = BuildSampleConditions();

        var sql = _builder.SelectWithParameters("sample", where, null, null, null, out var dbParams);

        StringAssert.Contains(sql, "name LIKE @name0");
        var likeParameter = dbParams.Single(parameter => parameter.ParamName == "name0");
        Assert.AreEqual("%abc%", likeParameter.Value, "LIKE wrapping belongs on the command parameter snapshot.");
        Assert.AreEqual("abc", where.Parameters.Single(parameter => parameter.Column == "name").Value,
            "The caller's original value stays unwrapped.");
        CollectionAssert.AreEqual(
            where.Parameters.Select(parameter => parameter.ParamName).ToArray(),
            dbParams.Select(parameter => parameter.ParamName).ToArray(),
            "Snapshot names must equal the names rendered into the SQL.");
    }

    [TestMethod]
    public void UpdateWithParameters_ListsInputParametersBeforeWhereParameters()
    {
        var where = new WhereConditions("id", 7);
        var input = new InputValues().Add("name", "updated");

        var sql = _builder.UpdateWithParameters("sample", input, where, false, out var dbParams);

        Assert.AreEqual("UPDATE sample SET name = @V0_name WHERE id = @id0", sql);
        CollectionAssert.AreEqual(new object[] { "updated", 7 }, dbParams.Select(parameter => parameter.Value).ToArray());
    }

    [TestMethod]
    public void EngineSelect_AppliesLikeWrapping_EndToEnd()
    {
        var path = Path.Combine(Path.GetTempPath(), $"coddloom-like-{Guid.NewGuid():N}.db");
        try
        {
            var executor = new SqliteExecutor($"Data Source={path};Version=3;Pooling=False;");
            var engine = new DbEngine(executor, [new TableDefine(typeof(LikeTable))]);

            engine.Insert(LikeTable.TableName, new InputValues().Add(LikeTable.Id, 1).Add(LikeTable.Name, "alice"));
            engine.Insert(LikeTable.TableName, new InputValues().Add(LikeTable.Id, 2).Add(LikeTable.Name, "Ada Lovelace"));
            engine.Insert(LikeTable.TableName, new InputValues().Add(LikeTable.Id, 3).Add(LikeTable.Name, "lancelot"));

            var matches = engine.Select<string>(
                record => record.GetString(1),
                LikeTable.TableName,
                new WhereConditions(LikeTable.Name, "love", WhereOperator.Like),
                null, null as ColumnParam);

            CollectionAssert.AreEqual(new[] { "Ada Lovelace" }, matches,
                "The materialized command parameter must carry the wrapped LIKE value.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static WhereConditions BuildSampleConditions()
    {
        var nested = new WhereConditions().In("item.id", new[] { 3, 4 });
        return new WhereConditions("a", 1)
            .Add("name", "abc", WhereOperator.Like)
            .Add(nested, WhereConnector.Or);
    }

    private static class LikeTable
    {
        [DbTableName]
        public const string TableName = "like_sample";

        [DbPrimaryKey]
        public const string Id = "id";

        [DbColumnString(Length = 50)]
        public const string Name = "name";
    }
}
