using Qz.Infra.Database.Common;
using Qz.Infra.Database.Table.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Qz.Infra.Database.Table;

public class TableDefine
{
    private const BindingFlags MemberFlags = BindingFlags.Public
                                             | BindingFlags.NonPublic
                                             | BindingFlags.Instance
                                             | BindingFlags.Static;

    public string Name { get; protected set; }

    public IReadOnlyList<DbColumnBaseAttribute> Columns { get; protected set; }

    public DbPrimaryKeyBaseAttribute PrimaryKey { get; protected set; }

    public TableDefine(IReflect tableType)
    {
        GetTableMembers(tableType, out var tableNameInfo, out var columnInfo, out var primaryKeyInfo);

        CheckTableName(tableNameInfo);
        CheckColumns(columnInfo, primaryKeyInfo);

        Name = tableNameInfo[0].GetMemberValue(null).ToString();
        Columns = GetColumns(columnInfo).AsReadOnly();
        PrimaryKey = GetPrimaryKey(primaryKeyInfo);
    }

    #region Private method

    private static void GetTableMembers(IReflect tableType,
        out List<MemberInfo> tableName, out List<MemberInfo> columns, out List<MemberInfo> primaryKey)
    {
        tableName = new List<MemberInfo>();
        columns = new List<MemberInfo>();
        primaryKey = new List<MemberInfo>();

        var members = tableType.GetMembers(MemberFlags);
        foreach (var member in members)
        {
            var attributes = member.GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                if (attribute is DbTableNameAttribute)
                {
                    tableName.Add(member);
                    break;
                }

                if (attribute is DbColumnBaseAttribute)
                {
                    columns.Add(member);
                    break;
                }

                if (attribute is DbPrimaryKeyBaseAttribute)
                {
                    primaryKey.Add(member);
                    break;
                }
            }
        }
    }

    private static void CheckTableName(List<MemberInfo> tableNameInfo)
    {
        if (tableNameInfo.Count < 1)
        {
            throw new InvalidOperationException("Table name not found.");
        }

        if (tableNameInfo.Count > 1)
        {
            var msg = "Table name: ";
            foreach (var item in tableNameInfo)
            {
                msg += $"'{item.Name}' ";
            }

            msg += "is not allow.";
            throw new InvalidOperationException(msg);
        }
    }

    private static void CheckColumns(ICollection columnInfo, List<MemberInfo> primaryKeyInfo)
    {
        if (columnInfo.Count < 1 && primaryKeyInfo.Count < 1)
        {
            throw new InvalidOperationException("Table columns not found.");
        }

        if (primaryKeyInfo.Count > 1)
        {
            var msg = "Primary key: ";
            foreach (var item in primaryKeyInfo)
            {
                msg += $"'{item.Name}' ";
            }

            msg += "is not allow.";
            throw new InvalidOperationException(msg);
        }
    }

    private static List<DbColumnBaseAttribute> GetColumns(List<MemberInfo> columnInfo)
    {
        var columns = new List<DbColumnBaseAttribute>();
        foreach (var column in columnInfo)
        {
            var columnAttribute = column.GetCustomAttribute<DbColumnBaseAttribute>();
            columnAttribute.Name = column.GetMemberValue(null).ToString();
            columns.Add(columnAttribute);
        }

        return columns;
    }

    private static DbPrimaryKeyBaseAttribute GetPrimaryKey(IReadOnlyList<MemberInfo> primaryKeyInfo)
    {
        if (primaryKeyInfo.Count < 1)
        {
            return null;
        }

        var primaryKeyAttribute = primaryKeyInfo[0].GetCustomAttribute<DbPrimaryKeyBaseAttribute>();
        primaryKeyAttribute.Name = primaryKeyInfo[0].GetMemberValue(null).ToString();
        return primaryKeyAttribute;
    }

    #endregion
}