using CoddLoom.Convert;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using CoddLoom.Entity;

namespace CoddLoom.Tests.DbTest;

[TestClass]
public class DbConverterTest
{
    [TestMethod]
    public void ToEntity_WhenProviderReturnsTargetType_AssignsValueDirectly()
    {
        var expected = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add(nameof(GuidProjection.Id), typeof(Guid));
        table.Rows.Add(expected);

        using var reader = table.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var entity = reader.ToEntity<GuidProjection>();

        Assert.AreEqual(expected, entity.Id);
    }

    [TestMethod]
    public void ToEntity_ConvertsStringsNumbersEnumsAndGuidBytes()
    {
        var expectedGuid = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("identifier", typeof(byte[]));
        table.Columns.Add("Count", typeof(long));
        table.Columns.Add("State", typeof(string));
        table.Columns.Add("Ignored", typeof(string));
        table.Rows.Add(expectedGuid.ToByteArray(), 12L, "active", DBNull.Value);

        using var reader = table.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var mapped = reader.ToEntity<MappedProjection>();
        Assert.AreEqual(expectedGuid, mapped.Id);
        Assert.AreEqual(12, mapped.Count);
        Assert.AreEqual(ProjectionState.Active, mapped.State);
    }

    [TestMethod]
    public void ToEntity_MapsPublicFieldsAndIgnoresMissingOrDbNullColumns()
    {
        var expectedGuid = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add(nameof(FieldProjection.Id), typeof(string));
        table.Columns.Add(nameof(FieldProjection.NullableCount), typeof(int));
        table.Rows.Add(expectedGuid.ToString(), DBNull.Value);

        using var reader = table.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var projection = reader.ToEntity<FieldProjection>();
        Assert.AreEqual(expectedGuid, projection.Id);
        Assert.IsNull(projection.NullableCount);
        Assert.AreEqual("initial", projection.Missing);
    }

    [TestMethod]
    public void ToEntity_ParsesInvariantDecimalTextRegardlessOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var table = new DataTable();
            table.Columns.Add(nameof(DecimalProjection.Amount), typeof(string));
            table.Rows.Add("1234567890123456.78");

            using var reader = table.CreateDataReader();
            Assert.IsTrue(reader.Read());

            Assert.AreEqual(1234567890123456.78m, reader.ToEntity<DecimalProjection>().Amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void ToEntity_DistinguishesTypesWithTheSameSimpleName()
    {
        var firstTable = new DataTable();
        firstTable.Columns.Add("first", typeof(string));
        firstTable.Rows.Add("first-value");
        using (var firstReader = firstTable.CreateDataReader())
        {
            Assert.IsTrue(firstReader.Read());
            Assert.AreEqual("first-value", firstReader.ToEntity<FirstModels.Projection>().Value);
        }

        var secondTable = new DataTable();
        secondTable.Columns.Add("second", typeof(string));
        secondTable.Rows.Add("second-value");
        using var secondReader = secondTable.CreateDataReader();
        Assert.IsTrue(secondReader.Read());

        Assert.AreEqual("second-value", secondReader.ToEntity<SecondModels.Projection>().Value);
    }

    [TestMethod]
    public void CreateTable_PreservesNullableTypesAndNullValues()
    {
        var table = DbConverter.CreateTable(new List<TableProjection>
        {
            new() { Id = 1, Name = "one", Optional = null },
            new() { Id = 2, Name = null, Optional = 4 }
        });

        Assert.AreEqual(nameof(TableProjection), table.TableName);
        Assert.AreEqual(typeof(int), table.Columns[nameof(TableProjection.Optional)].DataType);
        Assert.HasCount(2, table.Rows);
        Assert.AreEqual(DBNull.Value, table.Rows[0][nameof(TableProjection.Optional)]);
        Assert.AreEqual(DBNull.Value, table.Rows[1][nameof(TableProjection.Name)]);
    }

    [TestMethod]
    public void RecordValueHelpers_HandleProviderRepresentations()
    {
        var table = new DataTable();
        table.Columns.Add("one", typeof(object));
        table.Columns.Add("zero", typeof(object));
        table.Columns.Add("truth", typeof(object));
        table.Columns.Add("empty", typeof(object));
        table.Columns.Add("date", typeof(object));
        table.Columns.Add("invalidDate", typeof(object));
        table.Rows.Add(1, 0, "true", DBNull.Value, "2024-02-03T04:05:06", "invalid");

        using var reader = table.CreateDataReader();
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(DbConverter.GetBoolean(reader, "one"));
        Assert.IsFalse(DbConverter.GetBoolean(reader, "zero"));
        Assert.IsTrue(DbConverter.GetBoolean(reader, "truth"));
        Assert.IsFalse(DbConverter.GetBoolean(reader, "empty"));
        Assert.AreEqual(new DateTime(2024, 2, 3, 4, 5, 6), DbConverter.GetDateTime(reader, "date"));
        Assert.AreEqual(DateTime.MinValue, DbConverter.GetDateTime(reader, "invalidDate"));
        Assert.AreEqual(DateTime.MinValue, DbConverter.GetDateTime(null, "date"));
        Assert.IsFalse(DbConverter.GetBoolean(null, "one"));
    }

    private sealed class GuidProjection
    {
        public Guid Id { get; set; }
    }

    [MapTable(Name = "projection")]
    private sealed class MappedProjection
    {
        [MapColumn(Name = "identifier")] public Guid Id { get; set; }
        [MapColumn(Name = "count")] public int Count { get; set; }
        [MapColumn(Name = "state")] public ProjectionState State { get; set; }
    }

    private sealed class FieldProjection
    {
        public Guid Id = default;
        public int? NullableCount = default;
        public string Missing = "initial";
    }

    private sealed class DecimalProjection
    {
        public decimal Amount { get; set; }
    }

    private sealed class TableProjection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Optional { get; set; }
    }

    private enum ProjectionState { Inactive, Active }

    private static class FirstModels
    {
        [MapTable(Name = "first_projection")]
        internal sealed class Projection
        {
            [MapColumn(Name = "first")] public string Value { get; set; }
        }
    }

    private static class SecondModels
    {
        [MapTable(Name = "second_projection")]
        internal sealed class Projection
        {
            [MapColumn(Name = "second")] public string Value { get; set; }
        }
    }
}
