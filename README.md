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

Tables and entities are defined independently: table constants describe schema and reusable SQL identifiers, while mapping attributes connect entity members to those columns.

## Build and test

```bash
dotnet build CoddLoom.sln --configuration Release
dotnet test CoddLoom.sln --configuration Release --no-build
```

## License

CoddLoom is licensed under the [MIT License](LICENSE). Database drivers remain under their respective licenses; see [Third-party notices](THIRD-PARTY-NOTICES.md).
