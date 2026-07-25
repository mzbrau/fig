using System.Diagnostics;
using System.Net.Http;
using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Observability;
using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
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
using Microsoft.Extensions.DependencyInjection;
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

    [Test]
    public void Compact_WhenOverLimit_DoesNotOrphanToolResponses()
    {
        var compactor = new AssistantHistoryCompactor();
        var messages = new List<JObject>
        {
            new() { ["role"] = "system", ["content"] = "system" }
        };

        for (var i = 0; i < 120; i++)
        {
            messages.Add(new JObject
            {
                ["role"] = i % 2 == 0 ? "user" : "assistant",
                ["content"] = new string('x', 400)
            });
        }

        messages.Add(new JObject
        {
            ["role"] = "assistant",
            ["content"] = string.Empty,
            ["tool_calls"] = new JArray
            {
                new JObject
                {
                    ["id"] = "call_1",
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = "list_clients",
                        ["arguments"] = "{}"
                    }
                }
            }
        });
        messages.Add(new JObject
        {
            ["role"] = "tool",
            ["tool_call_id"] = "call_1",
            ["content"] = new string('t', 300)
        });
        messages.Add(new JObject { ["role"] = "assistant", ["content"] = new string('z', 42_000) });

        var result = compactor.Compact(messages);

        for (var i = 0; i < result.Count; i++)
        {
            if (result[i]["role"]?.Value<string>() != "tool")
                continue;

            var preceding = i - 1;
            while (preceding >= 0 && result[preceding]["role"]?.Value<string>() == "tool")
                preceding--;

            Assert.That(preceding, Is.GreaterThanOrEqualTo(0));
            Assert.That(result[preceding]["role"]?.Value<string>(), Is.EqualTo("assistant"));
            Assert.That(result[preceding]["tool_calls"], Is.Not.Null);
            Assert.That(result[preceding]["tool_calls"]!.Type, Is.EqualTo(JTokenType.Array));
            Assert.That(((JArray)result[preceding]["tool_calls"]!).Count, Is.GreaterThan(0));
        }
    }

    [Test]
    public void Compact_WhenOverLimit_PreservesFirstUserMessage()
    {
        var compactor = new AssistantHistoryCompactor();
        const string prompt = "UNIQUE_PROMPT_most_active_users_please";
        var messages = new List<JObject>
        {
            new() { ["role"] = "system", ["content"] = "You are Fig Assistant composing a report." },
            new() { ["role"] = "user", ["content"] = prompt }
        };

        for (var i = 0; i < 40; i++)
        {
            messages.Add(new JObject
            {
                ["role"] = "assistant",
                ["content"] = string.Empty,
                ["tool_calls"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = $"call_{i}",
                        ["type"] = "function",
                        ["function"] = new JObject
                        {
                            ["name"] = "get_events",
                            ["arguments"] = "{}"
                        }
                    }
                }
            });
            messages.Add(new JObject
            {
                ["role"] = "tool",
                ["tool_call_id"] = $"call_{i}",
                ["content"] = new string('e', 3_000)
            });
        }

        var result = compactor.Compact(messages);

        Assert.That(result.Count, Is.LessThan(messages.Count));
        Assert.That(
            result.Any(a => a["role"]?.Value<string>() == "user" &&
                            a["content"]?.Value<string>() == prompt),
            Is.True);
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
        reportExecution.Setup(a => a.GetAvailableReports()).ReturnsAsync([
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
    public async Task ProposeWebActions_UpdateSetting_RequiresClientAndSettingName()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"updateSetting","clientName":"A"}]}""", CancellationToken.None),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProposeWebActions_CreateInstance_RequiresClientAndInstance()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"createInstance","clientName":"A"}]}""", CancellationToken.None),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProposeWebActions_SearchSettings_RequiresSearchQuery()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"searchSettings"}]}""", CancellationToken.None),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProposeWebActions_HighlightSetting_RequiresClientAndSettingName()
    {
        var registry = CreateRegistry(new Mock<IReportExecutionService>());
        Assert.That(registry.TryGet("propose_web_actions", out var tool), Is.True);
        Assert.That(
            async () => await tool!.ExecuteAsync("""{"actions":[{"type":"highlightSetting","settingName":"X"}]}""", CancellationToken.None),
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
public class AssistantUsernameRedactionTests
{
    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("a", "a")]
    [TestCase("ab", "ab")]
    [TestCase("abc", "a*c")]
    [TestCase("admin", "a***n")]
    public void RedactUsername_MasksMiddleCharacters(string? username, string expected)
    {
        Assert.That(AssistantTrace.RedactUsername(username), Is.EqualTo(expected));
    }

    [Test]
    public void RedactUsernameInText_ReplacesExactOccurrences()
    {
        const string username = "admin";
        var text = """Authenticated user: admin / "Username": "admin" / administrator-facing""";

        var redacted = AssistantTrace.RedactUsernameInText(text, username);

        Assert.That(redacted, Does.Not.Contain("Authenticated user: admin"));
        Assert.That(redacted, Does.Contain("a***n"));
        Assert.That(redacted, Does.Contain("administrator-facing"));
    }

    [Test]
    public void RedactUsernameInText_WhenUsernameEmpty_ReturnsOriginal()
    {
        const string text = "no change";
        Assert.That(AssistantTrace.RedactUsernameInText(text, null), Is.EqualTo(text));
        Assert.That(AssistantTrace.RedactUsernameInText(text, ""), Is.EqualTo(text));
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
        string? tracedUsername = null;
        IReadOnlyList<JObject>? llmMessages = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApiActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started.Add(activity.OperationName),
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "Assistant.Chat")
                    tracedUsername = activity.GetTagItem("fig.assistant.username") as string;

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
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Callback<IReadOnlyList<JObject>, IReadOnlyCollection<IAssistantTool>, CancellationToken, double?>(
                (messages, _, _, _) => llmMessages = messages.ToList())
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
        Assert.That(tracedUsername, Is.EqualTo("a***n"));
        Assert.That(llmRequestPayloads, Is.Not.Empty);
        Assert.That(llmRequestPayloads[0], Does.Contain("You are Fig Assistant"));
        Assert.That(llmRequestPayloads[0], Does.Contain("What clients exist?"));
        Assert.That(llmRequestPayloads[0], Does.Contain("exact matched setting name"));
        Assert.That(llmRequestPayloads[0], Does.Contain("administrator-facing"));
        Assert.That(llmRequestPayloads[0], Does.Not.Contain("Authenticated user: admin"));
        Assert.That(llmRequestPayloads[0], Does.Contain("Authenticated user: a***n"));
        Assert.That(llmRequestPayloads[0], Does.Contain("Username"));
        Assert.That(llmRequestPayloads[0], Does.Contain("a***n"));
        Assert.That(llmMessages, Is.Not.Null);
        Assert.That(llmMessages![0]["content"]?.Value<string>(), Does.Contain("Authenticated user: admin"));
        Assert.That(llmMessages[0]["content"]?.Value<string>(), Does.Contain("\"Username\":\"admin\""));
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
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
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

[TestFixture]
public class AiComposedReportToolSelectionTests
{
    [Test]
    public void CuratedToolNames_ExcludesHeavyDumpTools()
    {
        Assert.That(AiComposedReport.CuratedToolNames, Does.Contain("get_events"));
        Assert.That(AiComposedReport.CuratedToolNames, Does.Contain("list_reports"));
        Assert.That(AiComposedReport.CuratedToolNames, Does.Not.Contain("list_clients"));
        Assert.That(AiComposedReport.CuratedToolNames, Does.Not.Contain("get_client_settings"));
        Assert.That(AiComposedReport.CuratedToolNames, Does.Not.Contain("get_lookup_table"));
        Assert.That(AiComposedReport.CuratedToolNames, Does.Not.Contain("fetch_fig_doc"));
    }

    [Test]
    public void ResolveCuratedTools_ReturnsOnlyCuratedTools()
    {
        var registry = new Mock<IAssistantToolRegistry>();
        registry.Setup(a => a.TryGet(It.IsAny<string>(), out It.Ref<IAssistantTool?>.IsAny))
            .Returns(new TryGetTool((string name, out IAssistantTool? tool) =>
            {
                if (!AiComposedReport.CuratedToolNames.Contains(name) &&
                    name is not ("list_clients" or "get_client_settings"))
                {
                    tool = null;
                    return false;
                }

                var mock = new Mock<IAssistantTool>();
                mock.SetupGet(t => t.Name).Returns(name);
                tool = mock.Object;
                return true;
            }));

        var tools = AiComposedReport.ResolveCuratedTools(registry.Object);
        var names = tools.Select(t => t.Name).ToArray();

        Assert.That(names, Is.EquivalentTo(AiComposedReport.CuratedToolNames));
        Assert.That(names, Does.Not.Contain("list_clients"));
        Assert.That(names, Does.Not.Contain("get_client_settings"));
    }

    private delegate bool TryGetTool(string name, out IAssistantTool? tool);
}

[TestFixture]
public class AssistantBackgroundRunnerNudgeTests
{
    [Test]
    public async Task RunAsync_OnFinalIterations_NudgesSubmitAiReportWhenUnused()
    {
        var seenMessages = new List<IReadOnlyList<JObject>>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Callback<IReadOnlyList<JObject>, IReadOnlyCollection<IAssistantTool>, CancellationToken, double?>(
                (messages, _, _, _) => seenMessages.Add(messages.Select(m => (JObject)m.DeepClone()).ToList()))
            .Returns(() => StreamReadOnlyToolCall());

        var readTool = new Mock<IAssistantTool>();
        readTool.SetupGet(a => a.Name).Returns("get_events");
        readTool.SetupGet(a => a.Description).Returns("events");
        readTool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");
        readTool.Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"events":[]}""");

        var submitTool = new Mock<IAssistantTool>();
        submitTool.SetupGet(a => a.Name).Returns("submit_ai_report");
        submitTool.SetupGet(a => a.Description).Returns("submit");
        submitTool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(a => a.Decrypt("encrypted-token", It.IsAny<bool>(), It.IsAny<bool>())).Returns("plain-token");

        var configuration = new Mock<IConfigurationRepository>();
        configuration.Setup(a => a.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = true,
            FigAssistantEndpoint = "https://llm.example",
            FigAssistantModel = "test-model",
            FigAssistantAccessTokenEncrypted = "encrypted-token",
            FigAssistantMaxToolIterations = 3,
            FigAssistantRequestTimeoutSeconds = 30
        });

        var runner = new AssistantBackgroundRunner(
            llm.Object,
            new AssistantHistoryCompactor(),
            configuration.Object,
            encryption.Object);
        runner.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false));

        Assert.That(
            async () => await runner.RunAsync(
                "ai-composed-report",
                "system",
                "user prompt",
                [readTool.Object, submitTool.Object],
                CancellationToken.None),
            Throws.InvalidOperationException.With.Message.Contain("iteration limit"));

        Assert.That(seenMessages, Has.Count.EqualTo(3));
        Assert.That(
            seenMessages[0].Any(m =>
                m["content"]?.Value<string>()?.Contains("submit_ai_report now", StringComparison.Ordinal) == true),
            Is.False);
        Assert.That(
            seenMessages[1].Any(m =>
                m["content"]?.Value<string>()?.Contains("submit_ai_report now", StringComparison.Ordinal) == true),
            Is.True);
        Assert.That(
            seenMessages[2].Any(m =>
                m["content"]?.Value<string>()?.Contains("submit_ai_report now", StringComparison.Ordinal) == true),
            Is.True);
    }

    [Test]
    public async Task RunAsync_WithSubmitTool_TextOnlyReplyContinuesWithNudge()
    {
        var seenMessages = new List<IReadOnlyList<JObject>>();
        var callCount = 0;
        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Callback<IReadOnlyList<JObject>, IReadOnlyCollection<IAssistantTool>, CancellationToken, double?>(
                (messages, _, _, _) => seenMessages.Add(messages.Select(m => (JObject)m.DeepClone()).ToList()))
            .Returns(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => StreamTextReply("Sorry, I need a different timeframe."),
                    2 => StreamSubmitToolCall(),
                    _ => StreamTextReply("Done.")
                };
            });

        var submitTool = new Mock<IAssistantTool>();
        submitTool.SetupGet(a => a.Name).Returns("submit_ai_report");
        submitTool.SetupGet(a => a.Description).Returns("submit");
        submitTool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");
        submitTool.Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"ok":true,"title":"Report","sectionCount":1}""");

        var runner = CreateRunner(llm.Object, maxIterations: 4);
        var result = await runner.RunAsync(
            "ai-composed-report",
            "system",
            "user prompt",
            [submitTool.Object],
            CancellationToken.None);

        Assert.That(seenMessages, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(
            seenMessages[1].Any(m =>
                m["role"]?.Value<string>() == "assistant" &&
                m["content"]?.Value<string>()?.Contains("different timeframe", StringComparison.Ordinal) == true),
            Is.True);
        Assert.That(
            seenMessages[1].Any(m =>
                m["content"]?.Value<string>()?.Contains("Never reply in prose", StringComparison.Ordinal) == true),
            Is.True);
        Assert.That(result.ToolCalls.Any(t => t.Name == "submit_ai_report"), Is.True);
    }

    [Test]
    public async Task RunAsync_WithoutSubmitTool_TextOnlyReplyCompletes()
    {
        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Returns(StreamTextReply("Analysis complete."));

        var readTool = new Mock<IAssistantTool>();
        readTool.SetupGet(a => a.Name).Returns("get_events");
        readTool.SetupGet(a => a.Description).Returns("events");
        readTool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");

        var runner = CreateRunner(llm.Object, maxIterations: 4);
        var result = await runner.RunAsync(
            "background-analysis",
            "system",
            "user prompt",
            [readTool.Object],
            CancellationToken.None);

        Assert.That(result.AssistantText, Is.EqualTo("Analysis complete."));
        Assert.That(result.ToolCalls, Is.Empty);
    }

    private static AssistantBackgroundRunner CreateRunner(ILlmClient llm, int maxIterations)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(a => a.Decrypt("encrypted-token", It.IsAny<bool>(), It.IsAny<bool>())).Returns("plain-token");

        var configuration = new Mock<IConfigurationRepository>();
        configuration.Setup(a => a.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = true,
            FigAssistantEndpoint = "https://llm.example",
            FigAssistantModel = "test-model",
            FigAssistantAccessTokenEncrypted = "encrypted-token",
            FigAssistantMaxToolIterations = maxIterations,
            FigAssistantRequestTimeoutSeconds = 30
        });

        var runner = new AssistantBackgroundRunner(
            llm,
            new AssistantHistoryCompactor(),
            configuration.Object,
            encryption.Object);
        runner.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            [],
            false));
        return runner;
    }

    private static async IAsyncEnumerable<LlmStreamChunk> StreamTextReply(string text)
    {
        yield return new LlmStreamChunk { Text = text };
        yield return new LlmStreamChunk { FinishReason = "stop" };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<LlmStreamChunk> StreamSubmitToolCall()
    {
        yield return new LlmStreamChunk
        {
            ToolCallIndex = 0,
            ToolCallId = "call_submit",
            ToolName = "submit_ai_report",
            ToolArguments = """{"title":"Report","sections":[{"type":"markdown","content":"ok"}]}"""
        };
        yield return new LlmStreamChunk { FinishReason = "tool_calls" };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<LlmStreamChunk> StreamReadOnlyToolCall()
    {
        yield return new LlmStreamChunk
        {
            ToolCallIndex = 0,
            ToolCallId = "call_read",
            ToolName = "get_events",
            ToolArguments = "{}"
        };
        yield return new LlmStreamChunk { FinishReason = "tool_calls" };
        await Task.CompletedTask;
    }

    [Test]
    public async Task RunAsync_WithSubmitTool_FirstInvalidSubmitInjectsCorrectionNudge()
    {
        var seenMessages = new List<IReadOnlyList<JObject>>();
        var callCount = 0;
        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Callback<IReadOnlyList<JObject>, IReadOnlyCollection<IAssistantTool>, CancellationToken, double?>(
                (messages, _, _, _) => seenMessages.Add(messages.Select(m => (JObject)m.DeepClone()).ToList()))
            .Returns(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => StreamSubmitToolCall(),
                    2 => StreamSubmitToolCall(),
                    _ => StreamTextReply("Done.")
                };
            });

        var submitAttempts = 0;
        var submitTool = new Mock<IAssistantTool>();
        submitTool.SetupGet(a => a.Name).Returns("submit_ai_report");
        submitTool.SetupGet(a => a.Description).Returns("submit");
        submitTool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");
        submitTool.Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                submitAttempts++;
                return submitAttempts == 1
                    ? """{"error":"chart sections require data: [{\"label\":\"Alice\",\"value\":3}]."}"""
                    : """{"ok":true,"title":"Report","sectionCount":1}""";
            });

        var runner = CreateRunner(llm.Object, maxIterations: 4);
        var result = await runner.RunAsync(
            "ai-composed-report",
            "system",
            "user prompt",
            [submitTool.Object],
            CancellationToken.None);

        Assert.That(seenMessages, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(
            seenMessages[1].Any(m =>
                m["role"]?.Value<string>() == "system" &&
                m["content"]?.Value<string>()?.Contains("validation failed", StringComparison.Ordinal) == true),
            Is.True);
        Assert.That(result.ToolCalls.Count(t => t.Name == "submit_ai_report"), Is.EqualTo(2));
        Assert.That(result.ToolCalls.Last().Result, Does.Contain("\"ok\":true"));
    }

    [Test]
    public async Task RunAsync_WithSubmitTool_SecondInvalidSubmitDoesNotInjectAnotherNudge()
    {
        var seenMessages = new List<IReadOnlyList<JObject>>();
        var callCount = 0;
        var llm = new Mock<ILlmClient>();
        llm.Setup(a => a.StreamChatAsync(
                It.IsAny<IReadOnlyList<JObject>>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Callback<IReadOnlyList<JObject>, IReadOnlyCollection<IAssistantTool>, CancellationToken, double?>(
                (messages, _, _, _) => seenMessages.Add(messages.Select(m => (JObject)m.DeepClone()).ToList()))
            .Returns(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => StreamSubmitToolCall(),
                    2 => StreamSubmitToolCall(),
                    3 => StreamSubmitToolCall(),
                    _ => StreamTextReply("Done.")
                };
            });

        var submitTool = new Mock<IAssistantTool>();
        submitTool.SetupGet(a => a.Name).Returns("submit_ai_report");
        submitTool.SetupGet(a => a.Description).Returns("submit");
        submitTool.SetupGet(a => a.ParameterJsonSchema).Returns("""{"type":"object","properties":{}}""");
        submitTool.Setup(a => a.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"error":"AI report document requires a non-empty title."}""");

        var runner = CreateRunner(llm.Object, maxIterations: 3);

        Assert.That(
            async () => await runner.RunAsync(
                "ai-composed-report",
                "system",
                "user prompt",
                [submitTool.Object],
                CancellationToken.None),
            Throws.InvalidOperationException.With.Message.Contain("iteration limit"));

        // The correction nudge is retained across iterations; count only in the final history snapshot.
        var correctionNudgesInFinalHistory = seenMessages.Last()
            .Count(m =>
                m["role"]?.Value<string>() == "system" &&
                m["content"]?.Value<string>()?.Contains("validation failed", StringComparison.Ordinal) == true);
        Assert.That(correctionNudgesInFinalHistory, Is.EqualTo(1));
        Assert.That(
            seenMessages[0].Any(m =>
                m["content"]?.Value<string>()?.Contains("validation failed", StringComparison.Ordinal) == true),
            Is.False);
        Assert.That(
            seenMessages[1].Any(m =>
                m["content"]?.Value<string>()?.Contains("validation failed", StringComparison.Ordinal) == true),
            Is.True);
    }
}

[TestFixture]
public class AssistantEventLogQueryTests
{
    [Test]
    public void BuildEventLogQuery_ParsesFiltersAndClampsMaxResults()
    {
        var args = JObject.Parse("""
            {
              "clientName": "MyApp",
              "instance": "prod",
              "authenticatedUser": "alice",
              "eventTypes": ["Login", "Setting value updated", ""],
              "searchText": "timeout",
              "maxResults": 999
            }
            """);

        var query = AssistantToolRegistry.BuildEventLogQuery(args);

        Assert.That(query.ClientName, Is.EqualTo("MyApp"));
        Assert.That(query.Instance, Is.EqualTo("prod"));
        Assert.That(query.AuthenticatedUser, Is.EqualTo("alice"));
        Assert.That(query.EventTypes, Is.EquivalentTo(new[] { "Login", "Setting value updated" }));
        Assert.That(query.SearchText, Is.EqualTo("timeout"));
        Assert.That(query.MaxResults, Is.EqualTo(AssistantToolRegistry.MaxEventMaxResults));
    }

    [Test]
    public void BuildEventLogQuery_DefaultsMaxResultsWhenOmitted()
    {
        var query = AssistantToolRegistry.BuildEventLogQuery(new JObject());

        Assert.That(query.ClientName, Is.Null);
        Assert.That(query.EventTypes, Is.Null);
        Assert.That(query.MaxResults, Is.EqualTo(AssistantToolRegistry.DefaultEventMaxResults));
    }

    [Test]
    public void BuildEventLogQuery_AcceptsSingleEventTypeString()
    {
        var args = new JObject { ["eventTypes"] = "Login" };
        var query = AssistantToolRegistry.BuildEventLogQuery(args);
        Assert.That(query.EventTypes, Is.EquivalentTo(new[] { "Login" }));
    }
}

[TestFixture]
public class AiComposedReportErrorMappingTests
{
    [Test]
    public void ExecuteAsync_MapsIterationLimitToDidNotSubmitError()
    {
        var background = new Mock<IAssistantBackgroundRunner>();
        background.Setup(a => a.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .ThrowsAsync(new InvalidOperationException(
                "The assistant reached the configured tool iteration limit."));

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(a => a.Decrypt("encrypted-token", It.IsAny<bool>(), It.IsAny<bool>())).Returns("plain-token");

        var configuration = new Mock<IConfigurationRepository>();
        configuration.Setup(a => a.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            EnableFigAssistant = true,
            FigAssistantEndpoint = "https://llm.example",
            FigAssistantModel = "test-model",
            FigAssistantAccessTokenEncrypted = "encrypted-token"
        });

        var registry = new Mock<IAssistantToolRegistry>();
        registry.Setup(a => a.TryGet(It.IsAny<string>(), out It.Ref<IAssistantTool?>.IsAny)).Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(registry.Object);
        var provider = services.BuildServiceProvider();

        var report = new AiComposedReport(
            background.Object,
            provider,
            configuration.Object,
            encryption.Object);

        Assert.That(
            async () => await report.ExecuteAsync(
                new AiComposedReportParameters { Prompt = "Who are the most active users?" },
                CancellationToken.None),
            Throws.InvalidOperationException
                .With.Message.Contain("did not submit a valid report")
                .And.InnerException.Message.Contain("iteration limit"));
    }
}
