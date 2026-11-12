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
tags. Every tagged release is also published to GitHub Packages, which requires a
GitHub token even for public packages; see
[Working with the NuGet registry](https://docs.github.com/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)
for source and authentication setup.

## Compatibility

`CoddLoom`, MariaDB, MySQL, PostgreSQL, SQLite, and SQL Server target
`netstandard2.0`. The Oracle provider targets `netstandard2.1`.

CI verifies that clean downstream projects targeting `net8.0` and `net10.0` can
restore and build all seven locally packed packages. The .NET 10 check also loads a
public type from every package and exercises SQLite by opening a database and
writing and reading a row. This establishes package and framework compatibility; it
does not mean that every provider connects to a live database on both target
frameworks. The real-database integration matrix remains the one documented in
[Build and test](#build-and-test).

## Example

```csharp
using CoddLoom;
using CoddLoom.Condition;
using CoddLoom.Sqlite;
using CoddLoom.Table;
using System.IO;

// The SQLite target directory is the caller's responsibility; the executor
// itself creates neither the directory nor the database file.
Directory.CreateDirectory("data");

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

## Construction performs no I/O

Since 0.2.0, constructing any executor neither opens a connection nor issues
DDL. Each constructor only stores its inputs:

- Connection validation is explicit: call `ValidateConnection()` to open and
  close one connection and surface driver failures eagerly.
- MySQL and MariaDB database creation is explicit: the five-argument
  convenience constructors only build a connection string; call
  `EnsureDatabase()` to run `CREATE DATABASE IF NOT EXISTS` with a safely
  quoted database name.
- SQLite creates the database file itself the first time a connection opens.
  The target directory is no longer created automatically and must already
  exist.

Tables and entities are defined independently: table constants describe schema and reusable SQL identifiers, while mapping attributes connect entity members to those columns.

## Asynchronous execution

Every execution-oriented API has a true-async counterpart that awaits the
driver's asynchronous APIs directly (`OpenAsync`, `ExecuteNonQueryAsync`,
`ExecuteReaderAsync`/`ReadAsync`, `ExecuteScalarAsync`) with
`ConfigureAwait(false)`; none of them wraps the synchronous path in
`Task.Run`. The SQL text, parameters, one-connection-per-command lifecycle,
command timeout, parameter limits, and exception types are identical to the
synchronous versions:

- `NonQueryAsync`, `NonQueryProcedureAsync`, `ReaderAsync`,
  `ReaderProcedureAsync`, `ScalarAsync`, `ScalarProcedureAsync`
- `TransactionAsync`, `TransactionAsync<T>`, `ExecuteAsync`,
  `ExecuteAsync<T>`, `ValidateConnectionAsync`
- MySQL and MariaDB additionally offer `EnsureDatabaseAsync`

The `DbEngine` command family has the same counterparts:
`InsertAsync` (single record and batched, entity and `InputValues`),
`UpdateAsync`, `DeleteAsync`, `CountAsync`, `ExistAsync`, `DropAsync`, and
`InitializeTableAsync`. Each one delegates to the executor methods above, so
its SQL text, parameters, batch boundaries, transaction ownership, and
exception types match its synchronous version exactly. The query family
(`Select`, `First`, `SelectById`, `PageSelect`) keeps its synchronous shape
for now.

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

await executor.ValidateConnectionAsync(cancellation.Token);

var users = await executor.ReaderAsync(
    "SELECT id, name FROM user_table WHERE region = @region",
    record => new User(record["id"].ToString(), record["name"].ToString()),
    dbParams: [new ValueParam("north", "region")],
    cancellationToken: cancellation.Token);

await executor.TransactionAsync(async tran =>
{
    await executor.NonQueryAsync(
        "UPDATE user_table SET seen = 1 WHERE id = @id",
        dbParams: [new ValueParam(users[0].Id, "id")],
        tran: tran);
}, cancellation.Token);

// Batched engine inserts run in one engine-owned transaction and split by
// batch size and the provider parameter limit, like the synchronous Insert.
var affected = await engine.InsertAsync(newUsers, batchSize: 50,
    cancellationToken: cancellation.Token);
```

**Cancellation convention.** The `CancellationToken` is always the last
optional parameter. It is observed before any database call — an
already-canceled token throws `OperationCanceledException` without opening a
connection — and is then forwarded to the driver's asynchronous execution, so
in-flight cancellation surfaces as the driver's exception. Callback-based
methods (`TransactionAsync`, `ExecuteAsync`) forward the token to
`OpenAsync` only; callbacks receive no token and must not block.

**Async boundaries.** Methods return `Task`/`Task<T>`, not `ValueTask`.
`TransactionAsync` begins, commits, and rolls back the transaction through the
synchronous members: netstandard2.0 has no `BeginTransactionAsync`, and for
most drivers beginning a transaction is not a network round trip. The
`Adapter`/`AdapterProcedure` family stays synchronous because ADO.NET has no
`FillAsync`; the swallowing `TryTransaction`/`TryExecute` pair and the
deprecated `Generate*Id` methods deliberately received no async twins. The
async layer was added in 0.3.0 and is purely additive.

## ID generation and concurrency

`GenerateId`, `GenerateMaxId`, `GenerateTimeId`, and `GenerateUtcTimeId` return a
candidate that was unused at the moment their existence query ran. They do not
reserve it, and they do not guarantee that a later insert will succeed.

**When these are enough.** A single writer generating IDs for the table, or an
insert backed by a unique constraint with a retry on duplicate key.

**Deprecated.** All four emit a compiler warning. They remain source-compatible, and
the deprecation itself does not change their behavior; existing callers can keep using
them while migrating to one of the alternatives below. One behavior did change
separately: `GenerateId` now throws `ArgumentOutOfRangeException` when `tryCount` is
zero or less, instead of reporting candidate exhaustion.

**Concurrency.** Otherwise, two callers can receive the same candidate in the window
between the existence query and either caller's insert. Supplying `con` or `tran`
does not by itself close that window — the query and the insert must be covered by
one isolation level or lock. A serializable transaction spanning both does close it;
read committed does not. On PostgreSQL that surfaces as a serialization failure for
one of the two, which the caller retries.

Time-based candidates format their 12-digit `yyMMddHHmmss` timestamp prefix with
the invariant culture's Gregorian calendar. The same `DateTime` therefore has the
same prefix regardless of the process's current culture. They keep second-level
timestamp precision and append a random three-digit suffix from `000` through `999`.
One local random generator supplies all retries in a `GenerateTimeId` call, but
retries can still choose the same suffix and concurrent calls still are not
guaranteed to return different candidates.

**Connections and transactions.** These methods query through `tran` when supplied,
otherwise `con`, otherwise a connection of their own; when both are given, `tran`
takes precedence and `con` is unused. This matters with no concurrency at all: a
caller inside an open transaction that does not forward it through `tran` runs the
query on a different connection, where its own uncommitted rows are invisible, so
two consecutive calls can return the same ID.

**Alternatives.** Prefer an ID the database generates atomically — an identity column
or a sequence. Client-generated UUIDs are the other standard choice. If IDs must
follow an application-specific scheme, enforce a unique constraint and retry the
insert after a duplicate-key failure.

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
