using System;

namespace Qz.Infra.Database.Entity
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MapColumnAttribute : Attribute
    {
        public string Name { get; set; }

        public bool PrimaryKey { get; set; }
    }
}
