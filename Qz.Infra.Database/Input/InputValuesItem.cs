using System.Data;

namespace Qz.Infra.Database.Input;

public class InputValuesItem<T> : IInputValuesItem
{
    public string Column { get; set; }

    public DbType Type { get; set; }

    public T Value { get; set; }

    public bool IsUnicode { get; set; }
}