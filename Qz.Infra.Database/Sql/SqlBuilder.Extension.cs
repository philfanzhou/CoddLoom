using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Sql
{
    partial class SqlBuilder
    {
        public string Insert(SqlBuilderInsertParam builderParam)
        {
            return Insert(builderParam.TableName, builderParam.Values);
        }

        public string Delete(SqlBuilderDeleteParam builderParam)
        {
            return Delete(builderParam.TableName, builderParam.WhereConditions);
        }

        public string Update(SqlBuilderUpdateParam builderParam)
        {
            return Update(builderParam.TableName, builderParam.Values, builderParam.WhereConditions);
        }

        public string Count(SqlBuilderCountParam builderParam)
        {
            return Count(builderParam.TableName, builderParam.WhereConditions);
        }

        public string Select(SqlBuilderSelectParam builderParam)
        {
            return Select(builderParam.TableName, builderParam.WhereConditions, builderParam.OrderBy);
        }

        public string First(SqlBuilderSelectParam builderParam)
        {
            return First(builderParam.TableName, builderParam.WhereConditions, builderParam.OrderBy);
        }

        public string Take(SqlBuilderSelectParam builderParam, int offset, int count)
        {
            return Take(builderParam.TableName, offset, count, builderParam.WhereConditions, builderParam.OrderBy);
        }

        public virtual string First(string tableName,
            WhereConditions where = null, OrderByCondition orderBy = null)
        {
            var selectSql = Select(tableName, where, orderBy);
            return AppendLimit(selectSql, 1);
        }

        public virtual string Take(string tableName, int offset, int count,
            WhereConditions where = null, OrderByCondition orderBy = null)
        {
            var selectSql = Select(tableName, where, orderBy);
            return AppendLimit(selectSql, count, offset);
        }
    }
}
