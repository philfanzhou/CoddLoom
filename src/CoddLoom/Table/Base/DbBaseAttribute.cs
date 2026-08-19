using System;

namespace CoddLoom.Table.Base;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class DbBaseAttribute : Attribute
{
    public string Name { get; internal set; }
}