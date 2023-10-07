using Qz.Infra.Database.Entity;
using System;
using TestProject.DbCode.Tables;

namespace TestProject.DbCode.Entity
{
    [MapTable(Name = UserTable.TableName)]
    public class User
    {
        [MapColumn(Name = UserTable.Id)]
        public string Id { get; set; }

        [MapColumn(Name = UserTable.UnionId)]
        public string UnionId { get; set; }

        [MapColumn(Name = UserTable.RegistrationDate)]
        public DateTime RegistrationDate { get; set; }
    }
}
