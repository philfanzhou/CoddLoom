using System;

namespace Qz.Infra.Database.Table
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DbTableNameAttribute : Attribute
    {
        internal string Value { get; set; }
    }
}
