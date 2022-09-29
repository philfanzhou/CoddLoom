using System.Data;

namespace QuantumZhou.Infrastructure.Data.Database.Input
{
    public class InputValuesItem
    {
        public string Column{ get; set; }

        public string Value{ get; set; }

        public DbType Type { get; set; }
    }
}
