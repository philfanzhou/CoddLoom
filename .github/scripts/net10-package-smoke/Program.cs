var targetFramework = AppContext.TargetFrameworkName;
Console.WriteLine($"Target framework: {targetFramework}");
if (targetFramework != ".NETCoreApp,Version=v10.0")
{
    throw new PlatformNotSupportedException(
        $"Expected a net10.0 application, but found '{targetFramework}'.");
}

Console.WriteLine($".NET runtime version: {Environment.Version}");
if (Environment.Version.Major != 10)
{
    throw new PlatformNotSupportedException(
        $"Expected .NET runtime major version 10, but found {Environment.Version}.");
}

var publicTypes = new[]
{
    typeof(CoddLoom.DbEngine),
    typeof(CoddLoom.MariaDb.MariaDbExecutor),
    typeof(CoddLoom.MySql.MySqlExecutor),
    typeof(CoddLoom.Oracle.OracleExecutor),
    typeof(CoddLoom.PostgreSql.PostgreSqlExecutor),
    typeof(CoddLoom.Sqlite.SqliteExecutor),
    typeof(CoddLoom.SqlServer.SqlServerExecutor)
};

Console.WriteLine("Resolved public package types:");
foreach (var publicType in publicTypes)
{
    Console.WriteLine($"- {publicType.FullName}");
}

var databasePath = Path.Combine(
    Path.GetTempPath(), $"coddloom-net10-smoke-{Guid.NewGuid():N}.db");

try
{
    var executor = new CoddLoom.Sqlite.SqliteExecutor(
        Path.GetDirectoryName(databasePath)!, Path.GetFileName(databasePath));

    using var connection = executor.GetConnection();
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText =
        "CREATE TABLE SmokeTest (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);";
    command.ExecuteNonQuery();

    command.CommandText =
        "INSERT INTO SmokeTest (Id, Value) VALUES (1, 'net10 package works');";
    if (command.ExecuteNonQuery() != 1)
    {
        throw new InvalidOperationException(
            "The .NET 10 SQLite smoke-test insert did not affect one row.");
    }

    command.CommandText = "SELECT Value FROM SmokeTest WHERE Id = 1;";
    var value = command.ExecuteScalar() as string;
    if (value != "net10 package works")
    {
        throw new InvalidOperationException(
            $"The .NET 10 SQLite smoke-test read returned '{value}'.");
    }

    Console.WriteLine(".NET 10 SQLite package create, insert, and read smoke test passed.");
}
finally
{
    File.Delete(databasePath);
}
