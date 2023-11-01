using Qz.Infra.Database.Cache;

namespace Qz.Infra.Database.Sql.Base;

public abstract class SqlBuilderParam
{
    protected SqlBuilderParam(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            throw new System.ArgumentNullException(nameof(tableName));
        }

        TableName = tableName;
    }

    public string TableName { get; }

}