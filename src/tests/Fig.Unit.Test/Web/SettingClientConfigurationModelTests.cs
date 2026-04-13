using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Scripting;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
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
            .Setup(x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>()))
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
            .Setup(x => x.RunScript(It.IsAny<string>(), It.IsAny<IScriptableClient>()))
            .Returns(ScriptRunResult.Succeeded("ClientA"));

        var model = new SettingClientConfigurationModel("ClientA", "Description", null, hasDisplayScripts: true, scriptRunner.Object);
        model.ScriptErrors["Parent->Child"] = "Old error";

        await model.SettingEvent(new SettingEventModel("Parent->Child", SettingEventType.RunScript, "good script"));

        Assert.That(model.ScriptErrors.ContainsKey("Parent->Child"), Is.False);
    }
}
