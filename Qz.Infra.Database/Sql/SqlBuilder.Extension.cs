using Qz.Infra.Database.Sql.Base;

namespace Qz.Infra.Database.Sql;

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

    public string Count(SqlBuilderWhereParam builderParam)
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
}