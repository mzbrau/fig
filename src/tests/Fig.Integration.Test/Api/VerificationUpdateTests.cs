using System.Linq;
using System.Threading.Tasks;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class VerificationUpdateTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddPluginVerificationIfAddedAfterInitialRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);
        await RegisterSettings<ClientAWithPluginVerification>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(1), "The plugin verification should have been added");
        Assert.That(clients.Single().PluginVerifications.Single().Name, Is.EqualTo("Rest200OkVerifier"));
    }

    [Test]
    public async Task ShallAddDynamicVerificationIfAddedAfterInitialRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);
        await RegisterSettings<ClientAWithDynamicVerification>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(1), "The dynamic verification should have been added");
        Assert.That(clients.Single().DynamicVerifications.Single().Name, Is.EqualTo("WebsiteVerifier"));
    }

    [Test]
    public async Task ShallRemovePluginVerificationIfRemovedAfterInitialRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientAWithPluginVerification>(secret);
        await RegisterSettings<ClientA>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(0), "The plugin verification should have been removed");
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task ShallRemoveDynamicVerificationIfRemovedAfterInitialRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientAWithDynamicVerification>(secret);
        await RegisterSettings<ClientA>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(0), "The dynamic verification should have been removed");
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallUpdatePluginVerification()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientAWithPluginVerification>(secret);
        await RegisterSettings<ClientAWithPluginVerification2>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().PluginVerifications.Count, Is.EqualTo(1), "The plugin verification should have been replaced");
        Assert.That(clients.Single().PluginVerifications.Single().Name, Is.EqualTo("PingVerifier"));
    }

    [Test]
    public async Task ShallUpdateDynamicVerification()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientAWithDynamicVerification>(secret);
        await RegisterSettings<ClientAWithDynamicVerification2>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().DynamicVerifications.Count, Is.EqualTo(1), "The dynamic verification should have been replaced");
        Assert.That(clients.Single().DynamicVerifications.Single().Name, Is.EqualTo("WebsiteVerifier"));
        Assert.That(clients.Single().DynamicVerifications.Single().Description, Is.EqualTo("VerifiesWebsites v2"));
    }
}