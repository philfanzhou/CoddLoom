# Third-party notices

CoddLoom is licensed under the MIT License. Its database-provider packages reference third-party drivers that remain governed by their own licenses.

| Dependency | Used by | License |
| --- | --- | --- |
| `Microsoft.Data.SqlClient` 7.0.2 | `CoddLoom.SqlServer` | MIT; its native SNI runtime has separate Microsoft redistribution terms |
| `Microsoft.Data.SqlClient.Extensions.Azure` 7.0.2 | `CoddLoom.SqlServer` | MIT |
| `System.Data.SQLite` 2.0.4 | `CoddLoom.Sqlite` | Public Domain, with separately licensed portions documented by System.Data.SQLite |
| `SourceGear.sqlite3` 3.53.4 | `CoddLoom.Sqlite` | Public Domain (SQLite) |
| `MySql.Data` 26.7.0 | `CoddLoom.MySql` | GPL-2.0-only WITH Universal-FOSS-exception-1.0 |
| `MySqlConnector` 2.6.2 | `CoddLoom.MariaDb` | MIT |
| `Oracle.ManagedDataAccess.Core` 23.26.300 | `CoddLoom.Oracle` | Oracle Free Distribution, Hosting, and Use Terms and Conditions |
| `Npgsql` 8.0.9 | `CoddLoom.PostgreSql` | PostgreSQL License |

License references:

- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient/blob/main/LICENSE)
- [Microsoft.Data.SqlClient.Extensions.Azure](https://github.com/dotnet/SqlClient/blob/main/LICENSE)
- [System.Data.SQLite copyright](https://system.data.sqlite.org/home/doc/trunk/www/copyright.wiki)
- [MySQL Connector/NET 26.7 licensing information](https://downloads.mysql.com/docs/licenses/connector-net-26.7-gpl-en.pdf)
- [MySqlConnector license](https://github.com/mysql-net/MySqlConnector/blob/master/LICENSE)
- [Oracle Free Distribution, Hosting, and Use Terms and Conditions](https://www.oracle.com/downloads/licenses/oracle-free-license.html)
- [Npgsql license](https://github.com/npgsql/npgsql/blob/main/LICENSE)

The dependency versions above describe the initial CoddLoom migration. Consumers should also review the license metadata of the exact dependency versions resolved by their build.
