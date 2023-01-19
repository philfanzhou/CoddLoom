using Qz.Infra.Database.Entity;
using TestProject.DbCode.Tables;

namespace TestProject.DbCode.Entity
{
    [MapTable(Name = TenantTable.TableName)]
    public class Tenant
    {
        [MapColumn(Name = TenantTable.Id)] 
        public string Id { get; set; }
    }
}