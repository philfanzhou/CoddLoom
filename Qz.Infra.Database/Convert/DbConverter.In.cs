using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Common;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using System;

namespace Qz.Infra.Database.Convert;

partial class DbConverter
{
    internal static void ToInsert<T>(T entity,
        out string tableName, out InputValues input)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        tableName = null;
        input = null;

        var entityMap = EntityMapCache.Get<T>();
        var insertColumns = TableColumnsCache.GetInsertColumns(entityMap.Table.Name);
        if (insertColumns == null)
        {
            return;
        }

        tableName = entityMap.Table.Name;
        input = new InputValues();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            if (!insertColumns.Contains(attribute.Name))
            {
                continue;
            }

            var value = memberInfo.GetMemberValue(entity);
            input.Add(attribute.Name, value);
        }
    }

    internal static void ToUpdate<T>(T entity,
        out string tableName, out InputValues input, out WhereConditions where)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        tableName = null;
        input = null;
        where = null;

        var entityMap = EntityMapCache.Get<T>();
        var updateColumns = TableColumnsCache.GetUpdateColumns(entityMap.Table.Name);
        if (updateColumns == null)
        {
            return;
        }

        tableName = entityMap.Table.Name;
        input = new InputValues();
        where = new WhereConditions();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            var value = memberInfo.GetMemberValue(entity);

            if (attribute.PrimaryKey)
            {
                where.Add(attribute.Name, value);
                continue;
            }

            if (!updateColumns.Contains(attribute.Name))
            {
                continue;
            }

            input.Add(attribute.Name, value);
        }
    }

    internal static void ToDelete<T>(string id,
        out string tableName, out WhereConditions where)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        tableName = null;
        where = null;

        var entityMap = EntityMapCache.Get<T>();
        if (string.IsNullOrEmpty(entityMap.PrimaryKey))
        {
            throw new ArgumentException($"{nameof(T)} does not have a primary key");
        }

        tableName = entityMap.Table.Name;
        where = new WhereConditions();
        where.Add(entityMap.PrimaryKey, id);
    }
}