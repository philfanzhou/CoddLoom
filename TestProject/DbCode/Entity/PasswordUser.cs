using Qz.Infra.Database.Entity;
using TestProject.DbCode.Tables;

namespace TestProject.DbCode.Entity
{
    [MapTable(Name = PasswordUserTable.TableName)]
    public class PasswordUser : User
    {
        [MapColumn(Name = PasswordUserTable.Password)]
        public string Password { get; set; }
    }
}
