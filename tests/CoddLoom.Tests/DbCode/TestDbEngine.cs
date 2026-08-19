using CoddLoom;
using CoddLoom.Table;
using System.Collections.Generic;
using CoddLoom.Tests.DbCode.Tables;

namespace CoddLoom.Tests.DbCode
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