using System.Linq;
using System.Threading.Tasks;
using Fig.Integration.Test.Api.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class VerificationUpdateTests : IntegrationTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
    }
    
    [Test]
    public async Task ShallAddPluginVerificationIfAddedAfterInitialRegistration()
    {
        await RegisterSettings<ClientA>();
        await RegisterSettings<ClientAWithPluginVerification>();

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(1), "The plugin verification should have been added");
        Assert.That(clients.Single().PluginVerifications.Single().Name, Is.EqualTo("Rest200OkVerifier"));
    }

    [Test]
    public async Task ShallAddDynamicVerificationIfAddedAfterInitialRegistration()
    {
        await RegisterSettings<ClientA>();
        await RegisterSettings<ClientAWithDynamicVerification>();

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(1), "The dynamic verification should have been added");
        Assert.That(clients.Single().DynamicVerifications.Single().Name, Is.EqualTo("WebsiteVerifier"));
    }

    [Test]
    public async Task ShallRemovePluginVerificationIfRemovedAfterInitialRegistration()
    {
        await RegisterSettings<ClientAWithPluginVerification>();
        await RegisterSettings<ClientA>();

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(0), "The plugin verification should have been removed");
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task ShallRemoveDynamicVerificationIfRemovedAfterInitialRegistration()
    {
        await RegisterSettings<ClientAWithDynamicVerification>();
        await RegisterSettings<ClientA>();

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(0), "The dynamic verification should have been removed");
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallUpdatePluginVerification()
    {
        await RegisterSettings<ClientAWithPluginVerification>();
        await RegisterSettings<ClientAWithPluginVerification2>();

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(1), "The plugin verification should have been replaced");
        Assert.That(clients.Single().PluginVerifications.Single().Name, Is.EqualTo("PingVerifier"));
    }

    [Test]
    public async Task ShallUpdateDynamicVerification()
    {
        await RegisterSettings<ClientAWithDynamicVerification>();
        await RegisterSettings<ClientAWithDynamicVerification2>();

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(1), "The dynamic verification should have been replaced");
        Assert.That(clients.Single().DynamicVerifications.Single().Name, Is.EqualTo("WebsiteVerifier"));
        Assert.That(clients.Single().DynamicVerifications.Single().Description, Is.EqualTo("VerifiesWebsites v2"));
    }
}