using Qz.Infra.Database.Cache;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

partial class SqlBuilder
{
    public SqlBuilderInsertParam CreateInsert(string tableName, InputValues inputValues)
    {
        return new SqlBuilderInsertParam(tableName, inputValues);
    }

    public SqlBuilderInsertParam CreateInsert<T>(InputValues inputValues)
    {
        return CreateInsert(GetTableName<T>(), inputValues);
    }

    public SqlBuilderDeleteParam CreateDelete(string tableName, WhereConditions where)
    {
        return new SqlBuilderDeleteParam(tableName, where);
    }

    public SqlBuilderDeleteParam CreateDelete<T>(WhereConditions where)
    {
        return CreateDelete(GetTableName<T>(), where);
    }

    public SqlBuilderUpdateParam CreateUpdate(string tableName, InputValues inputValues, WhereConditions where)
    {
        return new SqlBuilderUpdateParam(tableName, inputValues, where);
    }

    public SqlBuilderUpdateParam CreateUpdate<T>(InputValues inputValues, WhereConditions where)
    {
        return CreateUpdate(GetTableName<T>(), inputValues, where);
    }

    public SqlBuilderSelectParam CreateSelect(string tableName, WhereConditions where, 
        OrderByCondition orderBy = null)
    {
        return new SqlBuilderSelectParam(tableName, where, orderBy);
    }

    public SqlBuilderSelectParam CreateSelect(string tableName,
        OrderByCondition orderBy = null)
    {
        return CreateSelect(tableName, null, orderBy);
    }

    public SqlBuilderSelectParam CreateSelect<T>( WhereConditions where, 
        OrderByCondition orderBy = null)
    {
        return CreateSelect(GetTableName<T>(), where, orderBy);
    }

    public SqlBuilderSelectParam CreateSelect<T>(
        OrderByCondition orderBy = null)
    {
        return CreateSelect<T>(null, orderBy);
    }

    public SqlBuilderSelectParam CreateSelect(JoinConditions join, WhereConditions where, 
        OrderByCondition orderBy = null)
    {
        return CreateSelect(GetJoinTable(join), where, orderBy);
    }

    public SqlBuilderSelectParam CreateSelect(JoinConditions join,
        OrderByCondition orderBy = null)
    {
        return CreateSelect(join, null, orderBy);
    }

    public SqlBuilderWhereParam CreateCount(string tableName,
        WhereConditions where = null)
    {
        return new SqlBuilderWhereParam(tableName, where);
    }

    public SqlBuilderWhereParam CreateCount<T>(
        WhereConditions where = null)
    {
        return CreateCount(GetTableName<T>(), where);
    }

    public SqlBuilderWhereParam CreateCount(JoinConditions join,
        WhereConditions where = null)
    {
        return CreateCount(GetJoinTable(join), where);
    }

    private string GetTableName<T>()
    {
        var entityMap = EntityMapCache.Get<T>();
        return entityMap.Table.Name;
    }
}