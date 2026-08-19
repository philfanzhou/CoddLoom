using System.Data;

namespace CoddLoom.Table.Base;

public interface INormalColumn
{
    DbType Type { get; set; }
}