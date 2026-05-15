using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Exceptions;
using Fig.Contracts.CustomActions;
using Fig.Contracts.LookupTable;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class SettingUpdaterTests
{
    [TearDown]
    public void TearDown()
    {
        FigClientBridgeRegistry.Clear();
    }

    [Test]
    public void Create_ShouldMapNestedSettingExpressionToFigSettingName()
    {
        var contract = SettingUpdateContractFactory.Create((UpdaterTestSettings s) => s.Nested.Value, "updated");

        Assert.That(contract.Name, Is.EqualTo("Nested->Value"));
        Assert.That(contract.Value, Is.TypeOf<StringSettingDataContract>());
        Assert.That(contract.Value?.GetValue(), Is.EqualTo("updated"));
    }

    [Test]
    public void Create_ShouldMarkSecretSettingsAsSecret()
    {
        var contract = SettingUpdateContractFactory.Create((UpdaterTestSettings s) => s.Password, "secret");

        Assert.That(contract.IsSecret, Is.True);
    }

    [Test]
    public void Create_ShouldConvertTypedDataGridRows()
    {
        var rows = new List<UpdaterRow>
        {
            new() { Name = "First", Count = 1, Pet = UpdaterPet.Dog }
        };

        var contract = SettingUpdateContractFactory.Create((UpdaterTestSettings s) => s.Rows, rows);

        var dataGrid = (DataGridSettingDataContract)contract.Value!;
        Assert.That(dataGrid.Value, Has.Count.EqualTo(1));
        Assert.That(dataGrid.Value![0]["Name"], Is.EqualTo("First"));
        Assert.That(dataGrid.Value[0]["Count"], Is.EqualTo(1));
        Assert.That(dataGrid.Value[0]["Pet"], Is.EqualTo("Dog"));
        Assert.That(dataGrid.Value[0], Does.Not.ContainKey(nameof(UpdaterRow.IgnoredThing)));
    }

    [Test]
    public async Task ApplyAsync_ShouldSendBatchedUpdatesToBridge()
    {
        var bridge = new CapturingBridge();
        FigClientBridgeRegistry.Register(typeof(UpdaterTestSettings), bridge, FigClientBridgeOptions.Default);
        var updater = new SettingUpdater<UpdaterTestSettings>();

        await updater
            .Set(s => s.Name, "NewName")
            .Set(s => s.Count, 12)
            .WithMessage("Updated by test")
            .ApplyAsync();

        Assert.That(bridge.Updates, Is.Not.Null);
        Assert.That(bridge.Updates!.ChangeMessage, Is.EqualTo("Updated by test"));
        Assert.That(bridge.Updates.ValueUpdates.Select(a => a.Name), Is.EquivalentTo(new[] { "Name", "Count" }));
    }

    [Test]
    public async Task ApplyAsync_WhenReusedWithoutNewUpdates_ShouldThrow()
    {
        var bridge = new CapturingBridge();
        FigClientBridgeRegistry.Register(typeof(UpdaterTestSettings), bridge, FigClientBridgeOptions.Default);
        var updater = new SettingUpdater<UpdaterTestSettings>();

        await updater.Set(s => s.Name, "NewName").ApplyAsync();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => updater.ApplyAsync());
        Assert.That(ex!.Message, Is.EqualTo("At least one setting update must be supplied."));
    }

    [Test]
    public async Task ApplyAsync_WhenReusedWithNewUpdates_ShouldUseDefaultMessage()
    {
        var bridge = new CapturingBridge();
        FigClientBridgeRegistry.Register(typeof(UpdaterTestSettings), bridge, FigClientBridgeOptions.Default);
        var updater = new SettingUpdater<UpdaterTestSettings>();

        await updater.Set(s => s.Name, "NewName").WithMessage("Custom message").ApplyAsync();
        await updater.Set(s => s.Count, 10).ApplyAsync();

        Assert.That(bridge.Updates!.ChangeMessage, Is.EqualTo("Client self-update from application"));
    }

    [Test]
    public void ApplyAsync_WhenFigIsNotInitialized_ShouldThrow()
    {
        var updater = new SettingUpdater<UpdaterTestSettings>();

        var ex = Assert.ThrowsAsync<ConfigurationException>(() =>
            updater.Set(s => s.Name, "NewName").ApplyAsync());

        Assert.That(ex!.Message, Does.Contain(typeof(UpdaterTestSettings).FullName));
    }

    private sealed class CapturingBridge : IFigClientBridge
    {
        public SettingValueUpdatesDataContract? Updates { get; private set; }

        public Task<IEnumerable<CustomActionPollResponseDataContract>?> PollForCustomActionRequests()
        {
            return Task.FromResult<IEnumerable<CustomActionPollResponseDataContract>?>(null);
        }

        public Task SendCustomActionResults(CustomActionExecutionResultsDataContract results)
        {
            return Task.CompletedTask;
        }

        public Task RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions)
        {
            return Task.CompletedTask;
        }

        public Task RegisterLookupTable(LookupTableDataContract lookupTable)
        {
            return Task.CompletedTask;
        }

        public Task UpdateSettings(SettingValueUpdatesDataContract updates)
        {
            Updates = updates;
            return Task.CompletedTask;
        }
    }

    private sealed class UpdaterTestSettings : SettingsBase
    {
        public override string ClientDescription => "Updater test settings";

        [Setting("name")]
        public string Name { get; set; } = "Name";

        [Setting("count")]
        public int Count { get; set; }

        [Setting("password")]
        [Secret]
        public string Password { get; set; } = "secret";

        [Setting("rows")]
        public List<UpdaterRow> Rows { get; set; } = [];

        [NestedSetting]
        public UpdaterNested Nested { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private sealed class UpdaterNested
    {
        [Setting("value")]
        public string Value { get; set; } = "nested";
    }

    private sealed class UpdaterRow
    {
        public string Name { get; set; } = string.Empty;

        public int Count { get; set; }

        public UpdaterPet Pet { get; set; }

        [FigIgnore]
        public UpdaterIgnoredThing IgnoredThing { get; set; } = new();
    }

    private sealed class UpdaterIgnoredThing
    {
        public string Value { get; set; } = "ignored";
    }

    private enum UpdaterPet
    {
        Cat,
        Dog
    }
}
