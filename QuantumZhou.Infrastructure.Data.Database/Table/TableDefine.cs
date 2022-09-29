using System;
using System.Collections.Generic;
using System.Reflection;

namespace QuantumZhou.Infrastructure.Data.Database.Table
{
    public class TableDefine
    {
        public TableDefine(IReflect tableType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                       BindingFlags.Static;

            var tempTableName = new List<MemberInfo>();
            var tempColumns = new List<MemberInfo>();
            var tempPrimaryKey = new List<MemberInfo>();

            var members = tableType.GetMembers(flags);
            foreach (var member in members)
            {
                var attributes = member.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                {
                    if (attribute is DbTableNameAttribute)
                    {
                        tempTableName.Add(member);
                        break;
                    }

                    if (attribute is DbColumnAttribute)
                    {
                        tempColumns.Add(member);
                        break;
                    }

                    if (attribute is DbPrimaryKeyAttribute)
                    {
                        tempPrimaryKey.Add(member);
                        break;
                    }
                }
            }

            #region Check

            if (tempTableName.Count < 1)
            {
                throw new InvalidOperationException("Table name not found.");
            }

            if (tempTableName.Count > 1)
            {
                var msg = "Table name: ";
                foreach (var item in tempTableName)
                {
                    msg += $"'{item.Name}' ";
                }

                msg += "is not allow.";
                throw new InvalidOperationException(msg);
            }

            if (tempColumns.Count < 1 && tempPrimaryKey.Count < 1)
            {
                throw new InvalidOperationException("Table column not found.");
            }

            if (tempPrimaryKey.Count > 1)
            {
                var msg = "Primary key: ";
                foreach (var item in tempPrimaryKey)
                {
                    msg += $"'{item.Name}' ";
                }

                msg += "is not allow.";
                throw new InvalidOperationException(msg);
            }

            #endregion

            Name = GetMemberValue(tempTableName[0]);

            var columns = new List<DbColumnAttribute>();
            foreach (var column in tempColumns)
            {
                var columnAttribute = column.GetCustomAttribute<DbColumnAttribute>();
                columnAttribute.Name = GetMemberValue(column);
                columns.Add(columnAttribute);
            }

            Columns = columns.AsReadOnly();

            if (tempPrimaryKey.Count < 1)
            {
                PrimaryKey = null;
            }
            else
            {
                var primaryKeyAttribute = tempPrimaryKey[0].GetCustomAttribute<DbPrimaryKeyAttribute>();
                primaryKeyAttribute.Name = GetMemberValue(tempPrimaryKey[0]);
                PrimaryKey = primaryKeyAttribute;
            }
        }

        public string Name { get; protected set; }

        public DbPrimaryKeyAttribute PrimaryKey { get; protected set; }

        public IReadOnlyList<DbColumnAttribute> Columns { get; protected set; }

        #region Private method

        private static string GetMemberValue(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field)
            {
                return field.GetValue(null).ToString();
            }

            if (memberInfo is PropertyInfo property)
            {
                return property.GetValue(null).ToString();
            }

            return string.Empty;
        }

        #endregion
    }
}
