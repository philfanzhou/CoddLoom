using CoddLoom.Convert;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;

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

    private sealed class GuidProjection
    {
        public Guid Id { get; set; }
    }
}
