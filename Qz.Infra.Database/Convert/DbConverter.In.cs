using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System;
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
        var insertColumns = TableColumnsCache.GetInsertColumns(entityMap.Table.Name);
        if (insertColumns == null)
        {
            return null;
        }

        var input = new InputValues();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            if (!insertColumns.Contains(attribute.Name))
            {
                continue;
            }

            var value = memberInfo.GetEntityValue(entity);
            if (value != null)
            {
                input.Add(attribute.Name, value);
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
        var updateColumns = TableColumnsCache.GetUpdateColumns(entityMap.Table.Name);
        if (updateColumns == null)
        {
            return null;
        }

        var input = new InputValues();
        WhereParams whereParams = null;
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            var value = memberInfo.GetEntityValue(entity);

            if (updateColumns.Contains(attribute.Name))
            {
                input.Add(attribute.Name, value);
            }
            else if (attribute.PrimaryKey)
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                {
                    whereParams = new WhereParams(attribute.Name, value.ToString());
                }
            }
        }

        return whereParams == null ? null : new SqlBuilderUpdateParam<T>(input, whereParams);
    }

    private static object GetEntityValue<T>(this MemberInfo member, T entity)
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

        return obj;
    }
}