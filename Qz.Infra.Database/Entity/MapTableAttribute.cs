using System;

namespace Qz.Infra.Database.Entity
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MapTableAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
