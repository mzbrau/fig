using Fig.Contracts.SettingClients;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Fig.Integration.Test.Api;

public class ClientDescriptionsTests : IntegrationTestBase
{
    [Test]
    public async Task GetClientDescriptions_ShouldReturnClientNamesAndDescriptions()
    {
        // Arrange
        await RegisterSettings<AllSettingsAndTypes>();

        // Act
        var result = await ApiClient.Get<ClientsDescriptionDataContract>("/clients/descriptions");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Clients, Is.Not.Null);
        Assert.That(result.Clients.Count(), Is.GreaterThan(0));
        
        var firstClient = result.Clients.First();
        Assert.That(firstClient.Name, Is.Not.Null.And.Not.Empty);
        Assert.That(firstClient.Description, Is.Not.Null);
    }

    [Test]
    public async Task GetClientDescriptions_ShouldReturnCorrectClientData()
    {
        // Arrange
        var settings = await RegisterSettings<AllSettingsAndTypes>();

        // Act
        var descriptions = await ApiClient.Get<ClientsDescriptionDataContract>("/clients/descriptions");
        Assert.That(descriptions!.Clients.Single().Name, Is.EqualTo(settings.ClientName));
        Assert.That(descriptions!.Clients.Single().Description, Is.EqualTo(settings.ClientDescription));
    }
}
