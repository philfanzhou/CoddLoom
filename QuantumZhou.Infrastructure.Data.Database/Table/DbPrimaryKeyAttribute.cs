using System;

namespace QuantumZhou.Infrastructure.Data.Database.Table
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DbPrimaryKeyAttribute : DbColumnBaseAttribute
    {
    }
}
