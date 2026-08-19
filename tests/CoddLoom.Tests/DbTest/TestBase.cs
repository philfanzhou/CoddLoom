using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom;
using System;
using CoddLoom.Tests.DbCode;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// Base class that provides consistent database-executor management and cleanup.
    /// </summary>
    [TestClass]
    [TestCategory("Database")]
    public abstract class TestBase
    {
        private DbExecutor _executor;
        private TestDbEngine _dbEngine;

        /// <summary>
        /// The database executor.
        /// </summary>
        protected DbExecutor Executor => _executor;

        /// <summary>
        /// The database engine.
        /// </summary>
        protected TestDbEngine DbEngine => _dbEngine;

        /// <summary>
        /// Initializes each test before its test method runs.
        /// </summary>
        [TestInitialize]
        public virtual void TestInitialize()
        {
            _executor = TestExecutorFactory.CreateInMemoryExecutor();
            _dbEngine = new TestDbEngine(_executor);
        }

        /// <summary>
        /// Cleans up after each test method.
        /// </summary>
        [TestCleanup]
        public virtual void TestCleanup()
        {
            if (_executor != null)
            {
                TestExecutorFactory.CleanupTestData(_executor);
                _executor = null;
                _dbEngine = null;
            }
        }

    }
}
