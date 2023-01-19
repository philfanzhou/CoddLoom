using System;
using System.Data;

namespace Qz.Infra.Database.Table
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public abstract class DbColumnBaseAttribute : Attribute
    {
        public DbType Type { get; set; }

        public int Length { get; set; } = 50;

        internal string Name { get; set; }
    }
}
