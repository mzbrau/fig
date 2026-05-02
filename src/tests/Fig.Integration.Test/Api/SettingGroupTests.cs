using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Data;
using Fig.Common.Constants;
using Fig.Contracts.Authentication;
using Fig.Contracts.EventHistory;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;
using Fig.Integration.Test.Utils;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SettingGroupTests : IntegrationTestBase
{
    private SettingGroupDataContract CreateTestGroup(string name, string? description = null)
    {
        return new SettingGroupDataContract(
            null,
            name,
            description,
            new List<GroupedSettingDataContract>());
    }

    private SettingGroupDataContract CreateTestGroupWithSettings(string name, string? description = null)
    {
        return new SettingGroupDataContract(
            null,
            name,
            description,
            new List<GroupedSettingDataContract>
            {
                new("ConnectionString", "Database connection", "String",
                    new List<SourceSettingDataContract>
                    {
                        new("ServiceA", "ConnectionString"),
                        new("ServiceB", "ConnectionString")
                    }),
                new("MaxRetries", "Max retry count", "Int",
                    new List<SourceSettingDataContract>
                    {
                        new("ServiceA", "MaxRetries")
                    })
            });
    }

    [Test]
    public async Task ShallCreateSettingGroup()
    {
        var group = CreateTestGroup("DatabaseSettings", "All database related settings");

        var result = await CreateSettingGroup(group);

        Assert.That(result.Id, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("DatabaseSettings"));
        Assert.That(result.Description, Is.EqualTo("All database related settings"));
        Assert.That(result.CreatedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(5))));
        Assert.That(result.LastModifiedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(5))));
    }

    [Test]
    public async Task ShallCreateSettingGroupWithGroupedSettings()
    {
        var group = CreateTestGroupWithSettings("DatabaseSettings", "DB settings");

        var result = await CreateSettingGroup(group);

        Assert.That(result.Id, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("DatabaseSettings"));
        Assert.That(result.GroupedSettings.Count, Is.EqualTo(2));
        Assert.That(result.GroupedSettings[0].Name, Is.EqualTo("ConnectionString"));
        Assert.That(result.GroupedSettings[0].SourceSettings.Count, Is.EqualTo(2));
        Assert.That(result.GroupedSettings[1].Name, Is.EqualTo("MaxRetries"));
    }

    [Test]
    public async Task ShallGetAllSettingGroups()
    {
        var group1 = CreateTestGroup("Group1", "First group");
        var group2 = CreateTestGroup("Group2", "Second group");

        await CreateSettingGroup(group1);
        await CreateSettingGroup(group2);

        var allGroups = await GetAllSettingGroups();

        Assert.That(allGroups.Count, Is.EqualTo(2));
        Assert.That(allGroups.Select(g => g.Name), Is.EquivalentTo(new[] { "Group1", "Group2" }));
    }

    [Test]
    public async Task ShallGetSingleSettingGroupById()
    {
        var group = CreateTestGroupWithSettings("TestGroup", "Test description");
        var created = await CreateSettingGroup(group);

        var result = await GetSettingGroup(created.Id!.Value);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(created.Id));
        Assert.That(result.Name, Is.EqualTo("TestGroup"));
        Assert.That(result.Description, Is.EqualTo("Test description"));
        Assert.That(result.GroupedSettings.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallUpdateSettingGroupNameAndDescription()
    {
        var group = CreateTestGroup("OriginalName", "Original description");
        var created = await CreateSettingGroup(group);

        var updated = new SettingGroupDataContract(
            created.Id,
            "UpdatedName",
            "Updated description",
            created.GroupedSettings);

        var result = await UpdateSettingGroup(created.Id!.Value, updated);

        Assert.That(result.Name, Is.EqualTo("UpdatedName"));
        Assert.That(result.Description, Is.EqualTo("Updated description"));

        var fetched = await GetSettingGroup(created.Id!.Value);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Name, Is.EqualTo("UpdatedName"));
        Assert.That(fetched.Description, Is.EqualTo("Updated description"));
    }

    [Test]
    public async Task ShallDeleteSettingGroup()
    {
        var group1 = CreateTestGroup("ToKeep");
        var group2 = CreateTestGroup("ToDelete");

        await CreateSettingGroup(group1);
        var created2 = await CreateSettingGroup(group2);

        await DeleteSettingGroup(created2.Id!.Value);

        var allGroups = await GetAllSettingGroups();
        Assert.That(allGroups.Count, Is.EqualTo(1));
        Assert.That(allGroups[0].Name, Is.EqualTo("ToKeep"));
    }

    [Test]
    public async Task ShallReturnBadRequestForDuplicateGroupName()
    {
        var group = CreateTestGroup("DuplicateName");
        await CreateSettingGroup(group);

        var duplicate = CreateTestGroup("DuplicateName");
        var response = await CreateSettingGroupRaw(duplicate);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ShallReturnErrorForEmptyGroupName()
    {
        var group = CreateTestGroup("");

        var response = await CreateSettingGroupRaw(group);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ShallReturnNotFoundForNonExistentGroup()
    {
        var nonExistentId = Guid.NewGuid();

        await ApiClient.GetAndVerify($"/settinggroups/{nonExistentId}", HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallExportSettingGroups()
    {
        var group1 = CreateTestGroupWithSettings("ExportGroup1", "First export group");
        var group2 = CreateTestGroup("ExportGroup2", "Second export group");

        await CreateSettingGroup(group1);
        await CreateSettingGroup(group2);

        var exported = await ExportSettingGroups();

        Assert.That(exported.ExportedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(5))));
        Assert.That(exported.Version, Is.EqualTo(1));
        Assert.That(exported.Groups.Count, Is.EqualTo(2));
        var exportGroup1 = exported.Groups.FirstOrDefault(g => g.Name == "ExportGroup1");
        var exportGroup2 = exported.Groups.FirstOrDefault(g => g.Name == "ExportGroup2");
        Assert.That(exportGroup1, Is.Not.Null);
        Assert.That(exportGroup1!.GroupedSettings.Count, Is.EqualTo(2));
        Assert.That(exportGroup2, Is.Not.Null);
    }

    [Test]
    public async Task ShallImportSettingGroupsWithAddNew()
    {
        var existingGroup = CreateTestGroup("ExistingGroup", "Already exists");
        await CreateSettingGroup(existingGroup);

        var importData = new SettingGroupExportDataContract(
            DateTime.UtcNow,
            1,
            new List<SettingGroupDataContract>
            {
                CreateTestGroup("ExistingGroup", "Should be ignored"),
                CreateTestGroup("NewGroup", "Should be imported")
            });

        var result = await ImportSettingGroups(importData, ImportType.AddNew);

        Assert.That(result.ErrorMessage, Is.Null);

        var allGroups = await GetAllSettingGroups();
        Assert.That(allGroups.Count, Is.EqualTo(2));
        // ExistingGroup retains original description (not overwritten by AddNew)
        var existing = allGroups.First(g => g.Name == "ExistingGroup");
        Assert.That(existing.Description, Is.EqualTo("Already exists"));
        // NewGroup was imported
        Assert.That(allGroups.Any(g => g.Name == "NewGroup"), Is.True);
    }

    [Test]
    public async Task ShallImportSettingGroupsWithClearAndImport()
    {
        var existingGroup = CreateTestGroup("OldGroup", "Will be deleted");
        await CreateSettingGroup(existingGroup);

        var importData = new SettingGroupExportDataContract(
            DateTime.UtcNow,
            1,
            new List<SettingGroupDataContract>
            {
                CreateTestGroup("FreshGroup1", "Imported fresh"),
                CreateTestGroup("FreshGroup2", "Also imported fresh")
            });

        var result = await ImportSettingGroups(importData, ImportType.ClearAndImport);

        Assert.That(result.ErrorMessage, Is.Null);

        var allGroups = await GetAllSettingGroups();
        Assert.That(allGroups.Count, Is.EqualTo(2));
        Assert.That(allGroups.Any(g => g.Name == "OldGroup"), Is.False);
        Assert.That(allGroups.Any(g => g.Name == "FreshGroup1"), Is.True);
        Assert.That(allGroups.Any(g => g.Name == "FreshGroup2"), Is.True);
    }

    [Test]
    public async Task ShallLogEventWhenGroupCreated()
    {
        var startTime = DateTime.UtcNow;

        var group = CreateTestGroup("EventTestGroup");
        await CreateSettingGroup(group);

        var endTime = DateTime.UtcNow;
        var events = await GetEvents(startTime, endTime);
        var nonCheckPointEvents = events.Events.RemoveCheckPointEvents();

        Assert.That(nonCheckPointEvents.Any(e => e.EventType == EventMessage.GroupCreated), Is.True);
    }

    [Test]
    public async Task ShallLogEventWhenGroupUpdated()
    {
        var group = CreateTestGroup("UpdateEventGroup");
        var created = await CreateSettingGroup(group);

        var startTime = DateTime.UtcNow;

        var updated = new SettingGroupDataContract(
            created.Id,
            "UpdatedEventGroup",
            "Updated",
            created.GroupedSettings);
        await UpdateSettingGroup(created.Id!.Value, updated);

        var endTime = DateTime.UtcNow;
        var events = await GetEvents(startTime, endTime);
        var nonCheckPointEvents = events.Events.RemoveCheckPointEvents();

        Assert.That(nonCheckPointEvents.Any(e => e.EventType == EventMessage.GroupUpdated), Is.True);
    }

    [Test]
    public async Task ShallLogEventWhenGroupDeleted()
    {
        var group = CreateTestGroup("DeleteEventGroup");
        var created = await CreateSettingGroup(group);

        var startTime = DateTime.UtcNow;
        await DeleteSettingGroup(created.Id!.Value);
        var endTime = DateTime.UtcNow;

        var events = await GetEvents(startTime, endTime);
        var nonCheckPointEvents = events.Events.RemoveCheckPointEvents();

        Assert.That(nonCheckPointEvents.Any(e => e.EventType == EventMessage.GroupDeleted), Is.True);
    }

    [Test]
    public async Task ShallReturnEmptyListWhenNoGroupsExist()
    {
        var allGroups = await GetAllSettingGroups();

        Assert.That(allGroups, Is.Empty);
    }

    [Test]
    public async Task ShallRoundTripExportAndImportGroups()
    {
        var group = CreateTestGroupWithSettings("RoundTripGroup", "Round trip test");
        await CreateSettingGroup(group);

        var exported = await ExportSettingGroups();

        await DeleteAllSettingGroups();
        var afterDelete = await GetAllSettingGroups();
        Assert.That(afterDelete, Is.Empty);

        await ImportSettingGroups(exported, ImportType.AddNew);

        var afterImport = await GetAllSettingGroups();
        Assert.That(afterImport.Count, Is.EqualTo(1));
        Assert.That(afterImport[0].Name, Is.EqualTo("RoundTripGroup"));
        Assert.That(afterImport[0].Description, Is.EqualTo("Round trip test"));
        Assert.That(afterImport[0].GroupedSettings.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallPreserveGroupedSettingOrderWhenGroupIsUpdated()
    {
        var group = CreateTestGroupWithSettings("OrderedGroup", "Order test");

        var created = await CreateSettingGroup(group);
        Assert.That(created.GroupedSettings.Select(setting => setting.Name),
            Is.EqualTo(new[] { "ConnectionString", "MaxRetries" }));

        var reorderedGroup = new SettingGroupDataContract(
            created.Id,
            created.Name,
            created.Description,
            new List<GroupedSettingDataContract>
            {
                new(
                    created.GroupedSettings[1].Name,
                    created.GroupedSettings[1].Description,
                    created.GroupedSettings[1].ValueType,
                    created.GroupedSettings[1].SourceSettings),
                new(
                    created.GroupedSettings[0].Name,
                    created.GroupedSettings[0].Description,
                    created.GroupedSettings[0].ValueType,
                    created.GroupedSettings[0].SourceSettings)
            });

        var updated = await UpdateSettingGroup(created.Id!.Value, reorderedGroup);
        Assert.That(updated.GroupedSettings.Select(setting => setting.Name),
            Is.EqualTo(new[] { "MaxRetries", "ConnectionString" }));

        var fetched = await GetSettingGroup(created.Id.Value);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.GroupedSettings.Select(setting => setting.Name),
            Is.EqualTo(new[] { "MaxRetries", "ConnectionString" }));
    }

    [Test]
    public async Task ShallPreserveSourceSettingOrderWhenGroupIsUpdated()
    {
        var group = new SettingGroupDataContract(
            null,
            "OrderedGroup",
            "Order test",
            new List<GroupedSettingDataContract>
            {
                new("Timeout", null, "System.String",
                    new List<SourceSettingDataContract>
                    {
                        new("ServiceA", "Timeout"),
                        new("ServiceB", "Timeout"),
                        new("ServiceC", "Timeout")
                    })
            });

        var created = await CreateSettingGroup(group);
        Assert.That(created.GroupedSettings[0].SourceSettings.Select(s => s.ClientName),
            Is.EqualTo(new[] { "ServiceA", "ServiceB", "ServiceC" }));

        var reorderedGroup = new SettingGroupDataContract(
            created.Id,
            created.Name,
            created.Description,
            new List<GroupedSettingDataContract>
            {
                new(created.GroupedSettings[0].Name, created.GroupedSettings[0].Description, created.GroupedSettings[0].ValueType,
                    new List<SourceSettingDataContract>
                    {
                        new("ServiceC", "Timeout"),
                        new("ServiceA", "Timeout"),
                        new("ServiceB", "Timeout")
                    })
            });

        var updated = await UpdateSettingGroup(created.Id!.Value, reorderedGroup);
        Assert.That(updated.GroupedSettings[0].SourceSettings.Select(s => s.ClientName),
            Is.EqualTo(new[] { "ServiceC", "ServiceA", "ServiceB" }));

        var fetched = await GetSettingGroup(created.Id.Value);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.GroupedSettings[0].SourceSettings.Select(s => s.ClientName),
            Is.EqualTo(new[] { "ServiceC", "ServiceA", "ServiceB" }));
    }

    [Test]
    public async Task ShallFilterGroupSourceSettingsByClientFilter()
    {
        var settings1 = await RegisterSettings<ThreeSettings>();
        var settings2 = await RegisterSettings<ClientXWithTwoSettings>();

        var group = new SettingGroupDataContract(
            null,
            "MultiClientGroup",
            null,
            new List<GroupedSettingDataContract>
            {
                new("AStringSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings1.ClientName, "AStringSetting"),
                        new(settings2.ClientName, "AStringSetting")
                    })
            });

        var created = await CreateSettingGroup(group);

        var restrictedUser = NewUser(role: Role.User, clientFilter: settings1.ClientName);
        await CreateUser(restrictedUser);
        var loginResult = await Login(restrictedUser.Username, restrictedUser.Password!);

        var groups = await GetAllSettingGroups(loginResult.Token);

        Assert.That(groups.Count, Is.EqualTo(1));
        var returnedGroup = groups[0];
        Assert.That(returnedGroup.Name, Is.EqualTo("MultiClientGroup"));
        Assert.That(returnedGroup.GroupedSettings[0].SourceSettings.Count, Is.EqualTo(1));
        Assert.That(returnedGroup.GroupedSettings[0].SourceSettings[0].ClientName, Is.EqualTo(settings1.ClientName));
    }

    [Test]
    public async Task ShallHideGroupWhenAllSourceSettingsAreInaccessibleDueToClientFilter()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var group = new SettingGroupDataContract(
            null,
            "HiddenGroup",
            null,
            new List<GroupedSettingDataContract>
            {
                new("AStringSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings.ClientName, "AStringSetting")
                    })
            });

        var created = await CreateSettingGroup(group);

        var restrictedUser = NewUser(role: Role.User, clientFilter: "NoMatchingClient");
        await CreateUser(restrictedUser);
        var loginResult = await Login(restrictedUser.Username, restrictedUser.Password!);

        var groups = await GetAllSettingGroups(loginResult.Token);
        Assert.That(groups, Is.Empty);
    }

    [Test]
    public async Task ShallReturnNotFoundForGroupWithNoAccessibleSettingsWhenFetchedById()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var group = new SettingGroupDataContract(
            null,
            "HiddenGroupById",
            null,
            new List<GroupedSettingDataContract>
            {
                new("AStringSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings.ClientName, "AStringSetting")
                    })
            });

        var created = await CreateSettingGroup(group);

        var restrictedUser = NewUser(role: Role.User, clientFilter: "NoMatchingClient");
        await CreateUser(restrictedUser);
        var loginResult = await Login(restrictedUser.Username, restrictedUser.Password!);

        await ApiClient.GetAndVerify($"/settinggroups/{created.Id}", HttpStatusCode.NotFound, tokenOverride: loginResult.Token);
    }

    [Test]
    public async Task ShallFilterGroupSourceSettingsByClassification()
    {
        var settings = await RegisterSettings<ClassifiedSettings>();

        var group = new SettingGroupDataContract(
            null,
            "ClassifiedGroup",
            null,
            new List<GroupedSettingDataContract>
            {
                new("TechnicalSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings.ClientName, "TechnicalSetting")
                    }),
                new("FunctionalSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings.ClientName, "FunctionalSetting")
                    })
            });

        await CreateSettingGroup(group);

        var restrictedUser = NewUser(
            role: Role.User,
            allowedClassifications: new List<Classification> { Classification.Technical });
        await CreateUser(restrictedUser);
        var loginResult = await Login(restrictedUser.Username, restrictedUser.Password!);

        var groups = await GetAllSettingGroups(loginResult.Token);

        Assert.That(groups.Count, Is.EqualTo(1));
        Assert.That(groups[0].GroupedSettings.Count, Is.EqualTo(1));
        Assert.That(groups[0].GroupedSettings[0].Name, Is.EqualTo("TechnicalSetting"));
    }

    [Test]
    public async Task ShallHideGroupWhenAllSettingsHaveInaccessibleClassification()
    {
        var settings = await RegisterSettings<ClassifiedSettings>();

        var group = new SettingGroupDataContract(
            null,
            "FullyHiddenGroup",
            null,
            new List<GroupedSettingDataContract>
            {
                new("FunctionalSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings.ClientName, "FunctionalSetting")
                    }),
                new("SpecialSetting", null, "String",
                    new List<SourceSettingDataContract>
                    {
                        new(settings.ClientName, "SpecialSetting")
                    })
            });

        await CreateSettingGroup(group);

        var restrictedUser = NewUser(
            role: Role.User,
            allowedClassifications: new List<Classification> { Classification.Technical });
        await CreateUser(restrictedUser);
        var loginResult = await Login(restrictedUser.Username, restrictedUser.Password!);

        var groups = await GetAllSettingGroups(loginResult.Token);
        Assert.That(groups, Is.Empty);
    }

    [Test]
    public async Task ShallRemoveOrphanedSourceSettingFromGroupWhenClientDefinitionChanges()
    {
        var clientASecret = GetNewSecret();
        var clientBSecret = GetNewSecret();

        await RegisterSettings<GroupedRegistrationClientAOriginal>(clientASecret);
        await RegisterSettings<GroupedRegistrationClientB>(clientBSecret);

        var initialGroup = (await GetAllSettingGroups()).Single(group => group.Name == "Shared Group");
        Assert.That(initialGroup.GroupedSettings.Count, Is.EqualTo(1));
        Assert.That(initialGroup.GroupedSettings[0].SourceSettings.Select(source => source.ClientName),
            Is.EqualTo(new[] { "GroupedRegistrationClientA", "GroupedRegistrationClientB" }));

        await RegisterSettings<GroupedRegistrationClientAUpdated>(clientASecret);

        var updatedGroup = (await GetAllSettingGroups()).Single(group => group.Name == "Shared Group");
        Assert.That(updatedGroup.GroupedSettings.Count, Is.EqualTo(1));
        Assert.That(updatedGroup.GroupedSettings[0].SourceSettings.Select(source => source.ClientName),
            Is.EqualTo(new[] { "GroupedRegistrationClientB" }));
        Assert.That(updatedGroup.GroupedSettings[0].SourceSettings.Single().SettingName, Is.EqualTo("SharedSetting"));
    }
}

public class GroupedRegistrationClientAOriginal : TestSettingsBase
{
    public override string ClientName => "GroupedRegistrationClientA";
    public override string ClientDescription => "Client A with grouped source setting";

    [Setting("Shared setting")]
    [Group("Shared Group")]
    public string SharedSetting { get; set; } = "A";

    [Setting("Local setting")]
    public string LocalSetting { get; set; } = "Local";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class GroupedRegistrationClientAUpdated : TestSettingsBase
{
    public override string ClientName => "GroupedRegistrationClientA";
    public override string ClientDescription => "Client A after removing grouped source setting";

    [Setting("Local setting")]
    public string LocalSetting { get; set; } = "Local";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class GroupedRegistrationClientB : TestSettingsBase
{
    public override string ClientName => "GroupedRegistrationClientB";
    public override string ClientDescription => "Client B with grouped source setting";

    [Setting("Shared setting")]
    [Group("Shared Group")]
    public string SharedSetting { get; set; } = "B";

    [Setting("Other setting")]
    public string LocalSetting { get; set; } = "Local";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
