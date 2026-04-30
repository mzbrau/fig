using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Client.ConfigurationProvider;
using Fig.Contracts.CustomActions;
using Fig.Contracts.LookupTable;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigClientBridgeRegistryTests
{
    [SetUp]
    public void SetUp()
    {
        FigClientBridgeRegistry.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        FigClientBridgeRegistry.Clear();
    }

    [Test]
    public void Register_ThenTryGet_ReturnsBridgeAndOptions()
    {
        var bridge = new TestBridge();
        var options = new FigClientBridgeOptions(TimeSpan.FromMilliseconds(123), TimeSpan.FromMilliseconds(456));

        FigClientBridgeRegistry.Register(typeof(TestSettings), bridge, options);

        var found = FigClientBridgeRegistry.TryGet(typeof(TestSettings), out var result, out var resultOptions);

        Assert.That(found, Is.True);
        Assert.That(result, Is.SameAs(bridge));
        Assert.That(resultOptions.CustomActionPollInterval, Is.EqualTo(TimeSpan.FromMilliseconds(123)));
        Assert.That(resultOptions.LookupTableRegistrationDelay, Is.EqualTo(TimeSpan.FromMilliseconds(456)));
    }

    [Test]
    public void Unregister_WithSameBridge_RemovesBridge()
    {
        var bridge = new TestBridge();
        FigClientBridgeRegistry.Register(typeof(TestSettings), bridge, FigClientBridgeOptions.Default);

        FigClientBridgeRegistry.Unregister(typeof(TestSettings), bridge);

        var found = FigClientBridgeRegistry.TryGet(typeof(TestSettings), out _, out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void Unregister_WithDifferentBridge_DoesNotRemoveRegisteredBridge()
    {
        var registeredBridge = new TestBridge();
        var staleBridge = new TestBridge();
        FigClientBridgeRegistry.Register(typeof(TestSettings), registeredBridge, FigClientBridgeOptions.Default);

        FigClientBridgeRegistry.Unregister(typeof(TestSettings), staleBridge);

        var found = FigClientBridgeRegistry.TryGet(typeof(TestSettings), out var result, out _);

        Assert.That(found, Is.True);
        Assert.That(result, Is.SameAs(registeredBridge));
    }

    private class TestSettings
    {
    }

    private class TestBridge : IFigClientBridge
    {
        public Task<IEnumerable<CustomActionPollResponseDataContract>?> PollForCustomActionRequests() =>
            Task.FromResult<IEnumerable<CustomActionPollResponseDataContract>?>([]);

        public Task SendCustomActionResults(CustomActionExecutionResultsDataContract results) =>
            Task.CompletedTask;

        public Task RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions) =>
            Task.CompletedTask;

        public Task RegisterLookupTable(LookupTableDataContract lookupTable) =>
            Task.CompletedTask;
    }
}

