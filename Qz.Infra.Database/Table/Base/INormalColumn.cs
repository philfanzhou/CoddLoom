using System.Data;

namespace Qz.Infra.Database.Table.Base;

public interface INormalColumn
{
    DbType Type { get; set; }
}