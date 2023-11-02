using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using System;
using System.Reflection;

namespace Qz.Infra.Database.Convert;

partial class DbConverter
{
    internal static InputValues ToInsert<T>(T entity)
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

        return input;
    }

    internal static void ToUpdate<T>(T entity, out InputValues input, out WhereConditions where)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        input = null;
        where = null;

        var entityMap = EntityMapCache.Get<T>();
        var updateColumns = TableColumnsCache.GetUpdateColumns(entityMap.Table.Name);
        if (updateColumns == null)
        {
            return;
        }

        input = new InputValues();
        where = new WhereConditions();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            var value = memberInfo.GetEntityValue(entity);

            if (updateColumns.Contains(attribute.Name))
            {
                input.Add(attribute.Name, value);
            }
            else if (attribute.PrimaryKey && value != null)
            {
                where.Add(attribute.Name, value);
            }
        }
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