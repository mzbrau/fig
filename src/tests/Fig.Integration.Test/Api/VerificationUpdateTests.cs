using System.Linq;
using System.Threading.Tasks;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class VerificationUpdateTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddVerificationIfAddedAfterInitialRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientA>(secret);
        await RegisterSettings<ClientAWithVerification>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().Verifications.Count, Is.EqualTo(1), "The verification should have been added");
        Assert.That(clients.Single().Verifications.Single().Name, Is.EqualTo("Rest200OkVerifier"));
    }

    [Test]
    public async Task ShallRemoveVerificationIfRemovedAfterInitialRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientAWithVerification>(secret);
        await RegisterSettings<ClientA>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().Verifications.Count, Is.EqualTo(0), "The verification should have been removed");
    }

    [Test]
    public async Task ShallUpdateVerification()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientAWithVerification>(secret);
        await RegisterSettings<ClientAWithVerification2>(secret);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Single().Verifications.Count, Is.EqualTo(1), "The verification should have been replaced");
        Assert.That(clients.Single().Verifications.Single().Name, Is.EqualTo("PingVerifier"));
    }
}