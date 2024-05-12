using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Common;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using System;
using System.Collections.Generic;
using Qz.Infra.Database.Entity;

namespace Qz.Infra.Database.Convert;

partial class DbConverter
{
    internal static void ToInsert<T>(IEnumerable<T> entities, 
        out string tableName, out List<InputValues> inputs)
    {
        if(entities == null) 
        {
            throw new ArgumentNullException(nameof(entities));
        }

        var entityMap = GetEntityInfo<T>(out tableName, out var insertColumns);

        inputs = new List<InputValues>();
        var index = 0;
        foreach(var entity in entities)
        {
            inputs.Add(GetInputValues(entity, entityMap, insertColumns, index));
            index++;
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

            input.Add(attribute.Name, value, true);
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

    private static EntityMap GetEntityInfo<T>(out string tableName, out List<string> insertColumns)
    {
        var entityMap = EntityMapCache.Get<T>();
        tableName = entityMap.Table.Name;

        insertColumns = TableColumnsCache.GetInsertColumns(tableName);
        if (insertColumns == null)
        {
            throw new InvalidOperationException("Can't get insert columns");
        }

        return entityMap;
    }

    private static InputValues GetInputValues<T>(T entity, EntityMap entityMap, List<string> insertColumns, 
        int inputIndex = 0)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var input = new InputValues(inputIndex);
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            if (!insertColumns.Contains(attribute.Name))
            {
                continue;
            }

            var value = memberInfo.GetMemberValue(entity);
            input.Add(attribute.Name, value, attribute.ForceParameter);
        }
        return input;
    }
}