using CoddLoom.Tests.DbCode;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CoddLoom.Tests.DbTest;

[TestClass]
public class DbContainerIntegrationTest : TestBase
{
    [TestMethod]
    public void AddAndGet_ResolveNamedAndTypedEngines()
    {
        var engine = new ContainerTestEngine(Executor);
        var uniqueName = $"engine-{Guid.NewGuid():N}";

        DbContainer.Add(uniqueName, engine);
        DbContainer.Add(engine);

        Assert.AreSame(engine, DbContainer.Get(uniqueName));
        Assert.AreSame(engine, DbContainer.Get<ContainerTestEngine>());
        Assert.IsNull(DbContainer.Get($"missing-{Guid.NewGuid():N}"));
    }

    private sealed class ContainerTestEngine(DbExecutor executor) : TestDbEngine(executor);
}
