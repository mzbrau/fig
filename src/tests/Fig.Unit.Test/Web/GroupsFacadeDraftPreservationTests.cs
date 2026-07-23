using Fig.Common.Events;
using Fig.Contracts.LookupTable;
using Fig.Contracts.SettingGroups;
using Fig.Web.Converters;
using Fig.Web.Facades;
using Fig.Web.Models.LookupTables;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class GroupsFacadeDraftPreservationTests
{
    [Test]
    public async Task LoadAll_PreservesUnsavedDrafts()
    {
        var http = new Mock<IHttpService>();
        http.Setup(a => a.Get<List<SettingGroupDataContract>>("settinggroups", true))
            .ReturnsAsync([
                new SettingGroupDataContract(Guid.NewGuid(), "Existing", null, [])
            ]);

        var facade = new GroupsFacade(http.Object, Mock.Of<IEventDistributor>());
        facade.AddDraftGroup("DraftOps", "from assistant");

        await facade.LoadAll();

        Assert.That(facade.Items, Has.Count.EqualTo(2));
        Assert.That(facade.Items.Any(g => g.Id == null && g.Name == "DraftOps"), Is.True);
        Assert.That(facade.Items.Any(g => g.Id != null && g.Name == "Existing"), Is.True);
    }

    [Test]
    public async Task LoadAll_DropsDraftWhenServerGroupHasSameName()
    {
        var http = new Mock<IHttpService>();
        http.Setup(a => a.Get<List<SettingGroupDataContract>>("settinggroups", true))
            .ReturnsAsync([
                new SettingGroupDataContract(Guid.NewGuid(), "Ops", "server", [])
            ]);

        var facade = new GroupsFacade(http.Object, Mock.Of<IEventDistributor>());
        facade.AddDraftGroup("ops", "local");

        await facade.LoadAll();

        Assert.That(facade.Items, Has.Count.EqualTo(1));
        Assert.That(facade.Items[0].Id, Is.Not.Null);
        Assert.That(facade.Items[0].Description, Is.EqualTo("server"));
    }

    [Test]
    public void AddDraftGroup_RaisesItemsChanged()
    {
        var raised = 0;
        var facade = new GroupsFacade(Mock.Of<IHttpService>(), Mock.Of<IEventDistributor>());
        facade.ItemsChanged += () => raised++;

        facade.AddDraftGroup("Ops");

        Assert.That(raised, Is.EqualTo(1));
    }
}

[TestFixture]
public class LookupTableFacadeDraftPreservationTests
{
    [Test]
    public async Task LoadAll_PreservesUnsavedDrafts()
    {
        var http = new Mock<IHttpService>();
        http.Setup(a => a.Get<List<LookupTableDataContract>>("lookuptables", true))
            .ReturnsAsync([
                new LookupTableDataContract(Guid.NewGuid(), "Existing", new Dictionary<string, string?>(), false)
            ]);

        var converter = new Mock<ILookupTableConverter>();
        converter.Setup(a => a.Convert(It.IsAny<List<LookupTableDataContract>>()))
            .Returns((List<LookupTableDataContract> contracts) =>
                contracts.Select(c => new LookupTable(c.Name, "1,x") { Id = c.Id }).ToList());

        var facade = new LookupTableFacade(http.Object, converter.Object, Mock.Of<IEventDistributor>());
        facade.CreateDraft("DraftTable", "1,AU");

        await facade.LoadAll();

        Assert.That(facade.Items, Has.Count.EqualTo(2));
        Assert.That(facade.Items.Any(t => t.Id == null && t.Name == "DraftTable"), Is.True);
        Assert.That(facade.Items.Any(t => t.Id != null && t.Name == "Existing"), Is.True);
    }
}
