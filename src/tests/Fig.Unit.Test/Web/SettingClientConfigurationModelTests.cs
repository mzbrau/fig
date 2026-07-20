using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingClientConfigurationModelTests
{
    [Test]
    public async Task SettingEvent_WhenScriptFails_RaisesScriptFailedForSettingAndStoresLatestError()
    {
        var scriptRunner = new Mock<IScriptRunner>();
        scriptRunner
            .Setup(x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>(), It.IsAny<bool>()))
            .Returns(ScriptRunResult.Failed("ClientA", new InvalidOperationException("Unexpected token '>'")));

        var model = new SettingClientConfigurationModel("ClientA", "Description", null, hasDisplayScripts: true, scriptRunner.Object);
        var events = new List<SettingEventModel>();
        model.RegisterEventAction(args =>
        {
            events.Add(args);
            return Task.FromResult<object>(Task.CompletedTask);
        });

        await model.SettingEvent(new SettingEventModel("Parent->Child", SettingEventType.RunScript, "bad script"));

        Assert.That(model.ScriptErrors.TryGetValue("Parent->Child", out var errorMessage), Is.True);
        Assert.That(errorMessage, Is.EqualTo("Unexpected token '>'"));

        var scriptFailedEvent = events.Single(x => x.EventType == SettingEventType.ScriptFailed);
        Assert.That(scriptFailedEvent.Name, Is.EqualTo("Parent->Child"));
        Assert.That(scriptFailedEvent.Message, Is.EqualTo("Unexpected token '>'"));
        Assert.That(scriptFailedEvent.Client, Is.EqualTo(model));
    }

    [Test]
    public async Task SettingEvent_WhenScriptSucceeds_ClearsExistingErrorForSetting()
    {
        var scriptRunner = new Mock<IScriptRunner>();
        scriptRunner
            .Setup(x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>(), It.IsAny<bool>()))
            .Returns(ScriptRunResult.Succeeded("ClientA"));

        var model = new SettingClientConfigurationModel("ClientA", "Description", null, hasDisplayScripts: true, scriptRunner.Object);
        model.ScriptErrors["Parent->Child"] = "Old error";

        await model.SettingEvent(new SettingEventModel("Parent->Child", SettingEventType.RunScript, "good script"));

        Assert.That(model.ScriptErrors.ContainsKey("Parent->Child"), Is.False);
    }

    [Test]
    public async Task CreateInstance_CopiesMigrateFromSettingCount()
    {
        var model = new SettingClientConfigurationModel("ClientA", "Description", null, hasDisplayScripts: false, Mock.Of<IScriptRunner>())
        {
            MigrateFromSettingCount = 3
        };

        var createInstanceMethod = typeof(SettingClientConfigurationModel)
            .GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.NonPublic);
        var instanceTask = (Task<SettingClientConfigurationModel>)createInstanceMethod!.Invoke(model, ["Instance1"])!;
        var instance = await instanceTask;

        Assert.That(instance.MigrateFromSettingCount, Is.EqualTo(3));
    }

    [Test]
    public async Task InitializeAsync_DoesNotRunScriptsForSettingsWithoutDisplayScript()
    {
        var scriptRunner = new Mock<IScriptRunner>(MockBehavior.Strict);
        var status = new DisplayScriptStatusService(Mock.Of<IScriptRunner>());
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel(
            "ClientA", "Description", null, hasDisplayScripts: false, scriptRunner.Object,
            displayScriptStatusService: status);

        var plain = new StringSettingConfigurationModel(
            new SettingDefinitionDataContract("plain", "", new StringSettingDataContract("v"), valueType: typeof(string)),
            model, presentation);
        var withRegex = new StringSettingConfigurationModel(
            new SettingDefinitionDataContract("regex", "", new StringSettingDataContract("abc"),
                valueType: typeof(string), validationRegex: "^[a-z]+$"),
            model, presentation);

        model.Settings = [plain, withRegex];

        await model.InitializeAsync();

        scriptRunner.Verify(
            x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>(), It.IsAny<bool>()),
            Times.Never);
        Assert.That(withRegex.IsValid, Is.True);
    }

    [Test]
    public async Task InitializeAsync_RunsDisplayScriptsPerSettingViaRunScript()
    {
        var scriptRunner = new Mock<IScriptRunner>();
        scriptRunner
            .Setup(x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>(), true))
            .Returns(ScriptRunResult.Succeeded("ClientA"));
        scriptRunner
            .Setup(x => x.FormatScript(It.IsAny<string>()))
            .Returns((string s) => s);

        var status = new DisplayScriptStatusService(scriptRunner.Object);
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel(
            "ClientA", "Description", null, hasDisplayScripts: true, scriptRunner.Object,
            displayScriptStatusService: status);

        var plain = new StringSettingConfigurationModel(
            new SettingDefinitionDataContract("plain", "", new StringSettingDataContract("v"), valueType: typeof(string)),
            model, presentation);
        var scriptedA = new StringSettingConfigurationModel(
            new SettingDefinitionDataContract("a", "", new StringSettingDataContract("1"),
                valueType: typeof(string), displayScript: "a.IsVisible = true;"),
            model, presentation);
        var scriptedB = new StringSettingConfigurationModel(
            new SettingDefinitionDataContract("b", "", new StringSettingDataContract("2"),
                valueType: typeof(string), displayScript: "b.IsVisible = false;"),
            model, presentation);

        model.Settings = [plain, scriptedA, scriptedB];

        await model.InitializeAsync();

        scriptRunner.Verify(
            x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>(), true),
            Times.Exactly(2));
        scriptRunner.Verify(
            x => x.RunScripts(It.IsAny<IReadOnlyList<(string, string)>>(), It.IsAny<IScriptableClient>(), It.IsAny<bool>()),
            Times.Never);
        Assert.That(status.ExecutedCount, Is.EqualTo(2));
        Assert.That(status.SucceededCount, Is.EqualTo(2));
        Assert.That(status.IsComplete, Is.True);
    }

    [Test]
    public async Task InitializeAsync_WhenScriptFails_RecordsScriptErrorAndRaisesEvent()
    {
        var scriptRunner = new Mock<IScriptRunner>();
        scriptRunner
            .Setup(x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>(), true))
            .Returns(ScriptRunResult.Failed("ClientA", new InvalidOperationException("boom")));
        scriptRunner
            .Setup(x => x.FormatScript(It.IsAny<string>()))
            .Returns((string s) => s);

        var status = new DisplayScriptStatusService(scriptRunner.Object);
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel(
            "ClientA", "Description", null, hasDisplayScripts: true, scriptRunner.Object,
            displayScriptStatusService: status);
        var events = new List<SettingEventModel>();
        model.RegisterEventAction(args =>
        {
            events.Add(args);
            return Task.FromResult<object>(Task.CompletedTask);
        });

        model.Settings =
        [
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("scripted", "", new StringSettingDataContract("1"),
                    valueType: typeof(string), displayScript: "not valid js {{{"),
                model, presentation)
        ];

        await model.InitializeAsync();

        Assert.That(model.ScriptErrors.TryGetValue("scripted", out var error), Is.True);
        Assert.That(error, Is.EqualTo("boom"));
        Assert.That(events.Any(e => e.EventType == SettingEventType.ScriptFailed && e.Name == "scripted"), Is.True);
        Assert.That(status.FailedCount, Is.EqualTo(1));
    }
}
