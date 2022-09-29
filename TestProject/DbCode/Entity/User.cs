using QuantumZhou.Infrastructure.Data.Database.Entity;
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
    }
}
