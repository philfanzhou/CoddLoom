# CoddLoom

CoddLoom is a lightweight, explicit ORM that weaves entity mapping, schema definitions, and portable SQL directly over ADO.NET.

It keeps SQL visible and the data-access model small: there is no LINQ provider, change tracker, or hidden unit of work. CoddLoom provides explicit table and entity mapping, parameterized CRUD, conditions and joins, pagination, transactions, batch inserts, and additive column initialization.

## Packages

| Package | Database |
| --- | --- |
| `CoddLoom` | Database-independent core |
| `CoddLoom.SqlServer` | Microsoft SQL Server |
| `CoddLoom.Sqlite` | SQLite |
| `CoddLoom.MySql` | MySQL |
| `CoddLoom.MariaDb` | MariaDB |
| `CoddLoom.Oracle` | Oracle Database |
| `CoddLoom.PostgreSql` | PostgreSQL |

Stable versions are published to [NuGet.org](https://www.nuget.org/) from `vX.Y.Z`
tags. Every successful push to `main` also publishes a uniquely versioned
`0.0.0-edge.*` build to GitHub Packages for pre-release validation. GitHub Packages
requires a GitHub token even for public packages; see
[Working with the NuGet registry](https://docs.github.com/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)
for source and authentication setup.

## Example

```csharp
using CoddLoom;
using CoddLoom.Condition;
using CoddLoom.Sqlite;
using CoddLoom.Table;

var executor = new SqliteExecutor("data", "app.db");
var engine = new DbEngine(executor, new[]
{
    new TableDefine(typeof(UserTable))
});

var users = engine.Select<User>(
    new WhereConditions(UserTable.Name, "Ada"),
    new OrderByCondition(UserTable.Id));
```

For PostgreSQL, install `CoddLoom.PostgreSql` and construct the engine with a
`PostgreSqlExecutor` using an Npgsql-compatible connection string.

Tables and entities are defined independently: table constants describe schema and reusable SQL identifiers, while mapping attributes connect entity members to those columns.

## ID generation and concurrency

`GenerateId`, `GenerateMaxId`, `GenerateTimeId`, and `GenerateUtcTimeId` only
return candidate values that are unused when their existence queries run. They do
not reserve an ID or guarantee that a later insert will succeed: concurrent callers
can receive the same candidate between the query and the insert. Passing a database
connection or transaction does not remove that post-return race.

For concurrent workloads, prefer IDs generated atomically by the database through
an identity column or sequence. Client-generated UUIDs are another option. If IDs
must be generated with an application-specific scheme, enforce a unique constraint
in the database and retry the insert after a duplicate-key failure.

## Build and test

```bash
dotnet build CoddLoom.sln --configuration Release
dotnet test CoddLoom.sln --configuration Release --no-build
```

The default test run uses an isolated SQLite database and includes the complete
unit and integration suite. CI also runs the database integration category
against PostgreSQL 16, MySQL 8.4, MariaDB 11.4, and SQL Server 2022. Provider SQL
contract tests run for all six database providers, including Oracle, on every build.

To run the integration category against a local server, set the provider and its
connection string before invoking the filtered suite. Oracle is intentionally an
opt-in real-server test because its image and licensing requirements do not fit
the public CI runner; its SQL dialect remains covered by the provider contract
suite.

```bash
TEST_DATABASE_TYPE=Oracle \
TEST_DB_CONNECTION_ORACLE="Data Source=localhost:1521/FREEPDB1;User Id=test;Password=password;" \
dotnet test tests/CoddLoom.Tests/CoddLoom.Tests.csproj --filter "TestCategory=Database"
```

## Releasing

Repository maintainers configure a NuGet.org Trusted Publishing policy for
`.github/workflows/release.yml`, then push a semantic-version tag. The release
workflow verifies the repository, publishes all seven packages and their symbol
packages, and creates a GitHub Release containing the same artifacts.

```bash
git tag v1.2.3
git push origin v1.2.3
```

Pre-release tags such as `v1.2.3-rc.1` create a pre-release and publish a NuGet
pre-release version.

## License

CoddLoom is licensed under the [MIT License](LICENSE). Database drivers remain under their respective licenses; see [Third-party notices](THIRD-PARTY-NOTICES.md).
