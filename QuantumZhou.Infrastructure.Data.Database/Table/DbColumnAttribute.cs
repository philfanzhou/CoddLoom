using System;

namespace QuantumZhou.Infrastructure.Data.Database.Table
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DbColumnAttribute : DbColumnBaseAttribute
    {
        public virtual bool AllowEmpty { get; set; }
    }
}
