using System.Data;

namespace Qz.Infra.Database.Input
{
    public class InputValuesItem
    {
        public string Column{ get; set; }

        public string StringValue{ get; set; }

        public DbType Type { get; set; }
    }
}
