using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Fig.Api.Datalayer;

/// <summary>
/// Decorates a <see cref="DbCommand"/> so statement execution retries on lock contention.
/// </summary>
public sealed class LockRetryDbCommand : DbCommand
{
    private readonly DbCommand _inner;

    public LockRetryDbCommand(DbCommand inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public DbCommand Inner => _inner;

    [AllowNull]
    public override string CommandText
    {
        get => _inner.CommandText;
        set => _inner.CommandText = value ?? string.Empty;
    }

    public override int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _inner.DesignTimeVisible;
        set => _inner.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _inner.Connection;
        set => _inner.Connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value;
    }

    public override void Cancel() => _inner.Cancel();

    public override void Prepare() => _inner.Prepare();

    public override int ExecuteNonQuery() =>
        LockContentionRetry.Execute(() => _inner.ExecuteNonQuery());

    public override object? ExecuteScalar() =>
        LockContentionRetry.Execute(() => _inner.ExecuteScalar());

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        LockContentionRetry.Execute(() => _inner.ExecuteReader(behavior));

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        LockContentionRetry.ExecuteAsync(
            ct => _inner.ExecuteNonQueryAsync(ct),
            cancellationToken: cancellationToken);

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
        LockContentionRetry.ExecuteAsync(
            ct => _inner.ExecuteScalarAsync(ct),
            cancellationToken: cancellationToken);

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken) =>
        LockContentionRetry.ExecuteAsync(
            ct => _inner.ExecuteReaderAsync(behavior, ct),
            cancellationToken: cancellationToken);

    protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();

        base.Dispose(disposing);
    }
}
