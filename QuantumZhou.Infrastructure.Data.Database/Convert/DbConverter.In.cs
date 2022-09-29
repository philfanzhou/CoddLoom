using QuantumZhou.Infrastructure.Data.Database.Cache;
using QuantumZhou.Infrastructure.Data.Database.Entity;
using QuantumZhou.Infrastructure.Data.Database.Input;
using QuantumZhou.Infrastructure.Data.Database.Params;
using QuantumZhou.Infrastructure.Data.Database.Sql;
using System;
using System.Reflection;

namespace QuantumZhou.Infrastructure.Data.Database.Convert
{
    internal static partial class DbConverter
    {
        internal static SqlBuilderInsertParam ToInsert<T>(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entityMap = EntityMapCache.Get<T>();
            var columns = TableColumnsCache.GetTableInsertColumns(entityMap.Table.Name);
            if (columns == null)
            {
                return null;
            }

            var input = new InputValues();
            foreach (var (memberInfo, attribute) in entityMap.Members)
            {
                if (columns.Contains(attribute.Name))
                {
                    AddToInput(input, memberInfo, attribute, entity);
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
            var columns = TableColumnsCache.GetTableUpdateColumns(entityMap.Table.Name);
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
                    AddToInput(input, memberInfo, attribute, entity);
                }
                else if (attribute.PrimaryKey)
                {
                    var keyValue = GetMemberValue(memberInfo, entity);
                    if (string.IsNullOrEmpty(keyValue) == false)
                    {
                        whereParams = new WhereParams(attribute.Name, keyValue);
                    }
                }
            }

            return whereParams == null ? null : new SqlBuilderUpdateParam<T>(input, whereParams);
        }

        private static void AddToInput<T>(InputValues input, MemberInfo member, MapColumnAttribute attribute, T entity)
        {
            if (member is FieldInfo field)
            {
                input.Add(attribute.Name, field.GetValue(entity).ToString(), DbTypeConverter.ToDbType(field.FieldType));
            }
            else if (member is PropertyInfo property)
            {
                input.Add(attribute.Name, property.GetValue(entity).ToString(), DbTypeConverter.ToDbType(property.PropertyType));
            }
        }

        private static string GetMemberValue<T>(MemberInfo member, T entity)
        {
            if (member is FieldInfo field)
            {
                return field.GetValue(entity).ToString();
            }

            if (member is PropertyInfo property)
            {
                return property.GetValue(entity).ToString();
            }

            return string.Empty;
        }
    }
}
