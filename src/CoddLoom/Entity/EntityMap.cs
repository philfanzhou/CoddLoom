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

        // 获取TableDefine以确定主键
        var tableDefine = GetTableDefine(type);
        string primaryKeyFromTable = tableDefine?.PrimaryKey?.Name;

        foreach (var member in members)
        {
            var attribute = member.GetCustomAttribute<MapColumnAttribute>(true);
            if (attribute != null)
            {
                memberList.Add(new Tuple<MemberInfo, MapColumnAttribute>(member, attribute));

                // 主键确定逻辑：
                // 1. 如果TableDefine中定义了主键，且Entity中的属性映射到该主键字段名，则认为是主键（不需要Entity中标记PrimaryKey=true）
                if (primaryKeyFromTable != null && attribute.Name == primaryKeyFromTable)
                {
                    // TableDefine中已定义主键，且Entity中的属性映射到该主键字段，则认为是主键
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
            // 尝试通过Table名称找到对应的Table类型
            var tableAttribute = GetTableAttribute(entityType);
            if (tableAttribute == null) return null;

            // 查找包含TableName的Table类
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
            // 如果获取TableDefine失败，返回null，让代码回退到原有的PrimaryKey属性检测
            System.Diagnostics.Debug.WriteLine($"GetTableDefine failed for {entityType.Name}: {ex.Message}");
        }

        return null;
    }
}