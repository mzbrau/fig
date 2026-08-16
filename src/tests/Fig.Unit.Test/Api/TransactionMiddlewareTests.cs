using Fig.Api;
using Fig.Api.Attributes;
using Fig.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NHibernate;
using NUnit.Framework;
using ISession = NHibernate.ISession;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class TransactionMiddlewareTests
{
    private Mock<IOptionsMonitor<ApiSettings>> _settings = null!;
    private Mock<ISession> _session = null!;
    private Mock<ITransaction> _transaction = null!;
    private bool _nextCalled;

    [SetUp]
    public void SetUp()
    {
        _nextCalled = false;
        _settings = new Mock<IOptionsMonitor<ApiSettings>>();
        _settings.SetupGet(s => s.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "test",
            Secret = "secret",
            DisableTransactionMiddleware = false
        });

        _transaction = new Mock<ITransaction>();
        _transaction.SetupGet(t => t.IsActive).Returns(true);

        _session = new Mock<ISession>();
        _session.Setup(s => s.BeginTransaction()).Returns(_transaction.Object);
    }

    [Test]
    public async Task Invoke_ShouldCommit_WhenRequestSucceeds()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext();

        await middleware.Invoke(context, _session.Object);

        Assert.That(_nextCalled, Is.True);
        _session.Verify(s => s.BeginTransaction(), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Invoke_ShouldRollbackAndRethrow_WhenNextThrows()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("fail"));
        var context = CreateContext();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await middleware.Invoke(context, _session.Object));

        Assert.That(ex!.Message, Is.EqualTo("fail"));
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Invoke_ShouldSkipTransaction_WhenDisabledInSettings()
    {
        _settings.SetupGet(s => s.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "test",
            Secret = "secret",
            DisableTransactionMiddleware = true
        });

        var middleware = CreateMiddleware();
        var context = CreateContext();

        await middleware.Invoke(context, _session.Object);

        Assert.That(_nextCalled, Is.True);
        _session.Verify(s => s.BeginTransaction(), Times.Never);
    }

    [Test]
    public async Task Invoke_ShouldSkipTransaction_WhenEndpointHasSkipTransactionAttribute()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext(skipTransaction: true);

        await middleware.Invoke(context, _session.Object);

        Assert.That(_nextCalled, Is.True);
        _session.Verify(s => s.BeginTransaction(), Times.Never);
    }

    [Test]
    public async Task Invoke_ShouldNotCommitOrRollback_WhenTransactionNoLongerActive()
    {
        _transaction.SetupGet(t => t.IsActive).Returns(false);
        var middleware = CreateMiddleware();
        var context = CreateContext();

        await middleware.Invoke(context, _session.Object);

        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Invoke_ShouldNotRollback_WhenExceptionAndTransactionInactive()
    {
        _transaction.SetupGet(t => t.IsActive).Returns(false);
        var middleware = CreateMiddleware(_ => throw new Exception("boom"));
        var context = CreateContext();

        Assert.ThrowsAsync<Exception>(async () => await middleware.Invoke(context, _session.Object));

        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private TransactionMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        return new TransactionMiddleware(
            next ?? (_ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            }),
            _settings.Object,
            Mock.Of<ILogger<TransactionMiddleware>>());
    }

    private static DefaultHttpContext CreateContext(bool skipTransaction = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/test";

        if (skipTransaction)
        {
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new SkipTransactionAttribute()),
                "test"));
        }

        return context;
    }
}
