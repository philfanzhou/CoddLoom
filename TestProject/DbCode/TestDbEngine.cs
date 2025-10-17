using Qz.Infra.Database;
using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Table;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TestProject.DbCode.Tables;

namespace TestProject.DbCode
{
    public class TestDbEngine : DbEngine
    {
        public TestDbEngine(DbExecutor executor)
            : base(executor, new List<TableDefine>
            {
                new(typeof(UserTable))
            })
        {
        }
    }
}
