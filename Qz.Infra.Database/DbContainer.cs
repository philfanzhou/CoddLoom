using System.Collections.Concurrent;

namespace Qz.Infra.Database;

public static class DbContainer
{
    private const string DefaultName = nameof(DbEngine);
    private static readonly ConcurrentDictionary<string, DbEngine> Engines = new();

    public static void Add(string engineName, DbEngine engine)
    {
        Engines.TryAdd(DefaultName, engine);
        Engines.TryAdd(engineName, engine);
        //Engines.AddOrUpdate(engineName, engine, (_, _) => engine);
    }

    public static void Add(DbEngine engine)
    {
        var name = engine.GetType().Name;
        Add(name, engine);
    }

    public static DbEngine Get(string engineName = null)
    {
        if(string.IsNullOrEmpty(engineName))
        {
            engineName = DefaultName;
        }
        return Engines.TryGetValue(engineName, out var engine) ? engine : null;
    }

    public static T Get<T>() where T : DbEngine
    {
        var name = typeof(T).Name;
        return (T)Get(name);
    }
}