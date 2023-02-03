using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Qz.Infra.Database.Sql
{
    public partial class SqlBuilder
    {
        protected const string KeyWordSelect = "SELECT";

        protected internal string ParamPrefix { get; set; } = "@";

        public virtual string GetCreateTableSql(TableDefine table)
        {
            return $"CREATE TABLE {table.Name}({GetCreateColumnsSql(table.Columns, table.PrimaryKey)})";
        }

        public virtual string Insert(string tableName, InputValues input)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (input == null || input.Items.Count < 1) throw new ArgumentNullException(nameof(input));

            var columnBuilder = new StringBuilder();
            var valueBuilder = new StringBuilder();
            foreach (var item in input.Items)
            {
                if (columnBuilder.Length > 0)
                {
                    columnBuilder.Append(",");
                }

                columnBuilder.Append(item.Column);

                if (valueBuilder.Length > 0)
                {
                    valueBuilder.Append(",");
                }

                valueBuilder.Append(ToValueSql(item.Value, item.Type));
            }

            return $"INSERT INTO {tableName} ({columnBuilder}) VALUES({valueBuilder})";
        }

        public virtual string Delete(string tableName, WhereConditions where)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (where == null || where.Items.Count < 1) throw new ArgumentNullException(nameof(where));

            var sql = $"DELETE FROM {tableName}";
            return AppendWhere(sql, where);
        }

        public virtual string Update(string tableName, InputValues input, WhereConditions where)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (where == null) throw new ArgumentNullException(nameof(where));

            var valueBuilder = new StringBuilder();
            foreach (var item in input.Items)
            {
                if (valueBuilder.Length > 0)
                {
                    valueBuilder.Append(", ");
                }

                valueBuilder.Append($"{item.Column} = ");
                valueBuilder.Append(ToValueSql(item.Value, item.Type));
            }

            var sql = $"UPDATE {tableName} SET {valueBuilder}";
            return AppendWhere(sql, where);
        }

        public virtual string Count(string tableName,
            WhereConditions where = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

            var column = "*";
            if (where != null && where.Items.Count > 0)
            {
                // 只查询where条件的第一个column，提高性能
                column = where.Items[0].Column;
            }

            var sql = $"SELECT COUNT({column}) FROM {tableName}";
            return AppendWhere(sql, where);
        }

        public virtual string Select(string tableName,
            WhereConditions where = null, OrderByCondition orderBy = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

            var sql = $"{KeyWordSelect} * FROM {tableName}";
            sql = AppendWhere(sql, where);
            return AppendOrderBy(sql, orderBy);
        }

        #region Protected virtual

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

        protected internal virtual string GetJoinTable(JoinConditions joinCondition)
        {
            if (joinCondition?.Columns == null
                || joinCondition.Columns.Count < 1
                || string.IsNullOrEmpty(joinCondition.Table1)
                || string.IsNullOrEmpty(joinCondition.Table2))
            {
                throw new ArgumentOutOfRangeException(nameof(joinCondition));
            }

            var columnBuilder = new StringBuilder();
            foreach (var item in joinCondition.Columns)
            {
                if (columnBuilder.Length > 1)
                {
                    columnBuilder.Append(" AND ");
                }

                columnBuilder.Append($"{joinCondition.Table1}.{item.Column1} = {joinCondition.Table2}.{item.Column2}");
            }

            var joinType = joinCondition.Type.ToString().ToUpper();
            return $"{joinCondition.Table1} {joinType} JOIN {joinCondition.Table2} ON {columnBuilder}";
        }

        protected virtual string AppendWhere(string sql,
            WhereConditions where = null)
        {
            if (where == null) return sql;

            var sqlBuilder = new StringBuilder();
            foreach (var item in where.Items)
            {
                if (sqlBuilder.Length > 0)
                {
                    sqlBuilder.Append(item.WhereConnecter == WhereConnecter.And ? " AND " : " OR ");
                }
                sqlBuilder.Append($"{item.Column}");
                if (item is WhereConditionsIsItem isItem)
                {
                    sqlBuilder.Append($" IS {(isItem.IsNull ? "NULL" : "NOT NULL")}");
                }
                else
                {
                    switch (item.WhereOperator)
                    {
                        case WhereOperator.Equal:
                            sqlBuilder.Append(" = ");
                            break;
                        case WhereOperator.NotEqual:
                            sqlBuilder.Append(" != ");
                            break;
                        case WhereOperator.GreaterThan:
                            sqlBuilder.Append(" > ");
                            break;
                        case WhereOperator.GreaterEqual:
                            sqlBuilder.Append(" >= ");
                            break;
                        case WhereOperator.LessThan:
                            sqlBuilder.Append(" < ");
                            break;
                        case WhereOperator.LessEqual:
                            sqlBuilder.Append(" <= ");
                            break;
                        case WhereOperator.Like:
                            sqlBuilder.Append(" LIKE ");
                            break;
                    }
                    sqlBuilder.Append($"{ParamPrefix}{item.ParamName}");
                }
            }

            return $"{sql} WHERE {sqlBuilder}";
        }

        protected virtual string AppendOrderBy(string sql,
            OrderByCondition orderBy = null)
        {
            if (orderBy == null) return sql;

            var sort = orderBy.Descending ? "DESC" : "ASC";
            return $"{sql} ORDER BY {orderBy.Column} {sort}";
        }

        protected virtual string AppendLimit(string sql, int count,
            int offset = 0)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

            return $"{sql} LIMIT {offset},{count}";
        }

        protected virtual string ToValueSql(string value, DbType dbType)
        {
            switch (dbType)
            {
                case DbType.String:
                    return $"'{value}'";
                case DbType.Int32:
                case DbType.Int16:
                case DbType.Decimal:
                    return $"{value}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null);
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

        #endregion
    }
}
