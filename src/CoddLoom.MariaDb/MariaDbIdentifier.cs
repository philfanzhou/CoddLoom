namespace CoddLoom.MariaDb;

internal static class MariaDbIdentifier
{
    internal static string QuoteDatabase(string database)
    {
        return "`" + database.Replace("`", "``") + "`";
    }
}
