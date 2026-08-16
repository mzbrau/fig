using System.IO;
using System.Net;
using System.Text;
using Fig.Api.Exceptions;
using Fig.Api.Middleware;
using Fig.Api.Reports;
using Fig.Common.NetStandard.Exceptions;
using Fig.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ErrorHandlerMiddlewareTests
{
    [TestCaseSource(nameof(StatusMappingCases))]
    public async Task Invoke_ShouldMapKnownExceptionsToExpectedStatusCodes(Exception exception, HttpStatusCode expectedStatus)
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw exception, isDevelopment: false);

        await middleware.Invoke(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo((int)expectedStatus));
        Assert.That(context.Response.ContentType, Is.EqualTo("application/json"));

        var body = await ReadBody(context);
        var result = JsonConvert.DeserializeObject<ErrorResultDataContract>(body);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ErrorType, Is.EqualTo(((int)expectedStatus).ToString()));
        Assert.That(result.Message, Is.EqualTo(exception.Message));
        Assert.That(result.Detail, Is.Null);
        Assert.That(result.Reference, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Invoke_ShouldIncludeExceptionDetail_InDevelopment()
    {
        var exception = new InvalidOperationException("boom");
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw exception, isDevelopment: true);

        await middleware.Invoke(context);

        var body = await ReadBody(context);
        var result = JsonConvert.DeserializeObject<ErrorResultDataContract>(body);

        Assert.That(result!.Detail, Does.Contain("InvalidOperationException"));
        Assert.That(result.Detail, Does.Contain("boom"));
    }

    [Test]
    public async Task Invoke_ShouldNotModifyResponse_WhenResponseHasStarted()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var middleware = CreateMiddleware(_ => throw new Exception("late failure"), isDevelopment: false);

        await middleware.Invoke(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task Invoke_ShouldPassThrough_WhenNoException()
    {
        var nextCalled = false;
        var context = CreateContext();
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            _.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return Task.CompletedTask;
        }, isDevelopment: false);

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.NoContent));
        Assert.That(await ReadBody(context), Is.Empty);
    }

    private static IEnumerable<TestCaseData> StatusMappingCases()
    {
        yield return Case(new UnauthorizedAccessException("denied"), HttpStatusCode.Unauthorized);

        yield return Case(new UserExistsException("bob"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidSettingException("bad setting"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidClientSecretException("secret"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidPasswordException("bad password"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidClientSecretChangeException("cannot change"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidUserDeletionException(), HttpStatusCode.BadRequest);
        yield return Case(new InvalidOperationException("invalid op"), HttpStatusCode.BadRequest);
        yield return Case(new ApplicationException("app"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidImportException("bad import"), HttpStatusCode.BadRequest);
        yield return Case(new InvalidClientNameException("bad name"), HttpStatusCode.BadRequest);
        yield return Case(new ArgumentException("arg"), HttpStatusCode.BadRequest);
        yield return Case(new ReportParameterValidationException("param"), HttpStatusCode.BadRequest);

        yield return Case(new KeyNotFoundException("missing"), HttpStatusCode.NotFound);
        yield return Case(new UnknownUserException(), HttpStatusCode.NotFound);
        yield return Case(new UnknownClientException("client"), HttpStatusCode.NotFound);
        yield return Case(new ChangeNotFoundException("change"), HttpStatusCode.NotFound);
        yield return Case(new ActionExecutionNotFoundException(), HttpStatusCode.NotFound);
        yield return Case(new ReportNotFoundException("report-1"), HttpStatusCode.NotFound);

        yield return Case(new Exception("unexpected"), HttpStatusCode.InternalServerError);
        yield return Case(new NullReferenceException("nre"), HttpStatusCode.InternalServerError);
    }

    private static TestCaseData Case(Exception exception, HttpStatusCode status)
    {
        return new TestCaseData(exception, status).SetName($"{exception.GetType().Name}_{status}");
    }

    private static ErrorHandlerMiddleware CreateMiddleware(RequestDelegate next, bool isDevelopment)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName)
            .Returns(isDevelopment ? Environments.Development : Environments.Production);

        return new ErrorHandlerMiddleware(next, environment.Object, Mock.Of<ILogger<ErrorHandlerMiddleware>>());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
