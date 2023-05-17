using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System;
using System.Data;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

public static partial class DbConverter
{
    internal static SqlBuilderInsertParam ToInsert<T>(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var entityMap = EntityMapCache.Get<T>();
        var columns = TableColumnsCache.GetInsertColumns(entityMap.Table.Name);
        if (columns == null)
        {
            return null;
        }

        var input = new InputValues();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            if (columns.Contains(attribute.Name))
            {
                var value = memberInfo.GetEntityValue(entity);
                // do not add empty value while insert.
                if (!string.IsNullOrEmpty(value))
                {
                    input.Add(attribute.Name, value, memberInfo.GetDbType());
                }
            }
        }

        var builderParam = new SqlBuilderInsertParam<T>(input);
        return builderParam;
    }

    internal static SqlBuilderUpdateParam ToUpdate<T>(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var entityMap = EntityMapCache.Get<T>();
        var columns = TableColumnsCache.GetUpdateColumns(entityMap.Table.Name);
        if (columns == null)
        {
            return null;
        }

        var input = new InputValues();
        WhereParams whereParams = null;
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            if (columns.Contains(attribute.Name))
            {
                var value = memberInfo.GetEntityValue(entity);
                input.Add(attribute.Name, value, memberInfo.GetDbType());
            }
            else if (attribute.PrimaryKey)
            {
                var keyValue = memberInfo.GetEntityValue(entity);
                if (string.IsNullOrEmpty(keyValue) == false)
                {
                    whereParams = new WhereParams(attribute.Name, keyValue);
                }
            }
        }

        return whereParams == null ? null : new SqlBuilderUpdateParam<T>(input, whereParams);
    }

    private static string GetEntityValue<T>(this MemberInfo member, T entity)
    {
        object obj = null;
        if (member is FieldInfo field)
        {
            obj = field.GetValue(entity);
        }
        else if (member is PropertyInfo property)
        {
            obj = property.GetValue(entity);
        }

        return obj != null ? obj.ToString() : string.Empty;
    }

    private static DbType GetDbType(this MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            return DbTypeConverter.ToDbType(field.FieldType);
        }

        if (member is PropertyInfo property)
        {
            return DbTypeConverter.ToDbType(property.PropertyType);
        }

        throw new NotSupportedException($"Member type {member.GetType()} not supported.");
    }
}