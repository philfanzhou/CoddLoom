namespace CoddLoom.Table.Base;

public interface IStringColumn
{
    int Length { get; }

    bool FixedLength { get; }

    bool AllowUnicode { get; }
}