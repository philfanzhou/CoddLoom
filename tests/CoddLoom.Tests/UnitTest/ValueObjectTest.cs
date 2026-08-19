using CoddLoom.Condition;
using CoddLoom.Dto;
using CoddLoom.Entity;
using CoddLoom.Input;
using CoddLoom.Model;
using CoddLoom.Params;
using CoddLoom.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Linq;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class ValueObjectTest
{
    [TestMethod]
    public void InputValues_NormalizeNullEmptyDateAndDuplicateInputs()
    {
        var values = new InputValues()
            .Add<string>("null", null)
            .AddString("blank", "   ", allowEmpty: false)
            .AddString("trimmed", " value ", autoTrim: true)
            .AddDateTime("missingDate", null)
            .AddDateTime("minDate", DateTime.MinValue, allowMinValue: false)
            .AddNull("explicitNull");

        Assert.AreEqual(DBNull.Value, values.Items.Single(item => item.Column == "null").Value);
        Assert.AreEqual(DBNull.Value, values.Items.Single(item => item.Column == "blank").Value);
        Assert.AreEqual("value", values.Items.Single(item => item.Column == "trimmed").Value);
        Assert.AreEqual(DBNull.Value, values.Items.Single(item => item.Column == "missingDate").Value);
        Assert.AreEqual(DBNull.Value, values.Items.Single(item => item.Column == "minDate").Value);
        Assert.ThrowsExactly<ArgumentNullException>(() => new InputValues().Add(" ", 1));
        Assert.ThrowsExactly<ArgumentException>(() => values.Add("trimmed", "again"));
    }

    [TestMethod]
    public void PageParam_ValidatesBoundsAndCalculatesOffset()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PageParam { PageSize = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PageParam { PageNumber = -1 });

        var page = new PageParam { PageNumber = 4, PageSize = 25 };
        Assert.AreEqual(75, page.Offset);
    }

    [TestMethod]
    public void DtoExtensions_MapPagingAndValidatedOrdering()
    {
        var dto = new PageAndOrderByDto { PageNumber = 2, PageSize = 15, OrderBy = "Created", IsDesc = true };

        var page = dto.GetPageParam();
        var order = dto.GetOrderByCondition<OrderTable>();

        Assert.AreEqual(2, page.PageNumber);
        Assert.AreEqual(15, page.PageSize);
        Assert.AreEqual("SELECT * FROM sample ORDER BY createdAt DESC",
            new CoddLoom.Sql.SqlBuilder().Select("sample", orderBy: order));
        Assert.AreEqual("SELECT * FROM sample ORDER BY id ASC",
            new CoddLoom.Sql.SqlBuilder().Select("sample",
                orderBy: new OrderByCondition<OrderTable>(string.Empty, OrderTable.Id)));
        Assert.ThrowsExactly<Exception>(() => new OrderByCondition<OrderTable>("unknown"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new OrderByCondition<OrderTable>(string.Empty));
        Assert.ThrowsExactly<ArgumentNullException>(() => new OrderByCondition<OrderTypeWithoutColumns>("id"));
    }

    [TestMethod]
    public void PageResult_StoresResultMetadata()
    {
        var result = new PageResult<int> { Items = [1, 2], PageNumber = 2, TotalPage = 3, TotalCount = 6 };

        CollectionAssert.AreEqual(new[] { 1, 2 }, result.Items);
        Assert.AreEqual(2, result.PageNumber);
        Assert.AreEqual(3, result.TotalPage);
        Assert.AreEqual(6, result.TotalCount);
    }

    [TestMethod]
    public void TableDefine_ReadsValidDefinitionsAndRejectsInvalidOnes()
    {
        var table = new TableDefine(typeof(ValidTable));

        Assert.AreEqual("valid", table.Name);
        Assert.AreEqual("id", table.PrimaryKey.Name);
        Assert.HasCount(1, table.Columns);
        Assert.ThrowsExactly<InvalidOperationException>(() => new TableDefine(typeof(NoTableName)));
        Assert.ThrowsExactly<InvalidOperationException>(() => new TableDefine(typeof(TwoTableNames)));
        Assert.ThrowsExactly<InvalidOperationException>(() => new TableDefine(typeof(NoColumns)));
        Assert.ThrowsExactly<InvalidOperationException>(() => new TableDefine(typeof(TwoPrimaryKeys)));
    }

    [TestMethod]
    public void EntityPrimaryKeyAttribute_IsUsedWhenNoTableDefinitionCanBeResolved()
    {
        var where = WhereConditions.ById<AttributePrimaryKeyEntity>("42", out var tableName);

        Assert.AreEqual("external_table", tableName);
        Assert.AreEqual("id", where.Parameters.Single().Column);
        Assert.ThrowsExactly<ArgumentNullException>(() => WhereConditions.ById<AttributePrimaryKeyEntity>(string.Empty, out _));
        Assert.ThrowsExactly<ArgumentException>(() => WhereConditions.ById<NoPrimaryKeyEntity>("42", out _));
    }

    private sealed class OrderTable
    {
        public const string Id = "id";
        public const string Created = "createdAt";
    }

    private sealed class OrderTypeWithoutColumns { }

    private static class ValidTable
    {
        [DbTableName] public const string Table = "valid";
        [DbPrimaryKeyString(Length = 20)] public const string Id = "id";
        [DbColumn(Type = DbType.Int32)] public const string Count = "count";
    }

    private static class NoTableName
    {
        [DbColumn(Type = DbType.Int32)] public const string Count = "count";
    }

    private static class TwoTableNames
    {
        [DbTableName] public const string First = "first";
        [DbTableName] public const string Second = "second";
        [DbColumn(Type = DbType.Int32)] public const string Count = "count";
    }

    private static class NoColumns
    {
        [DbTableName] public const string Table = "empty";
    }

    private static class TwoPrimaryKeys
    {
        [DbTableName] public const string Table = "two_keys";
        [DbPrimaryKey(Type = DbType.Int32)] public const string First = "first";
        [DbPrimaryKey(Type = DbType.Int32)] public const string Second = "second";
    }

    [MapTable(Name = "external_table")]
    private sealed class AttributePrimaryKeyEntity
    {
        [MapColumn(Name = "id", PrimaryKey = true)] public string Id { get; set; }
    }

    [MapTable(Name = "external_no_key")]
    private sealed class NoPrimaryKeyEntity
    {
        [MapColumn(Name = "id")] public string Id { get; set; }
    }
}
