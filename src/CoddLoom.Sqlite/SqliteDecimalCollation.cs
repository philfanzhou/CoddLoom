using System;
using System.Data.SQLite;
using System.Globalization;

namespace CoddLoom.Sqlite;

[SQLiteFunction(Name = "CODDLOOM_DECIMAL", FuncType = FunctionType.Collation)]
internal sealed class SqliteDecimalCollation : SQLiteFunction
{
    public override int Compare(string left, string right)
    {
        if (decimal.TryParse(left, NumberStyles.Number, CultureInfo.InvariantCulture, out var leftValue)
            && decimal.TryParse(right, NumberStyles.Number, CultureInfo.InvariantCulture, out var rightValue))
        {
            return leftValue.CompareTo(rightValue);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    }
}
