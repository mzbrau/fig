using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using Fig.Api.Datalayer;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class LockContentionRetryTests
{
    [Test]
    public void Execute_ReturnsResult_WhenActionSucceeds()
    {
        var result = LockContentionRetry.Execute(() => 42, maxAttempts: 3, baseDelayMs: 1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Execute_RetriesOnLockContention_ThenSucceeds()
    {
        var attempts = 0;

        var result = LockContentionRetry.Execute(() =>
        {
            attempts++;
            if (attempts < 3)
                throw new SQLiteException(SQLiteErrorCode.Busy, "database is locked");
            return "ok";
        }, maxAttempts: 3, baseDelayMs: 1);

        Assert.That(result, Is.EqualTo("ok"));
        Assert.That(attempts, Is.EqualTo(3));
    }

    [Test]
    public void Execute_DoesNotRetry_OnNonLockException()
    {
        var attempts = 0;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LockContentionRetry.Execute<object>(() =>
            {
                attempts++;
                throw new InvalidOperationException("boom");
            }, maxAttempts: 3, baseDelayMs: 1));

        Assert.That(ex!.Message, Is.EqualTo("boom"));
        Assert.That(attempts, Is.EqualTo(1));
    }

    [Test]
    public void Execute_ThrowsOriginal_WhenAttemptsExhausted()
    {
        var attempts = 0;
        var retryCallbacks = 0;

        var ex = Assert.Throws<SQLiteException>(() =>
            LockContentionRetry.Execute(() =>
            {
                attempts++;
                throw new SQLiteException(SQLiteErrorCode.Busy, "database is locked");
            }, maxAttempts: 3, baseDelayMs: 1, onRetry: (_, _) => retryCallbacks++));

        Assert.That(ex!.ErrorCode, Is.EqualTo((int)SQLiteErrorCode.Busy));
        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(retryCallbacks, Is.EqualTo(2));
    }

    [Test]
    public async Task ExecuteAsync_RetriesOnLockContention_ThenSucceeds()
    {
        var attempts = 0;

        var result = await LockContentionRetry.ExecuteAsync(async _ =>
        {
            attempts++;
            await Task.Yield();
            if (attempts < 2)
                throw new SQLiteException(SQLiteErrorCode.Locked, "table is locked");
            return 7;
        }, maxAttempts: 3, baseDelayMs: 1);

        Assert.That(result, Is.EqualTo(7));
        Assert.That(attempts, Is.EqualTo(2));
    }

    [Test]
    public void Execute_ThrowsArgumentOutOfRange_WhenMaxAttemptsInvalid()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LockContentionRetry.Execute(() => 1, maxAttempts: 0, baseDelayMs: 1));

        Assert.That(ex!.ParamName, Is.EqualTo("maxAttempts"));
    }

    [Test]
    public void Execute_ThrowsArgumentOutOfRange_WhenBaseDelayMsNegative()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LockContentionRetry.Execute(() => 1, maxAttempts: 3, baseDelayMs: -1));

        Assert.That(ex!.ParamName, Is.EqualTo("baseDelayMs"));
    }

    [Test]
    public void LockRetryDbCommand_ExecuteNonQuery_RetriesOnBusy()
    {
        var attempts = 0;
        using var command = new LockRetryDbCommand(new FakeDbCommand(() =>
        {
            attempts++;
            if (attempts < 2)
                throw new SQLiteException(SQLiteErrorCode.Busy, "database is locked");
            return 1;
        }));

        var result = command.ExecuteNonQuery();

        Assert.That(result, Is.EqualTo(1));
        Assert.That(attempts, Is.EqualTo(2));
    }

    [Test]
    public void RetryingSQLiteDriver_CreateCommand_WrapsInnerCommand()
    {
        var driver = new RetryingSQLiteDriver();
        using var command = driver.CreateCommand();

        Assert.That(command, Is.TypeOf<LockRetryDbCommand>());
        Assert.That(driver.UnwrapDbCommand(command), Is.Not.TypeOf<LockRetryDbCommand>());
    }

    private sealed class FakeDbCommand : System.Data.Common.DbCommand
    {
        private readonly Func<int> _executeNonQuery;

        public FakeDbCommand(Func<int> executeNonQuery)
        {
            _executeNonQuery = executeNonQuery;
        }

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override System.Data.CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override System.Data.UpdateRowSource UpdatedRowSource { get; set; }
        protected override System.Data.Common.DbConnection? DbConnection { get; set; }
        protected override System.Data.Common.DbParameterCollection DbParameterCollection { get; } =
            new FakeParameterCollection();
        protected override System.Data.Common.DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => _executeNonQuery();

        public override object? ExecuteScalar() => throw new NotSupportedException();

        public override void Prepare()
        {
        }

        protected override System.Data.Common.DbParameter CreateDbParameter() =>
            throw new NotSupportedException();

        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) =>
            throw new NotSupportedException();
    }

    private sealed class FakeParameterCollection : System.Data.Common.DbParameterCollection
    {
        public override int Count => 0;
        public override object SyncRoot { get; } = new();

        public override int Add(object value) => throw new NotSupportedException();
        public override void AddRange(Array values) => throw new NotSupportedException();
        public override void Clear()
        {
        }

        public override bool Contains(object value) => false;
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) => throw new NotSupportedException();
        public override System.Collections.IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
        public override int IndexOf(object value) => -1;
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => throw new NotSupportedException();
        public override void Remove(object value) => throw new NotSupportedException();
        public override void RemoveAt(int index) => throw new NotSupportedException();
        public override void RemoveAt(string parameterName) => throw new NotSupportedException();
        protected override System.Data.Common.DbParameter GetParameter(int index) =>
            throw new NotSupportedException();
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) =>
            throw new NotSupportedException();
        protected override void SetParameter(int index, System.Data.Common.DbParameter value) =>
            throw new NotSupportedException();
        protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) =>
            throw new NotSupportedException();
    }
}
