using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class SqlBuilderTest
{
    private readonly SqlBuilder _builder = new();

    [TestMethod]
    public void Insert_UsesParametersForEveryRow()
    {
        var rows = new[]
        {
            new InputValues().Add("id", 1).Add("name", "one"),
            new InputValues().Add("name", "two").Add("id", 2)
        };

        var sql = _builder.Insert("sample", rows, out var parameters);

        Assert.AreEqual("INSERT INTO sample (id,name) VALUES (@V0_id,@V0_name),(@V1_id,@V1_name)", sql);
        Assert.HasCount(4, parameters);
        CollectionAssert.AreEqual(new object[] { 1, "one", 2, "two" }, parameters.Select(parameter => parameter.Value).ToArray());
    }

    [TestMethod]
    public void Insert_RejectsInvalidOrInconsistentRows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Insert(null, [], out _));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Insert("sample", null, out _));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Insert("sample", [], out _));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _builder.Insert("sample", [new InputValues()], out _));
        Assert.ThrowsExactly<ArgumentException>(() => _builder.Insert("sample",
            [new InputValues().Add("id", 1), new InputValues().Add("name", "two")], out _));
    }

    [TestMethod]
    public void Insert_LiteralMode_IsEscapedAndCultureInvariant()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var row = new InputValues()
                .Add("text", "O'Brien")
                .Add("number", 12.5m)
                .Add("enabled", true)
                .Add("created", new DateTime(2024, 2, 3, 4, 5, 6))
                .AddNull("missing");

            var sql = _builder.Insert("sample", [row], out var parameters, useParameter: false);

            Assert.AreEqual(
                "INSERT INTO sample (text,number,enabled,created,missing) VALUES ('O''Brien',12.5,1,'2024-02-03 04:05:06',NULL)",
                sql);
            Assert.IsEmpty(parameters);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestMethod]
    public void Insert_LiteralMode_StillHonorsForceParameter()
    {
        var row = new InputValues().Add("secret", "value", forceParameter: true);

        var sql = _builder.Insert("sample", [row], out var parameters, useParameter: false);

        Assert.AreEqual("INSERT INTO sample (secret) VALUES (@V0_secret)", sql);
        Assert.HasCount(1, parameters);
    }

    [TestMethod]
    public void DeleteAndUpdate_RequireConditionsUnlessForced()
    {
        var values = new InputValues().Add("name", "updated");

        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Delete("sample", null));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Delete("sample", new WhereConditions()));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Update("sample", values, null));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Update("sample", new InputValues(), null, force: true));
        Assert.AreEqual("DELETE FROM sample", _builder.Delete("sample", null, force: true));
        Assert.AreEqual("UPDATE sample SET name = @V0_name", _builder.Update("sample", values, null, force: true));
    }

    [TestMethod]
    public void WhereConditions_RenderEveryOperatorAndConnector()
    {
        var where = new WhereConditions("a", 1)
            .Add("b", 2, WhereOperator.NotEqual)
            .Add("c", 3, WhereOperator.GreaterThan)
            .Add("d", 4, WhereOperator.GreaterEqual)
            .Add("e", 5, WhereOperator.LessThan)
            .Add("f", 6, WhereOperator.LessEqual)
            .Add("name", "abc", WhereOperator.Like, WhereConnector.Or)
            .IsNull("deletedAt")
            .IsNotNull("createdAt", WhereConnector.Or)
            .In("status", new[] { 1, 2 });

        var sql = _builder.Select("sample", where);

        Assert.AreEqual(
            "SELECT * FROM sample WHERE a = @a0 AND b != @b0 AND c > @c0 AND d >= @d0 AND e < @e0 AND f <= @f0 OR name LIKE @name0 AND deletedAt IS NULL OR createdAt IS NOT NULL AND status IN (@status0,@status1)",
            sql);
        Assert.AreEqual("%abc%", where.Parameters.Single(parameter => parameter.Column == "name").Value);
    }

    [TestMethod]
    public void NestedInConditions_RenameAllParametersWithoutCollisions()
    {
        var nested = new WhereConditions().In("item.id", new[] { 3, 4 });
        var where = new WhereConditions().In("item.id", new[] { 1, 2 }).Add(nested, WhereConnector.Or);

        var sql = _builder.Select("sample", where);
        var names = where.Parameters.Select(parameter => parameter.ParamName).ToArray();

        Assert.AreEqual(
            "SELECT * FROM sample WHERE item.id IN (@item_id0,@item_id1) OR (item.id IN (@item_id2,@item_id3))",
            sql);
        Assert.HasCount(4, names.Distinct().ToArray());
    }

    [TestMethod]
    public void EmptyNestedConditions_AreIgnored()
    {
        var where = new WhereConditions().Add((WhereConditions)null).Add(new WhereConditions());

        Assert.IsTrue(where.IsEmpty());
        Assert.AreEqual("SELECT * FROM sample", _builder.Select("sample", where));
    }

    [TestMethod]
    public void EmptyValues_AreIgnoredUnlessExplicitlyAllowed()
    {
        var where = new WhereConditions()
            .Add("ignoredNull", null)
            .Add("ignoredEmpty", string.Empty)
            .Add("allowedEmpty", string.Empty, allowEmptyValue: true)
            .In<int>("ignoredRange", null)
            .In("ignoredNullItems", new int?[] { null });

        Assert.AreEqual("SELECT * FROM sample WHERE allowedEmpty = @allowedEmpty0", _builder.Select("sample", where));
        Assert.HasCount(1, where.Parameters.ToArray());
    }

    [TestMethod]
    public void Select_ComposesColumnsGroupingOrderingAndPagination()
    {
        var columns = new ColumnParam()
            .AddSelect("createdAt", "MAX", DbType.DateTime, "latest", groupBy: true)
            .AddSelect("tenantId", groupBy: true);
        var orderBy = new OrderByCondition("tenantId", descending: true);
        orderBy.Add("latest");
        var page = new PageParam { PageNumber = 2, PageSize = 25 };

        var sql = _builder.Select("sample", orderBy: orderBy, pageParam: page, columns: columns);

        Assert.AreEqual(
            "SELECT MAX(CAST(createdAt AS DateTime)) AS latest,tenantId FROM sample GROUP BY createdAt,tenantId ORDER BY tenantId DESC,latest ASC LIMIT 25,25",
            sql);
        Assert.AreEqual("SELECT COUNT(createdAt) FROM sample GROUP BY createdAt,tenantId", _builder.Count("sample", columns: columns));
    }

    [TestMethod]
    public void PublicMethods_ValidateRequiredNames()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Select(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Count(string.Empty));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Delete(null, null, force: true));
        Assert.ThrowsExactly<ArgumentNullException>(() => _builder.Update(null, new InputValues().Add("a", 1), null, force: true));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _builder.GetJoinTable(null));
    }
}
