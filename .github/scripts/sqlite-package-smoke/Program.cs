using System.Runtime.InteropServices;
using CoddLoom.Sqlite;

Console.WriteLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
{
    throw new PlatformNotSupportedException(
        $"Expected an arm64 process, but found {RuntimeInformation.ProcessArchitecture}.");
}

var databasePath = Path.Combine(
    Path.GetTempPath(), $"coddloom-sqlite-smoke-{Guid.NewGuid():N}.db");

try
{
    var executor = new SqliteExecutor(
        Path.GetDirectoryName(databasePath)!, Path.GetFileName(databasePath));

    using var connection = executor.GetConnection();
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = "CREATE TABLE SmokeTest (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);";
    command.ExecuteNonQuery();

    command.CommandText = "INSERT INTO SmokeTest (Id, Value) VALUES (1, 'arm64 package works');";
    if (command.ExecuteNonQuery() != 1)
    {
        throw new InvalidOperationException("The SQLite smoke-test insert did not affect one row.");
    }

    command.CommandText = "SELECT Value FROM SmokeTest WHERE Id = 1;";
    var value = command.ExecuteScalar() as string;
    if (value != "arm64 package works")
    {
        throw new InvalidOperationException($"The SQLite smoke-test read returned '{value}'.");
    }

    Console.WriteLine("SQLite package create, insert, and read smoke test passed.");
}
finally
{
    File.Delete(databasePath);
}
