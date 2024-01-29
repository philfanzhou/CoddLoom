using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Entity;
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
            SetInputItems(input, memberInfo, attribute, value);
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

            if (attribute.PrimaryKey)
            {
                where.Add(attribute.Name, value);
                continue;
            }

            if (!updateColumns.Contains(attribute.Name))
            {
                continue;
            }

            SetInputItems(input, memberInfo, attribute, value);
        }
    }

    #region Private method

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

    private static string GetMemberType(this MemberInfo member)
    {
        string type = null;
        if (member is FieldInfo field)
        {
            type = field.FieldType.FullName;
        }
        else if (member is PropertyInfo property)
        {
            type = property.PropertyType.FullName;
        }

        return type;
    }

    private static void SetInputItems(InputValues input, MemberInfo memberInfo, MapColumnAttribute attribute, object value)
    {
        if (value == null)
        {
            input.Add(attribute.Name, null);
        }
        else
        {
            var memberType = memberInfo.GetMemberType();
            if (memberType == "System.String")
            {
                input.Add(attribute.Name, value.ToString());
            }
            else
            {
                input.Add(attribute.Name, value);
            }
        }
    }

    #endregion
}