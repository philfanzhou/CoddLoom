using System.Data;

namespace Qz.Infra.Database.Input;

public interface IInputValuesItem
{
    string Column { get; set; }

    DbType Type { get; set; }
}