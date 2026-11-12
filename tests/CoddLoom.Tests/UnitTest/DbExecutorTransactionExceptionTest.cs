using CoddLoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CoddLoom.Tests.UnitTest;

/// <summary>
/// Regression tests for issue #77: when a transaction callback throws and the
/// rollback itself also fails, <see cref="DbExecutor.Transaction"/> and
/// <see cref="DbExecutor.TransactionAsync"/> must rethrow the callback's
/// original exception. The rollback failure is retained in the original
/// exception's <c>Data</c> dictionary under the documented key
/// <c>CoddLoom.RollbackException</c> instead of replacing the original
/// exception. The fake connection and transaction construct the rollback
/// failure deterministically, without depending on a real broken connection.
/// </summary>
[TestClass]
public class DbExecutorTransactionExceptionTest
{
    /// <summary>The documented data key for a retained rollback failure.</summary>
    private const string RollbackExceptionDataKey = "CoddLoom.RollbackException";

    [TestMethod]
    public void Transaction_RollbackFailure_ThrowsTheOriginalBusinessException()
    {
        var connection = new FakeConnection();
        var executor = new FakeExecutor(connection);

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
            executor.Transaction(tran =>
            {
                ((FakeTransaction)tran).ThrowOnRollback = true;
                throw new InvalidOperationException("business rule failed");
            }));

        Assert.AreEqual("business rule failed", thrown.Message,
            "The original business exception, not the rollback failure, must propagate.");
        var rollbackFailure = Assert.IsInstanceOfType<ApplicationException>(
            thrown.Data[RollbackExceptionDataKey]);
        Assert.AreEqual("rollback failed", rollbackFailure.Message);
        Assert.AreEqual(1, connection.Transaction.RollbackAttempts,
            "The rollback must still be attempted despite its failure.");
        Assert.AreEqual(1, connection.Closed,
            "The connection must still be closed after the failure.");
    }

    [TestMethod]
    public async Task TransactionAsync_RollbackFailure_ThrowsTheOriginalBusinessException()
    {
        var connection = new FakeConnection();
        var executor = new FakeExecutor(connection);

        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            executor.TransactionAsync(tran =>
            {
                ((FakeTransaction)tran).ThrowOnRollback = true;
                throw new InvalidOperationException("business rule failed");
            }));

        Assert.AreEqual("business rule failed", thrown.Message,
            "The original business exception, not the rollback failure, must propagate.");
        var rollbackFailure = Assert.IsInstanceOfType<ApplicationException>(
            thrown.Data[RollbackExceptionDataKey]);
        Assert.AreEqual("rollback failed", rollbackFailure.Message);
        Assert.AreEqual(1, connection.Transaction.RollbackAttempts,
            "The rollback must still be attempted despite its failure.");
        Assert.AreEqual(1, connection.OpenedAsync,
            "TransactionAsync must open the connection asynchronously.");
        Assert.AreEqual(0, connection.OpenedSync);
        Assert.AreEqual(1, connection.Closed,
            "The connection must still be closed after the failure.");
    }

    [TestMethod]
    public void Transaction_RollbackSuccess_ThrowsTheOriginalExceptionUnchanged()
    {
        var connection = new FakeConnection();
        var executor = new FakeExecutor(connection);
        var original = new InvalidOperationException(
            "business rule failed", new ArgumentException("inner cause"));

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(
            () => executor.Transaction(_ => throw original));

        Assert.AreSame(original, thrown,
            "A successful rollback must leave the original exception instance untouched.");
        Assert.IsInstanceOfType<ArgumentException>(thrown.InnerException);
        Assert.IsFalse(thrown.Data.Contains(RollbackExceptionDataKey),
            "No rollback failure is retained when the rollback succeeds.");
        Assert.AreEqual(1, connection.Transaction.RollbackAttempts);
        Assert.AreEqual(0, connection.Transaction.Commits);
        Assert.AreEqual(1, connection.Closed);
    }

    [TestMethod]
    public async Task TransactionAsync_RollbackSuccess_ThrowsTheOriginalExceptionUnchanged()
    {
        var connection = new FakeConnection();
        var executor = new FakeExecutor(connection);
        var original = new InvalidOperationException(
            "business rule failed", new ArgumentException("inner cause"));

        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => executor.TransactionAsync(_ => throw original));

        Assert.AreSame(original, thrown,
            "A successful rollback must leave the original exception instance untouched.");
        Assert.IsInstanceOfType<ArgumentException>(thrown.InnerException);
        Assert.IsFalse(thrown.Data.Contains(RollbackExceptionDataKey),
            "No rollback failure is retained when the rollback succeeds.");
        Assert.AreEqual(1, connection.Transaction.RollbackAttempts);
        Assert.AreEqual(0, connection.Transaction.Commits);
        Assert.AreEqual(1, connection.Closed);
    }

    private sealed class FakeExecutor : DbExecutor
    {
        private readonly FakeConnection _connection;

        public FakeExecutor(FakeConnection connection)
            : base("fake")
        {
            _connection = connection;
        }

        public override DbConnection GetConnection() => _connection;

        protected override Func<string, object, IDbDataParameter> GetAddParameterFunc(IDbCommand command)
        {
            throw new NotSupportedException("The transaction exception tests execute no commands.");
        }

        protected override IDataAdapter GetAdapter(IDbCommand command)
        {
            throw new NotSupportedException("The transaction exception tests execute no commands.");
        }
    }

    private sealed class FakeConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public FakeTransaction Transaction { get; private set; }

        public int OpenedSync { get; private set; }

        public int OpenedAsync { get; private set; }

        public int Closed { get; private set; }

        public override string ConnectionString { get; set; } = "fake";

        public override string Database => "fake";

        public override string DataSource => "fake";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close()
        {
            _state = ConnectionState.Closed;
            Closed++;
        }

        public override void Open()
        {
            OpenedSync++;
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            OpenedAsync++;
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            Transaction = new FakeTransaction(this, isolationLevel);
            return Transaction;
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotSupportedException("The transaction exception tests execute no commands.");
        }
    }

    private sealed class FakeTransaction(FakeConnection connection, IsolationLevel isolationLevel)
        : DbTransaction
    {
        public int Commits { get; private set; }

        public int RollbackAttempts { get; private set; }

        public bool ThrowOnRollback { get; set; }

        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        public override void Commit()
        {
            Commits++;
        }

        public override void Rollback()
        {
            RollbackAttempts++;
            if (ThrowOnRollback)
            {
                throw new ApplicationException("rollback failed");
            }
        }
    }
}
