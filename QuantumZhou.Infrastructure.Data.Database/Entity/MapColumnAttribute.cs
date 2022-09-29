using System;

namespace QuantumZhou.Infrastructure.Data.Database.Entity
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MapColumnAttribute : Attribute
    {
        public string Name { get; set; }

        public bool PrimaryKey { get; set; }
    }
}
