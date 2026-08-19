using CoddLoom.Common;
using CoddLoom.Table;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CoddLoom.Entity;

internal class EntityMap
{
    internal EntityMap(string name, Type type)
    {
        Name = name;

        Table = GetTableAttribute(type);
        if (Table == null)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        var memberList = new List<Tuple<MemberInfo, MapColumnAttribute>>();
        var members = type.GetAllMembers();

        // Obtain the TableDefine to determine the primary key.
        var tableDefine = GetTableDefine(type);
        string primaryKeyFromTable = tableDefine?.PrimaryKey?.Name;

        foreach (var member in members)
        {
            var attribute = member.GetCustomAttribute<MapColumnAttribute>(true);
            if (attribute != null)
            {
                memberList.Add(new Tuple<MemberInfo, MapColumnAttribute>(member, attribute));

                // Primary-key resolution:
                // 1. A property mapped to the primary-key column declared by TableDefine is a primary key,
                //    even when the entity property is not marked with PrimaryKey = true.
                if (primaryKeyFromTable != null && attribute.Name == primaryKeyFromTable)
                {
                    // TableDefine declares the primary key and this entity property maps to that column.
                    PrimaryKey = attribute.Name;
                }
            }
        }

        if (memberList.Count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Members = memberList.AsReadOnly();
    }

    #region Property

    internal string Name { get; }

    internal MapTableAttribute Table { get; }

    internal string PrimaryKey { get; }

    internal IReadOnlyList<Tuple<MemberInfo, MapColumnAttribute>> Members { get; }

    #endregion

    internal static bool HasMap(Type type)
    {
        var table = GetTableAttribute(type);
        return table != null;
    }

    private static MapTableAttribute GetTableAttribute(MemberInfo type)
    {
        return type.GetCustomAttribute<MapTableAttribute>();
    }

    private static TableDefine GetTableDefine(Type entityType)
    {
        try
        {
            // Try to locate the corresponding table type by table name.
            var tableAttribute = GetTableAttribute(entityType);
            if (tableAttribute == null) return null;

            // Find a table class that contains TableName.
            var tableTypes = entityType.Assembly.GetTypes();
            foreach (var tableType in tableTypes)
            {
                if (tableType.IsClass && (!tableType.IsAbstract || tableType.IsSealed))
                {
                    var members = tableType.GetAllMembers();
                    foreach (var member in members)
                    {
                        var tableNameAttr = member.GetCustomAttribute<DbTableNameAttribute>();
                        if (tableNameAttr != null)
                        {
                            var tableName = member.GetMemberValue(null)?.ToString();
                            if (tableName == tableAttribute.Name)
                            {
                                return new TableDefine(tableType);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Return null when TableDefine resolution fails so primary-key attribute detection remains the fallback.
            System.Diagnostics.Debug.WriteLine($"GetTableDefine failed for {entityType.Name}: {ex.Message}");
        }

        return null;
    }
}
