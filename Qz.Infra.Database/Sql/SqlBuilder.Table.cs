using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Qz.Infra.Database.Sql
{
    partial class SqlBuilder
    {
        protected internal virtual string GetCreateTableSql(TableDefine table)
        {
            return $"CREATE TABLE {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
        }

        protected virtual string GetCreateColumnsSql(IEnumerable<DbColumnAttribute> columns,
            DbPrimaryKeyAttribute primaryKey = null)
        {
            var columnBuilder = new StringBuilder();

            if (primaryKey != null)
            {
                columnBuilder.Append(GetPrimaryKeySql(primaryKey));
            }

            foreach (var column in columns)
            {
                if (columnBuilder.Length > 0)
                {
                    columnBuilder.Append(",");
                }

                columnBuilder.Append(GetColumnSql(column));
            }

            return columnBuilder.ToString();
        }

        protected virtual string GetPrimaryKeySql(DbPrimaryKeyAttribute primaryKey)
        {
            return $"{primaryKey.Name} {ToColumnSql(primaryKey)} PRIMARY KEY NOT NULL";
        }

        protected virtual string GetColumnSql(DbColumnAttribute column)
        {
            if (column.AllowEmpty)
            {
                return $"{column.Name} {ToColumnSql(column)}";
            }
            else
            {
                return $"{column.Name} {ToColumnSql(column)} NOT NULL";
            }
        }

        protected virtual string ToColumnSql(DbColumnBaseAttribute column)
        {
            return column.Type switch
            {
                DbType.String => column.Length > 50 ? $"VARCHAR({column.Length})" : $"CHAR({column.Length})",
                DbType.Int32 => "INTEGER",
                DbType.Int16 => "SMALLINT",
                DbType.Decimal => "DECIMAL(18,2)",
                _ => throw new NotSupportedException($"{column.Type} not support.")
            };
        }
    }
}
