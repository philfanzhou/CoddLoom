using Qz.Infra.Database.Common;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Qz.Infra.Database.Entity;

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

                // 优先使用TableDefine中定义的主键，如果未指定则使用Entity的PrimaryKey属性
                if (primaryKeyFromTable != null && attribute.Name == primaryKeyFromTable)
                {
                    PrimaryKey = attribute.Name;
                }
                else if (attribute.PrimaryKey && string.IsNullOrEmpty(PrimaryKey))
                {
                    // 如果TableDefine未指定主键，但Entity中标记了PrimaryKey，则使用Entity的标记
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
                if (tableType.IsClass && !tableType.IsAbstract)
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
        catch
        {
            // 如果获取TableDefine失败，返回null，让代码回退到原有的PrimaryKey属性检测
        }

        return null;
    }
}