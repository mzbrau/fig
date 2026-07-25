using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Api.Services;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;
using Fig.Unit.Test.Api.Reports;
using Microsoft.Extensions.Logging;
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

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object],
            Mock.Of<IReportAiAnalysisService>(),
            Mock.Of<IConfigurationRepository>(),
            Mock.Of<IEncryptionService>(),
            Mock.Of<ILogger<ReportExecutionService>>());
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

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object],
            Mock.Of<IReportAiAnalysisService>(),
            Mock.Of<IConfigurationRepository>(),
            Mock.Of<IEncryptionService>(),
            Mock.Of<ILogger<ReportExecutionService>>());

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

        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity());

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object],
            Mock.Of<IReportAiAnalysisService>(),
            configRepo.Object,
            Mock.Of<IEncryptionService>(),
            Mock.Of<ILogger<ReportExecutionService>>());
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

    [Test]
    public async Task ExecuteAsync_IncludesAiAnalysisWhenRequested()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Returns(new EmptyParameters());

        report.Setup(r => r.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new { Ok = true });

        var analysis = new Mock<IReportAiAnalysisService>();
        analysis.Setup(a => a.AnalyzeAsync("Stub Report", It.IsAny<object>(), "focus on trends", It.IsAny<CancellationToken>()))
            .ReturnsAsync("## Trends\nLooks stable.");

        ReportRenderContext? captured = null;
        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(ReportFormat.Html)).Returns(true);
        renderer.Setup(r => r.RenderAsync(It.IsAny<ReportRenderContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReportRenderContext, CancellationToken>((ctx, _) => captured = ctx)
            .ReturnsAsync("<html/>");

        var config = new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = true,
            FigAssistantEndpoint = "https://llm.example/v1",
            FigAssistantModel = "gpt",
            FigAssistantAccessTokenEncrypted = "enc-token"
        };
        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(config);
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt("enc-token", It.IsAny<bool>(), It.IsAny<bool>())).Returns("plain-token");

        var service = new ReportExecutionService(
            registry.Object, binder.Object, [renderer.Object], analysis.Object,
            configRepo.Object, encryption.Object, Mock.Of<ILogger<ReportExecutionService>>());
        ReportTestFixtures.Authenticate(service);

        await service.ExecuteAsync("stub", new ReportExecutionRequestDataContract(
            new Dictionary<string, object?>(),
            enableAiAnalysis: true,
            aiPrompt: "focus on trends"));

        Assert.That(captured!.AiAnalysisMarkdown, Is.EqualTo("## Trends\nLooks stable."));
        Assert.That(captured.ParameterSummary["AI Analysis"], Is.EqualTo("Yes"));
    }

    [Test]
    public async Task ExecuteAsync_OmitsAiAnalysisWhenServiceReturnsNull()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Returns(new EmptyParameters());

        report.Setup(r => r.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new { Ok = true });

        var analysis = new Mock<IReportAiAnalysisService>();
        analysis.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        ReportRenderContext? captured = null;
        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(ReportFormat.Html)).Returns(true);
        renderer.Setup(r => r.RenderAsync(It.IsAny<ReportRenderContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReportRenderContext, CancellationToken>((ctx, _) => captured = ctx)
            .ReturnsAsync("<html/>");

        var config = new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = true,
            FigAssistantEndpoint = "https://llm.example/v1",
            FigAssistantModel = "gpt",
            FigAssistantAccessTokenEncrypted = "enc-token"
        };
        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(config);
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt("enc-token", It.IsAny<bool>(), It.IsAny<bool>())).Returns("plain-token");

        var service = new ReportExecutionService(
            registry.Object, binder.Object, [renderer.Object], analysis.Object,
            configRepo.Object, encryption.Object, Mock.Of<ILogger<ReportExecutionService>>());
        ReportTestFixtures.Authenticate(service);

        await service.ExecuteAsync("stub", new ReportExecutionRequestDataContract(
            new Dictionary<string, object?>(),
            enableAiAnalysis: true));

        Assert.That(captured!.AiAnalysisMarkdown, Is.Null);
        Assert.That(captured.ParameterSummary["AI Analysis"], Is.EqualTo("Requested (omitted)"));
    }

    [Test]
    public async Task ExecuteAsync_UsesDynamicTitleFromDocumentMetadata()
    {
        var report = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.Get("stub")).Returns(report.Object);

        var binder = new Mock<IReportParameterBinder>();
        binder.Setup(b => b.Bind(typeof(EmptyParameters), It.IsAny<IDictionary<string, object?>>()))
            .Returns(new EmptyParameters());

        report.Setup(r => r.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiReportDocument
            {
                Title = "Dynamic Title",
                Description = "From AI",
                Sections = []
            });

        ReportRenderContext? captured = null;
        var renderer = new Mock<IReportRenderer>();
        renderer.Setup(r => r.CanRender(ReportFormat.Html)).Returns(true);
        renderer.Setup(r => r.RenderAsync(It.IsAny<ReportRenderContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReportRenderContext, CancellationToken>((ctx, _) => captured = ctx)
            .ReturnsAsync("<html/>");

        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity());

        var service = new ReportExecutionService(registry.Object, binder.Object, [renderer.Object],
            Mock.Of<IReportAiAnalysisService>(),
            configRepo.Object,
            Mock.Of<IEncryptionService>(),
            Mock.Of<ILogger<ReportExecutionService>>());
        ReportTestFixtures.Authenticate(service);

        await service.ExecuteAsync("stub", new ReportExecutionRequestDataContract(new Dictionary<string, object?>()));

        Assert.That(captured!.Title, Is.EqualTo("Dynamic Title"));
        Assert.That(captured.Description, Is.EqualTo("From AI"));
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
        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity());
        return new ReportExecutionService(
            registry,
            binder,
            [renderer.Object],
            Mock.Of<IReportAiAnalysisService>(),
            configRepo.Object,
            Mock.Of<IEncryptionService>(),
            Mock.Of<ILogger<ReportExecutionService>>());
    }

    [Test]
    public async Task GetAvailableReports_HidesAiReportWhenAssistantDisabled()
    {
        var aiReport = new Mock<IReport>();
        aiReport.SetupGet(r => r.Id).Returns(AiComposedReport.ReportId);
        aiReport.SetupGet(r => r.Name).Returns("AI Report");
        aiReport.SetupGet(r => r.Category).Returns("AI");
        aiReport.SetupGet(r => r.Description).Returns("desc");
        aiReport.Setup(r => r.GetParameterDefinitions()).Returns([]);

        var other = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.GetAll()).Returns([other.Object, aiReport.Object]);

        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = false
        });

        var service = new ReportExecutionService(
            registry.Object,
            Mock.Of<IReportParameterBinder>(),
            Array.Empty<IReportRenderer>(),
            Mock.Of<IReportAiAnalysisService>(),
            configRepo.Object,
            Mock.Of<IEncryptionService>(),
            Mock.Of<ILogger<ReportExecutionService>>());

        var reports = await service.GetAvailableReports();
        Assert.That(reports.Select(r => r.Id), Does.Not.Contain(AiComposedReport.ReportId));
        Assert.That(reports.Single().SupportsAiAnalysis, Is.False);
    }

    [Test]
    public async Task GetAvailableReports_IncludesAiReportWhenAssistantReady()
    {
        var aiReport = new Mock<IReport>();
        aiReport.SetupGet(r => r.Id).Returns(AiComposedReport.ReportId);
        aiReport.SetupGet(r => r.Name).Returns("AI Report");
        aiReport.SetupGet(r => r.Category).Returns("AI");
        aiReport.SetupGet(r => r.Description).Returns("desc");
        aiReport.Setup(r => r.GetParameterDefinitions()).Returns([]);

        var other = CreateStubReport();
        var registry = new Mock<IReportRegistry>();
        registry.Setup(r => r.GetAll()).Returns([other.Object, aiReport.Object]);

        var config = new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = true,
            FigAssistantEndpoint = "https://llm.example/v1",
            FigAssistantModel = "gpt",
            FigAssistantAccessTokenEncrypted = "enc-token"
        };
        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(c => c.GetConfiguration()).ReturnsAsync(config);
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt("enc-token", It.IsAny<bool>(), It.IsAny<bool>())).Returns("plain-token");

        var service = new ReportExecutionService(
            registry.Object,
            Mock.Of<IReportParameterBinder>(),
            Array.Empty<IReportRenderer>(),
            Mock.Of<IReportAiAnalysisService>(),
            configRepo.Object,
            encryption.Object,
            Mock.Of<ILogger<ReportExecutionService>>());

        var reports = await service.GetAvailableReports();
        Assert.That(reports.Select(r => r.Id), Does.Contain(AiComposedReport.ReportId));
        Assert.That(reports.Single(r => r.Id == "stub").SupportsAiAnalysis, Is.True);
        Assert.That(reports.Single(r => r.Id == AiComposedReport.ReportId).SupportsAiAnalysis, Is.False);
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
