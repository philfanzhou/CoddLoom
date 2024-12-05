using MySql.Data.MySqlClient;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Sql;
using Qz.Infra.Database.Table;
using System;
using System.Data;

namespace Qz.Infra.Database.MySql;

public class MySqlExecutor : DbExecutor
{
    public MySqlExecutor(string connectionString)
        : base(connectionString, new MySqlConnection(connectionString))
    {
    }

    public MySqlExecutor(string server, string database, string user, string password, uint port = 3306)
        : this(BuildConnectionString(server, database, user, password, port))
    {
    }

    public override SqlBuilder SqlBuilder { get; } = new MySqlBuilder();

    public override IDbConnection GetConnection()
    {
        return new MySqlConnection(ConnectionString);
    }

    protected override void GetExistTableParam(TableDefine table, out string checkTable, out WhereConditions where)
    {
        // use create table sql to check exist, not here
        checkTable = null;
        where = null;
    }

    protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
    {
        if (command is not MySqlCommand cmd)
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        return cmd.Parameters.AddWithValue;
    }

    private static string BuildConnectionString(string server, string database,
        string user, string password, uint port)
    {
        var connStrBuilder = new MySqlConnectionStringBuilder
        {
            Server = server,
            UserID = user,
            Password = password,
            Port = port
        };

        var checkDbSql = $"CREATE DATABASE IF NOT EXISTS {database}";
        using var connection = new MySqlConnection(connStrBuilder.ConnectionString);
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = checkDbSql;
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }

        connStrBuilder.Database = database;
        return connStrBuilder.ConnectionString;
    }
}