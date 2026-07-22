using Fig.Api.Reports;
using Fig.Contracts.Reports;
using Fig.Unit.Test.Api.Reports;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReportExecutionServiceTests
{
    private sealed class EmptyParameters
    {
    }

    [Test]
    public void ExecuteAsync_ThrowsWhenReportMissing()
    {
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("missing")).Returns((IReport?)null);
        var service = CreateService(registry.Object);

        Assert.ThrowsAsync<ReportNotFoundException>(() =>
            service.ExecuteAsync("missing", new ReportExecutionRequestDataContract(new Dictionary<string, object?>())));
    }

    [Test]
    public void ExecuteAsync_ThrowsWhenParametersInvalid()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Throws(new ReportParameterValidationException("bad"));

        var service = CreateService(registry.Object, binder.Object);
        ReportTestFixtures.Authenticate(service);

        Assert.ThrowsAsync<ReportParameterValidationException>(() =>
            service.ExecuteAsync("stub", new ReportExecutionRequestDataContract(new Dictionary<string, object?>())));
    }

    [Test]
    public void ExecuteAsync_ThrowsWhenFormatUnsupported()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Returns(new EmptyParameters());

        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(It.IsAny<ReportFormat>())).Returns(false);

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object]);
        ReportTestFixtures.Authenticate(service);

        Assert.ThrowsAsync<NotSupportedException>(() =>
            service.ExecuteAsync("stub", new ReportExecutionRequestDataContract(new Dictionary<string, object?>())));
    }

    [Test]
    public void ExecuteAsync_RequiresAuthenticatedUser()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Returns(new EmptyParameters());

        report.Setup(r => r.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(ReportFormat.Html)).Returns(true);
        renderer.Setup(r => r.RenderAsync(It.IsAny<ReportRenderContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html/>");

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object]);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ExecuteAsync("stub", new ReportExecutionRequestDataContract(new Dictionary<string, object?>())));
    }

    [Test]
    public async Task ExecuteAsync_RendersHtmlWithGeneratedBy()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Returns(new EmptyParameters());

        report.Setup(r => r.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new { Ok = true });

        ReportRenderContext? captured = null;
        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(ReportFormat.Html)).Returns(true);
        renderer.Setup(r => r.RenderAsync(It.IsAny<ReportRenderContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReportRenderContext, CancellationToken>((ctx, _) => captured = ctx)
            .ReturnsAsync("<html>ok</html>");

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object]);
        ReportTestFixtures.Authenticate(service, ReportTestFixtures.CreateAdminUser(username: "report-runner"));

        var (html, contentType) = await service.ExecuteAsync(
            "stub",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>()));

        Assert.That(html, Is.EqualTo("<html>ok</html>"));
        Assert.That(contentType, Does.Contain("text/html"));
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.GeneratedBy, Is.EqualTo("report-runner"));
        Assert.That(captured.Title, Is.EqualTo("Stub Report"));
    }

    private static ReportExecutionService CreateService(
        IReportRegistry registry,
        IReportParameterBinder? binder = null)
    {
        binder ??= Mock.Of<IReportParameterBinder>();
        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(ReportFormat.Html)).Returns(true);
        renderer.Setup(r => r.RenderAsync(It.IsAny<ReportRenderContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html/>");
        return new ReportExecutionService(registry, binder, [renderer.Object]);
    }

    private static Mock<IReport> CreateStubReport()
    {
        var report = new Mock<IReport>();
        report.SetupGet(r => r.Id).Returns("stub");
        report.SetupGet(r => r.Name).Returns("Stub Report");
        report.SetupGet(r => r.Description).Returns("desc");
        report.SetupGet(r => r.ParametersType).Returns(typeof(EmptyParameters));
        report.SetupGet(r => r.BodyComponentType).Returns(typeof(object));
        report.SetupGet(r => r.PageOrientation).Returns(ReportPageOrientation.Portrait);
        report.Setup(r => r.GetParameterDefinitions()).Returns([]);
        return report;
    }
}
