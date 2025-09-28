using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Common;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Entity;
using Qz.Infra.Database.Input;
using System;
using System.Collections.Generic;

namespace Qz.Infra.Database.Convert;

partial class DbConverter
{
    internal static void ToInsert<T>(T entity, 
        out string tableName, out List<InputValues> inputs)
    {
        if(entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
        ToInsert([entity], out tableName, out inputs);
    }

    internal static void ToInsert<T>(IEnumerable<T> entities, 
        out string tableName, out List<InputValues> inputs)
    {
        if(entities == null) 
        {
            throw new ArgumentNullException(nameof(entities));
        }

        var entityMap = EntityMapCache.Get<T>();
        tableName = entityMap.Table.Name;
        var insertColumns = TableColumnsCache.GetInsertColumns(tableName);
        if (insertColumns == null)
        {
            throw new InvalidOperationException("Can't get insert columns");
        }

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

        var entityMap = EntityMapCache.Get<T>();
        tableName = entityMap.Table.Name;
        var updateColumns = TableColumnsCache.GetUpdateColumns(entityMap.Table.Name);
        if (updateColumns == null)
        {
            throw new InvalidOperationException("Can't get update columns");
        }

        input = new InputValues();
        where = new WhereConditions();
        foreach (var (memberInfo, attribute) in entityMap.Members)
        {
            var value = memberInfo.GetMemberValue(entity);

            if (attribute.PrimaryKey)
            {
                where.Add(attribute.Name, value);
            }
            else if (updateColumns.Contains(attribute.Name))
            {
                input.Add(attribute.Name, value, true);
            }
        }
    }

    private static InputValues GetInputValues<T>(T entity, EntityMap entityMap, 
        ICollection<string> insertColumns, int inputIndex = 0)
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