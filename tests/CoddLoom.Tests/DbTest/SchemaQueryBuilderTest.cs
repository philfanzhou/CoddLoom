using CoddLoom.MariaDb;
using CoddLoom.MySql;
using CoddLoom.Oracle;
using CoddLoom.PostgreSql;
using CoddLoom.Condition;
using CoddLoom.Params;
using CoddLoom.Sql;
using CoddLoom.SqlServer;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using CoddLoom.Tests.DbCode.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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
        AssertSchemaQueries(new ExposedPostgreSqlBuilder(), table, "current_schema()", table.Name.ToLowerInvariant());
    }

    [TestMethod]
    public void InitializeTable_UsesLegacyProviderSchemaHooks()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"coddloom-legacy-schema-{Guid.NewGuid():N}.db");
        var executor = new LegacySqliteExecutor($"Data Source={dbPath};Version=3;Pooling=False;");
        var engine = new DbEngine(executor);
        var table = new TableDefine(typeof(UserTable));

        try
        {
            engine.InitializeTable(new[] { table });
            engine.InitializeTable(new[] { table });

            Assert.IsTrue(executor.LegacyExistsHookCalled);
            Assert.IsTrue(executor.LegacyBuilder.LegacyColumnsHookCalled);
        }
        finally
        {
            engine.Drop(table.Name);
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
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

    private sealed class ExposedPostgreSqlBuilder : PostgreSqlBuilder, ISchemaQueryBuilder
    {
        string ISchemaQueryBuilder.GetTableExistsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableExistsSql(table, out dbParams);

        string ISchemaQueryBuilder.GetTableColumnsSql(TableDefine table, out List<ValueParam> dbParams)
            => base.GetTableColumnsSql(table, out dbParams);
    }

    private sealed class LegacySqliteExecutor(string connectionString) : SqliteExecutor(connectionString)
    {
        public bool LegacyExistsHookCalled { get; private set; }

        public LegacySqlBuilder LegacyBuilder { get; } = new();

        public override SqlBuilder SqlBuilder => LegacyBuilder;

        [Obsolete("Test-only legacy override.")]
        protected internal override void GetExistTableParam(TableDefine table,
            out string checkTable, out WhereConditions where)
        {
            LegacyExistsHookCalled = true;
            checkTable = "sqlite_master";
            where = new WhereConditions()
                .Add("type", "table")
                .Add("name", table.Name);
        }
    }

    private sealed class LegacySqlBuilder : SqlBuilder
    {
        public bool LegacyColumnsHookCalled { get; private set; }

        [Obsolete("Test-only legacy override.")]
        protected internal override string GetTableColumnsSql(string tableName)
        {
            LegacyColumnsHookCalled = true;
            return $"SELECT name FROM pragma_table_info('{tableName.Replace("'", "''")}')";
        }
    }
}
