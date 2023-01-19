using System;
using System.Collections.Generic;

namespace Qz.Infra.Database
{
    public static class DbContainer
    {
        private static readonly Dictionary<string, DbEngine> Engines = new();

        public static void Add(DbEngine engine)
        {
            var name = engine.GetType().Name;
            Add(name, engine);
        }

        public static void Add(string engineName, DbEngine engine)
        {
            if (Engines.ContainsKey(engineName))
            {
                throw new InvalidOperationException($"{engineName} exists");
            }
            Engines[engineName] = engine;
        }

        public static T Get<T>() where T : DbEngine
        {
            var name = typeof(T).Name;
            return (T) Get(name);
        }

        public static DbEngine Get(string engineName)
        {
            return Engines[engineName];
        }
    }
}
