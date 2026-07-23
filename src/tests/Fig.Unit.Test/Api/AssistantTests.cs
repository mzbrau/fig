using System.Diagnostics;
using System.Net.Http;
using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Observability;
using Fig.Api.Reports;
using Fig.Api.Services;
using Fig.Common;
using Fig.Contracts.Assistant;
using Fig.Contracts.Authentication;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Services.Assistant;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Radzen;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class AssistantHistoryCompactorTests
{
    [Test]
    public void Compact_WhenUnderLimit_ReturnsOriginalMessages()
    {
        var compactor = new AssistantHistoryCompactor();
        var messages = new List<JObject>
        {
            new() { ["role"] = "system", ["content"] = "You are Fig Assistant" },
            new() { ["role"] = "user", ["content"] = "Hello" }
        };

        var result = compactor.Compact(messages);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0]["content"]?.Value<string>(), Is.EqualTo("You are Fig Assistant"));
    }

    [Test]
    public void Compact_WhenOverLimit_KeepsSystemAndRecentMessages()
    {
        var compactor = new AssistantHistoryCompactor();
        var messages = new List<JObject>
        {
            new() { ["role"] = "system", ["content"] = "system" }
        };

        for (var i = 0; i < 200; i++)
        {
            messages.Add(new JObject
            {
                ["role"] = i % 2 == 0 ? "user" : "assistant",
                ["content"] = new string('x', 400)
            });
        }

        var result = compactor.Compact(messages);

        Assert.That(result.Count, Is.LessThan(messages.Count));
        Assert.That(result[0]["role"]?.Value<string>(), Is.EqualTo("system"));
        Assert.That(result.Any(a =>
            a["content"]?.Value<string>()?.Contains("omitted", StringComparison.OrdinalIgnoreCase) == true));
    }
}

[TestFixture]
public class AssistantProposedActionParsingTests
{
    [Test]
    public void ProposedActionTypes_MatchContractConstants()
    {
        Assert.That(AssistantProposedActionTypes.UpdateSetting, Is.EqualTo("updateSetting"));
        Assert.That(AssistantProposedActionTypes.CreateGroup, Is.EqualTo("createGroup"));
        Assert.That(AssistantProposedActionTypes.CreateLookupTable, Is.EqualTo("createLookupTable"));
        Assert.That(AssistantProposedActionTypes.CreateInstance, Is.EqualTo("createInstance"));
        Assert.That(AssistantProposedActionTypes.SearchSettings, Is.EqualTo("searchSettings"));
        Assert.That(AssistantProposedActionTypes.HighlightSetting, Is.EqualTo("highlightSetting"));
        Assert.That(AssistantProposedActionTypes.GenerateReport, Is.EqualTo("generateReport"));
    }
}

[TestFixture]
public class AssistantActionApplierTests
{
    private static AssistantActionApplier CreateApplier(
        Mock<ISettingClientFacade> settings,
        Mock<IGroupsFacade> groups,
        Mock<ILookupTablesFacade> lookups,
        IAssistantUiActionQueue? queue = null,
        Mock<INotificationFactory>? notificationFactory = null,
        Mock<IReportsFacade>? reports = null,
        Mock<IJSRuntime>? jsRuntime = null,
        TestNavigationManager? navigation = null)
    {
        notificationFactory ??= new Mock<INotificationFactory>();
        notificationFactory
            .Setup(a => a.Success(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new NotificationMessage());
        notificationFactory
            .Setup(a => a.Failure(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new NotificationMessage());

        return new AssistantActionApplier(
            settings.Object,
            groups.Object,
            lookups.Object,
            queue ?? new AssistantUiActionQueue(),
            navigation ?? new TestNavigationManager(),
            reports?.Object ?? Mock.Of<IReportsFacade>(),
            jsRuntime?.Object ?? Mock.Of<IJSRuntime>(),
            new NotificationService(),
            notificationFactory.Object);
    }

    [Test]
    public async Task ApplyAsync_CreateGroup_AddsDraftWithoutHttp()
    {
        var groups = new Mock<IGroupsFacade>();
        groups.Setup(a => a.AddDraftGroup("Ops", "desc", null))
            .Returns(new Fig.Contracts.SettingGroups.SettingGroupDataContract(
                null, "Ops", "desc", new List<Fig.Contracts.SettingGroups.GroupedSettingDataContract>()));

        var settings = new Mock<ISettingClientFacade>();
        var lookups = new Mock<ILookupTablesFacade>();
        var navigation = new TestNavigationManager();

        var applier = CreateApplier(settings, groups, lookups, navigation: navigation);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.CreateGroup,
                GroupName = "Ops",
                Description = "desc"
            }
        ]);

        groups.Verify(a => a.AddDraftGroup("Ops", "desc", null), Times.Once);
        groups.Verify(a => a.CreateGroup(It.IsAny<Fig.Contracts.SettingGroups.SettingGroupDataContract>()), Times.Never);
        Assert.That(navigation.Navigations, Does.Contain("/groups"));
    }

    [Test]
    public async Task ApplyAsync_CreateLookupTable_AddsDraft()
    {
        var groups = new Mock<IGroupsFacade>();
        var settings = new Mock<ISettingClientFacade>();
        var lookups = new Mock<ILookupTablesFacade>();
        lookups.Setup(a => a.CreateDraft("Regions", "1,AU"))
            .Returns(new Fig.Web.Models.LookupTables.LookupTable("Regions", "1,AU"));
        var navigation = new TestNavigationManager();

        var applier = CreateApplier(settings, groups, lookups, navigation: navigation);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.CreateLookupTable,
                LookupTableName = "Regions",
                Data = "1,AU"
            }
        ]);

        lookups.Verify(a => a.CreateDraft("Regions", "1,AU"), Times.Once);
        Assert.That(navigation.Navigations, Does.Contain("/lookuptables"));
    }

    [Test]
    public async Task ApplyAsync_CreateInstance_CallsFacade()
    {
        var settings = new Mock<ISettingClientFacade>();
        settings.Setup(a => a.CreatePendingInstance("AspNetApi", "prod"))
            .ReturnsAsync(new SettingClientConfigurationModel(
                "AspNetApi", "desc", "prod", false, Mock.Of<Fig.Common.NetStandard.Scripting.IScriptRunner>()));
        var groups = new Mock<IGroupsFacade>();
        var lookups = new Mock<ILookupTablesFacade>();

        var applier = CreateApplier(settings, groups, lookups);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.CreateInstance,
                ClientName = "AspNetApi",
                Instance = "prod"
            }
        ]);

        settings.Verify(a => a.CreatePendingInstance("AspNetApi", "prod"), Times.Once);
    }

    [Test]
    public async Task ApplyAsync_SearchSettings_EnqueuesSearch()
    {
        var queue = new AssistantUiActionQueue();
        var applier = CreateApplier(
            new Mock<ISettingClientFacade>(),
            new Mock<IGroupsFacade>(),
            new Mock<ILookupTablesFacade>(),
            queue);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.SearchSettings,
                SearchQuery = "client:AspNetApi setting:Items"
            }
        ]);

        var actions = queue.DequeueAll();
        Assert.That(actions, Has.Count.EqualTo(1));
        Assert.That(actions[0].Kind, Is.EqualTo(AssistantUiActionKind.Search));
        Assert.That(actions[0].SearchQuery, Is.EqualTo("client:AspNetApi setting:Items"));
    }

    [Test]
    public async Task ApplyAsync_HighlightSetting_EnqueuesHighlight()
    {
        var queue = new AssistantUiActionQueue();
        var applier = CreateApplier(
            new Mock<ISettingClientFacade>(),
            new Mock<IGroupsFacade>(),
            new Mock<ILookupTablesFacade>(),
            queue);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.HighlightSetting,
                ClientName = "AspNetApi",
                SettingName = "Items",
                Instance = "prod"
            }
        ]);

        var actions = queue.DequeueAll();
        Assert.That(actions, Has.Count.EqualTo(1));
        Assert.That(actions[0].Kind, Is.EqualTo(AssistantUiActionKind.Highlight));
        Assert.That(actions[0].ClientName, Is.EqualTo("AspNetApi"));
        Assert.That(actions[0].SettingName, Is.EqualTo("Items"));
        Assert.That(actions[0].Instance, Is.EqualTo("prod"));
    }

    [Test]
    public async Task ApplyAsync_UpdateSetting_AlsoEnqueuesHighlight()
    {
        var settings = new Mock<ISettingClientFacade>();
        var queue = new AssistantUiActionQueue();
        var applier = CreateApplier(
            settings,
            new Mock<IGroupsFacade>(),
            new Mock<ILookupTablesFacade>(),
            queue);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.UpdateSetting,
                ClientName = "AspNetApi",
                SettingName = "Items",
                Value = "[\"a\"]"
            }
        ]);

        settings.Verify(a => a.ApplyPendingValueFromCompare("AspNetApi", null, "Items", "[\"a\"]"), Times.Once);
        var actions = queue.DequeueAll();
        Assert.That(actions, Has.Count.EqualTo(1));
        Assert.That(actions[0].Kind, Is.EqualTo(AssistantUiActionKind.Highlight));
        Assert.That(actions[0].SettingName, Is.EqualTo("Items"));
    }

    [Test]
    public async Task ApplyAsync_GenerateReport_OpensHtmlInNewTab()
    {
        var reports = new Mock<IReportsFacade>();
        reports.Setup(a => a.GenerateReport(
                "client-uptime",
                It.Is<Dictionary<string, object?>>(p =>
                    p.ContainsKey("ClientName") && Equals(p["ClientName"], "AspNetApi"))))
            .ReturnsAsync("<html>ok</html>");

        var js = new Mock<IJSRuntime>();
        js.Setup(a => a.InvokeAsync<bool>(
                "openHtmlInNewTab",
                It.Is<object[]>(args => args.Length == 1 && Equals(args[0], "<html>ok</html>"))))
            .ReturnsAsync(true);

        var notifications = new Mock<INotificationFactory>();
        notifications
            .Setup(a => a.Success(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new NotificationMessage());
        notifications
            .Setup(a => a.Failure(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new NotificationMessage());

        var applier = CreateApplier(
            new Mock<ISettingClientFacade>(),
            new Mock<IGroupsFacade>(),
            new Mock<ILookupTablesFacade>(),
            notificationFactory: notifications,
            reports: reports,
            jsRuntime: js);

        await applier.ApplyAsync([
            new AssistantProposedActionDataContract
            {
                Type = AssistantProposedActionTypes.GenerateReport,
                ReportId = "client-uptime",
                Parameters = new Dictionary<string, object?>
                {
                    ["ClientName"] = "AspNetApi"
                }
            }
        ]);

        reports.Verify(a => a.GenerateReport(
            "client-uptime",
            It.IsAny<Dictionary<string, object?>>()), Times.Once);
        js.Verify(a => a.InvokeAsync<bool>(
            "openHtmlInNewTab",
            It.Is<object[]>(args => args.Length == 1 && Equals(args[0], "<html>ok</html>"))), Times.Once);
        notifications.Verify(a => a.Success("Report Generated", It.IsAny<string>()), Times.Once);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string uri = "http://localhost/")
        {
            Initialize("http://localhost/", uri);
        }

        public List<string> Navigations { get; } = new();

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Navigations.Add(uri);
        }
    }
}

[TestFixture]
public class AssistantReportToolTests
{
    [Test]
    public async Task ListReports_ReturnsCatalogue()
    {
        var reportExecution = new Mock<IReportExecutionService>();
        reportExecution.Setup(a => a.GetAvailableReports()).Returns([
            new ReportDefinitionDataContract(
                "client-uptime",
                "Client Uptime",
                "Clients",
                "Uptime for a client",
                new List<ReportParameterDataContract>())
        ]);

        var registry = CreateRegistry(reportExecution);
        Assert.That(registry.TryGet("list_reports", out var tool), Is.True);
        var result = await tool!.ExecuteAsync("{}", CancellationToken.None);
        Assert.That(result, Does.Contain("client-uptime"));
        Assert.That(result, Does.Contain("Client Uptime"));
    }

    [Test]
    public async Task ProposeWebActions_GenerateReport_RequiresReportId()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"generateReport"}]}""", CancellationToken.None),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProposeWebActions_CreateGroup_RequiresGroupName()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"createGroup"}]}""", CancellationToken.None),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProposeWebActions_CreateLookupTable_RequiresLookupTableName()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"createLookupTable"}]}""", CancellationToken.None),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProposeWebActions_GenerateReport_AcceptsValidAction()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        var result = await tool!.ExecuteAsync(
            """{"actions":[{"type":"generateReport","reportId":"client-uptime","parameters":{"ClientName":"AspNetApi"}}]}""",
            CancellationToken.None);
        Assert.That(result, Does.Contain("generateReport"));
        Assert.That(result, Does.Contain("client-uptime"));
    }

    [Test]
    public void GetApiStatus_Description_MentionsRunningApiInstances()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("get_api_status", out var tool), Is.True);
        Assert.That(tool!.Description, Does.Contain("running Fig.Api"));
    }

    private static AssistantToolRegistry CreateRegistry(Mock<IReportExecutionService> reportExecution)
    {
        return new AssistantToolRegistry(
            Mock.Of<ISettingsService>(),
            Mock.Of<IEventsService>(),
            Mock.Of<IStatusService>(),
            Mock.Of<ILookupTablesService>(),
            Mock.Of<ISettingGroupService>(),
            Mock.Of<IWebHookService>(),
            Mock.Of<ISchedulingService>(),
            Mock.Of<ITimeMachineService>(),
            Mock.Of<ICustomActionService>(),
            Mock.Of<IApiStatusService>(),
            reportExecution.Object,
            Mock.Of<IVersionHelper>(),
            Mock.Of<IHttpClientFactory>());
    }
}

[TestFixture]
public class AssistantBase64StrippingTests
{
    [Test]
    public void StripBase64FromString_RemovesMarkdownAndRawDataUris()
    {
        const string description =
            "Logo ![logo](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB) " +
            "and inline data:image/svg+xml;base64,PHN2ZyB4bWxucz0= end.";

        var stripped = AssistantToolRegistry.StripBase64FromString(description);

        Assert.That(stripped, Does.Not.Contain("base64,"));
        Assert.That(stripped, Does.Contain("[image omitted]"));
        Assert.That(stripped, Does.Contain("Logo"));
        Assert.That(stripped, Does.Contain("and inline"));
        Assert.That(stripped, Does.Contain("end."));
    }

    [Test]
    public void StripBase64FromString_LeavesNormalTextUnchanged()
    {
        const string description = "A normal setting description without images.";
        Assert.That(AssistantToolRegistry.StripBase64FromString(description), Is.EqualTo(description));
    }
}

[TestFixture]
public class AssistantChatTracingTests
{
    [Test]
    public async Task ChatAsync_EmitsChatAndLlmActivities_WithFullPromptOnLlmRequest()
    {
        var started = new List<string>();
        var llmRequestPayloads = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApiActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started.Add(activity.OperationName),
            ActivityStopped = activity =>
            {
                if (activity.OperationName != "Assistant.Llm")
                    return;

                foreach (var activityEvent in activity.Events)
                {
                    if (activityEvent.Name != "llm.request" && !activityEvent.Name.StartsWith("llm.request.part."))
                        continue;

                    foreach (var tag in activityEvent.Tags)
                    {
                        if (tag.Key == "fig.assistant.messages" && tag.Value is string messages)
                            llmRequestPayloads.Add(messages);
                    }
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamText("Hello from the assistant"));

        var tool = new Mock<IAssistantTool>();
        tool.SetupGet(a => a.Name).Returns("list_clients");
        tool.SetupGet(a => a.Description).Returns("List clients");
        tool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");

        var registry = new Mock<IAssistantToolRegistry>();
        registry.SetupGet(a => a.Tools).Returns([tool.Object]);
        registry.Setup(a => a.TryGet(It.IsAny<string>(), out It.Ref<IAssistantTool?>.IsAny)).Returns(false);

        var configuration = new Mock<IConfigurationRepository>();
        configuration.Setup(a => a.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            FigAssistantModel = "test-model",
            FigAssistantMaxToolIterations = 4,
            FigAssistantRequestTimeoutSeconds = 30
        });

        var service = new AssistantChatService(
            llm.Object,
            registry.Object,
            new AssistantHistoryCompactor(),
            configuration.Object);
        service.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false));

        var request = new AssistantChatRequestDataContract
        {
            Messages =
            [
                new AssistantChatMessageDataContract { Role = "user", Content = "What clients exist?" }
            ],
            UiContext = new AssistantUiContextDataContract { CurrentPage = "Settings", Username = "admin" }
        };

        await foreach (var _ in service.ChatAsync(request, CancellationToken.None))
        {
        }

        Assert.That(started, Does.Contain("Assistant.Chat"));
        Assert.That(started, Does.Contain("Assistant.Llm"));
        Assert.That(llmRequestPayloads, Is.Not.Empty);
        Assert.That(llmRequestPayloads[0], Does.Contain("You are Fig Assistant"));
        Assert.That(llmRequestPayloads[0], Does.Contain("What clients exist?"));
        Assert.That(llmRequestPayloads[0], Does.Contain("exact matched setting name"));
    }

    [Test]
    public async Task ChatAsync_WhenToolCalled_EmitsToolActivity()
    {
        var started = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApiActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started.Add(activity.OperationName)
        };
        ActivitySource.AddActivityListener(listener);

        var callCount = 0;
        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1
                    ? StreamToolCall("call_1", "list_clients", "{}")
                    : StreamText("There is one client.");
            });

        var tool = new Mock<IAssistantTool>();
        tool.SetupGet(a => a.Name).Returns("list_clients");
        tool.SetupGet(a => a.Description).Returns("List clients");
        tool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");
        tool.Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"clients":["Demo"]}""");

        var registry = new Mock<IAssistantToolRegistry>();
        registry.SetupGet(a => a.Tools).Returns([tool.Object]);
        IAssistantTool? resolved = tool.Object;
        registry.Setup(a => a.TryGet("list_clients", out resolved)).Returns(true);

        var configuration = new Mock<IConfigurationRepository>();
        configuration.Setup(a => a.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            FigAssistantModel = "test-model",
            FigAssistantMaxToolIterations = 4,
            FigAssistantRequestTimeoutSeconds = 30
        });

        var service = new AssistantChatService(
            llm.Object,
            registry.Object,
            new AssistantHistoryCompactor(),
            configuration.Object);
        service.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false));

        await foreach (var _ in service.ChatAsync(
                           new AssistantChatRequestDataContract
                           {
                               Messages =
                               [
                                   new AssistantChatMessageDataContract { Role = "user", Content = "List clients" }
                               ]
                           },
                           CancellationToken.None))
        {
        }

        Assert.That(started, Does.Contain("Assistant.Chat"));
        Assert.That(started.Count(a => a == "Assistant.Llm"), Is.EqualTo(2));
        Assert.That(started, Does.Contain("Assistant.Tool"));
    }

    private static async IAsyncEnumerable<LlmStreamChunk> StreamText(string text)
    {
        yield return new LlmStreamChunk { Text = text };
        yield return new LlmStreamChunk { FinishReason = "stop" };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<LlmStreamChunk> StreamToolCall(string id, string name, string args)
    {
        yield return new LlmStreamChunk
        {
            ToolCallIndex = 0,
            ToolCallId = id,
            ToolName = name,
            ToolArguments = args
        };
        yield return new LlmStreamChunk { FinishReason = "tool_calls" };
        await Task.CompletedTask;
    }
}
