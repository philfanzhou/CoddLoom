using Qz.Infra.Database.Entity;
using TestProject.DbCode.Tables;

namespace TestProject.DbCode.Entity;

[MapTable(Name = BatchRecordTable.TableName)]
internal class BatchRecord
{
    [MapColumn(Name = BatchRecordTable.Id)]
    public int Id { get; set; }

    [MapColumn(Name = BatchRecordTable.Name)]
    public string Name { get; set; }
}
