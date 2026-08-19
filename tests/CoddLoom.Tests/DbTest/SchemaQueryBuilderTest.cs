using CoddLoom.MariaDb;
using CoddLoom.MySql;
using CoddLoom.Oracle;
using CoddLoom.Params;
using CoddLoom.Sql;
using CoddLoom.SqlServer;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CoddLoom.Tests.DbTest;

[TestClass]
public class SchemaQueryBuilderTest
{
    [TestMethod]
    public void SchemaQueries_AreProviderScopedAndParameterizeTableNames()
    {
        var table = new TableDefine(typeof(UserTable));

        AssertSchemaQueries(new ExposedSqlBuilder(), table, "sqlite_master", table.Name);
        AssertSchemaQueries(new ExposedMySqlBuilder(), table, "DATABASE()", table.Name);
        AssertSchemaQueries(new ExposedMariaDbBuilder(), table, "DATABASE()", table.Name);
        AssertSchemaQueries(new ExposedSqlServerBuilder(), table, "SCHEMA_NAME()", table.Name);
        AssertSchemaQueries(new ExposedOracleBuilder(), table, "USER_TABLES", table.Name.ToUpperInvariant());
    }

    private static void AssertSchemaQueries(
        ISchemaQueryBuilder builder, TableDefine table, string scopeSql, string expectedTableName)
    {
        var existsSql = builder.GetTableExistsSql(table, out var existsParams);
        var columnsSql = builder.GetTableColumnsSql(table, out var columnsParams);

        StringAssert.Contains(existsSql, scopeSql);
        Assert.AreEqual(expectedTableName, GetTableNameParameter(existsParams).Value);
        Assert.AreEqual(expectedTableName, GetTableNameParameter(columnsParams).Value);
        Assert.DoesNotContain(expectedTableName, existsSql, "Table names should be passed as values, not SQL literals.");
        Assert.DoesNotContain(expectedTableName, columnsSql, "Table names should be passed as values, not SQL literals.");
    }

    private static ValueParam GetTableNameParameter(IEnumerable<ValueParam> parameters)
    {
        return parameters.Single(parameter => parameter.ParamName == "schema_table_name");
    }

    private interface ISchemaQueryBuilder
    {
        string GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams);

        string GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams);
    }

    private sealed class ExposedSqlBuilder : SqlBuilder, ISchemaQueryBuilder
    {
        string ISchemaQueryBuilder.GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableExistsSql(table, out dbParams);

        string ISchemaQueryBuilder.GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableColumnsSql(table, out dbParams);
    }

    private sealed class ExposedMySqlBuilder : MySqlBuilder, ISchemaQueryBuilder
    {
        string ISchemaQueryBuilder.GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableExistsSql(table, out dbParams);

        string ISchemaQueryBuilder.GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableColumnsSql(table, out dbParams);
    }

    private sealed class ExposedMariaDbBuilder : MariaDbBuilder, ISchemaQueryBuilder
    {
        string ISchemaQueryBuilder.GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableExistsSql(table, out dbParams);

        string ISchemaQueryBuilder.GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableColumnsSql(table, out dbParams);
    }

    private sealed class ExposedSqlServerBuilder : SqlServerBuilder, ISchemaQueryBuilder
    {
        string ISchemaQueryBuilder.GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableExistsSql(table, out dbParams);

        string ISchemaQueryBuilder.GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableColumnsSql(table, out dbParams);
    }

    private sealed class ExposedOracleBuilder : OracleBuilder, ISchemaQueryBuilder
    {
        string ISchemaQueryBuilder.GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableExistsSql(table, out dbParams);

        string ISchemaQueryBuilder.GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableColumnsSql(table, out dbParams);
    }
}
