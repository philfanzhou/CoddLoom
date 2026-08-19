using System;

namespace CoddLoom.Entity;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class MapColumnAttribute : Attribute
{
    public string Name { get; set; }

    public bool PrimaryKey { get; set; }

    public bool ForceParameter { get; set; }
}