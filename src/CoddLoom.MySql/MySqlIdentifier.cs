namespace CoddLoom.MySql;

internal static class MySqlIdentifier
{
    internal static string QuoteDatabase(string database)
    {
        return "`" + database.Replace("`", "``") + "`";
    }
}
