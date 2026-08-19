# Third-party notices

CoddLoom is licensed under the MIT License. Its database-provider packages reference third-party drivers that remain governed by their own licenses.

| Dependency | Used by | License |
| --- | --- | --- |
| `Microsoft.Data.SqlClient` 6.1.2 | `CoddLoom.SqlServer` | MIT; its native SNI runtime has separate Microsoft redistribution terms |
| `System.Data.SQLite.Core` 1.0.119 | `CoddLoom.Sqlite` | Public Domain, with separately licensed portions documented by System.Data.SQLite |
| `MySql.Data` 9.5.0 | `CoddLoom.MySql` | GPL-2.0-only WITH Universal-FOSS-exception-1.0 |
| `MySqlConnector` 2.4.0 | `CoddLoom.MariaDb` | MIT |
| `Oracle.ManagedDataAccess.Core` 23.26.0 | `CoddLoom.Oracle` | Oracle Free Distribution, Hosting, and Use Terms and Conditions |

License references:

- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient/blob/main/LICENSE)
- [System.Data.SQLite copyright](https://system.data.sqlite.org/home/doc/trunk/www/copyright.wiki)
- [MySQL Connector/NET 9.5 licensing information](https://downloads.mysql.com/docs/licenses/connector-net-9.5-gpl-en.pdf)
- [MySqlConnector license](https://github.com/mysql-net/MySqlConnector/blob/master/LICENSE)
- [Oracle Free Distribution, Hosting, and Use Terms and Conditions](https://www.oracle.com/downloads/licenses/oracle-free-license.html)

The dependency versions above describe the initial CoddLoom migration. Consumers should also review the license metadata of the exact dependency versions resolved by their build.
