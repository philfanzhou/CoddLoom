using CoddLoom.Condition;
using CoddLoom.Input;
using CoddLoom.Params;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CoddLoom.Tests.UnitTest;

[TestClass]
public class SqliteExecutorTest
{
    [TestMethod]
    public void DecimalColumn_PreservesValuesBeyondDoublePrecision()
    {
        WithExecutor(executor =>
        {
            var engine = new DbEngine(executor, [new TableDefine(typeof(PreciseDecimalTable))]);
            const decimal expected = 1234567890123456.78m;

            engine.Insert(PreciseDecimalTable.TableName,
                new InputValues().Add(PreciseDecimalTable.Amount, expected));
            engine.Insert(PreciseDecimalTable.TableName,
                new InputValues().Add(PreciseDecimalTable.Amount, 10m));
            engine.Insert(PreciseDecimalTable.TableName,
                new InputValues().Add(PreciseDecimalTable.Amount, 2m));
            engine.Insert(PreciseDecimalTable.TableName,
                new InputValues().Add(PreciseDecimalTable.Amount, -5m));

            var storedType = executor.Scalar("SELECT typeof(amount) FROM precise_decimal WHERE id = 1",
                value => value.ToString());
            var actual = executor.Scalar("SELECT amount FROM precise_decimal WHERE id = 1",
                value => decimal.Parse(value.ToString(), CultureInfo.InvariantCulture));
            var ordered = executor.Reader("SELECT amount FROM precise_decimal ORDER BY amount",
                record => decimal.Parse(record[0].ToString(), CultureInfo.InvariantCulture));
            var greaterThanTwo = engine.Count(PreciseDecimalTable.TableName,
                new WhereConditions(PreciseDecimalTable.Amount, 2m, WhereOperator.GreaterThan));

            Assert.AreEqual("text", storedType);
            Assert.AreEqual(expected, actual);
            CollectionAssert.AreEqual(new[] { -5m, 2m, 10m, expected }, ordered);
            Assert.AreEqual(2, greaterThanTwo);
        });
    }

    [TestMethod]
    public void Commands_RejectParameterCountsAboveProviderLimitBeforeExecution()
    {
        WithExecutor(executor =>
        {
            Assert.AreEqual(32766, executor.MaxParametersPerCommand);

            var limitedExecutor = new SmallLimitSqliteExecutor(executor.ConnectionString);
            var parameters = new List<ValueParam>
            {
                new(1, "one"),
                new(2, "two"),
                new(3, "three")
            };

            var result = limitedExecutor.Scalar("SELECT @one + @two",
                value => System.Convert.ToInt32(value), parameters.GetRange(0, 2));
            var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
                limitedExecutor.NonQuery("SELECT 1", parameters));

            Assert.AreEqual(3, result);
            StringAssert.Contains(exception.Message, "provider limit of 2 parameters");
        });
    }

    private static void WithExecutor(Action<SqliteExecutor> action)
    {
        var path = Path.Combine(Path.GetTempPath(), $"coddloom-unit-{Guid.NewGuid():N}.db");
        try
        {
            action(new SqliteExecutor($"Data Source={path};Version=3;Pooling=False;"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class SmallLimitSqliteExecutor(string connectionString)
        : SqliteExecutor(connectionString)
    {
        public override int MaxParametersPerCommand => 2;
    }

    private static class PreciseDecimalTable
    {
        [DbTableName]
        public const string TableName = "precise_decimal";

        [DbPrimaryKeyIdentity]
        public const string Id = "id";

        [DbColumnDecimal(Length = 18, PointLength = 2)]
        public const string Amount = "amount";
    }
}
