using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoddLoom;
using System;
using CoddLoom.Tests.DbCode;

namespace CoddLoom.Tests.DbTest
{
    /// <summary>
    /// 测试基类，提供统一的数据库执行器管理和清理
    /// </summary>
    [TestClass]
    public abstract class TestBase
    {
        private DbExecutor _executor;
        private TestDbEngine _dbEngine;

        /// <summary>
        /// 数据库执行器
        /// </summary>
        protected DbExecutor Executor => _executor;

        /// <summary>
        /// 数据库引擎
        /// </summary>
        protected TestDbEngine DbEngine => _dbEngine;

        /// <summary>
        /// 测试初始化，在每个测试方法执行前调用
        /// </summary>
        [TestInitialize]
        public virtual void TestInitialize()
        {
            _executor = TestExecutorFactory.CreateInMemoryExecutor();
            _dbEngine = new TestDbEngine(_executor);
        }

        /// <summary>
        /// 测试清理，在每个测试方法执行后调用
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

        /// <summary>
        /// 执行测试操作，自动处理异常和清理
        /// </summary>
        /// <param name="testAction">测试操作</param>
        /// <param name="cleanupAction">可选的额外清理操作</param>
        protected void ExecuteTest(Action<TestDbEngine> testAction, Action<TestDbEngine> cleanupAction = null)
        {
            try
            {
                testAction?.Invoke(_dbEngine);
            }
            finally
            {
                cleanupAction?.Invoke(_dbEngine);
            }
        }
    }
}