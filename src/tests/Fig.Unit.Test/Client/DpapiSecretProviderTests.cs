using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Fig.Client.Contracts;
using Fig.Client.SecretProvider.Dpapi;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class DpapiSecretProviderTests
{
    [Test]
    public async Task GetSecret_WhenUserScopedSecretExists_ReusesPersistedSecret()
    {
        var userScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::existing-secret"
        };

        var provider = new TestDpapiSecretProvider(true, userScopedSecrets);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("existing-secret"));
        Assert.That(provider.ProtectCalls, Is.EqualTo(0));
        Assert.That(provider.WriteCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSecret_WhenSecretMissing_PersistsItForLaterRuns()
    {
        var userScopedSecrets = new Dictionary<string, string>();
        var firstProvider = new TestDpapiSecretProvider(true, userScopedSecrets);

        var firstSecret = await firstProvider.GetSecret("Test Client");

        var secondProvider = new TestDpapiSecretProvider(true, userScopedSecrets);
        var secondSecret = await secondProvider.GetSecret("Test Client");

        Assert.That(userScopedSecrets, Contains.Key("FIG_TESTCLIENT_SECRET"));
        Assert.That(secondSecret, Is.EqualTo(firstSecret));
        Assert.That(firstProvider.WriteCalls, Is.EqualTo(1));
        Assert.That(secondProvider.ProtectCalls, Is.EqualTo(0));
        Assert.That(secondProvider.WriteCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSecret_WhenBothUserAndMachineSecretsExist_PrefersUserScopedSecret()
    {
        var userScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::user-secret"
        };
        var machineScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::machine-secret"
        };
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets, machineScopedSecrets);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("user-secret"));
        Assert.That(provider.ProtectCalls, Is.EqualTo(0));
        Assert.That(provider.WriteCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSecret_WhenOnlyMachineScopedSecretExists_UsesMachineScopedSecret()
    {
        var userScopedSecrets = new Dictionary<string, string>();
        var machineScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::machine-secret"
        };
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets, machineScopedSecrets);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("machine-secret"));
        Assert.That(userScopedSecrets, Contains.Key("FIG_TESTCLIENT_SECRET"));
        Assert.That(userScopedSecrets["FIG_TESTCLIENT_SECRET"], Is.EqualTo("protected::machine-secret"));
        Assert.That(provider.ProtectCalls, Is.EqualTo(0));
        Assert.That(provider.WriteCalls, Is.EqualTo(1));
    }

    [Test]
    public void GetSecret_WhenSecretMissingAndAutoCreateDisabled_Throws()
    {
        var provider = new TestDpapiSecretProvider(false, new Dictionary<string, string>());

        var ex = Assert.ThrowsAsync<SecretNotFoundException>(async () => await provider.GetSecret("Test Client"));

        Assert.That(ex!.Message, Does.Contain("user or machine environment variable"));
    }

    private sealed class TestDpapiSecretProvider : DpapiSecretProvider
    {
        private readonly IDictionary<string, string> _userScopedSecrets;
        private readonly IDictionary<string, string> _machineScopedSecrets;

        public TestDpapiSecretProvider(
            bool autoCreate,
            IDictionary<string, string> userScopedSecrets,
            IDictionary<string, string>? machineScopedSecrets = null)
            : base(autoCreate)
        {
            _userScopedSecrets = userScopedSecrets;
            _machineScopedSecrets = machineScopedSecrets ?? new Dictionary<string, string>();
        }

        public int ProtectCalls { get; private set; }
        public int WriteCalls { get; private set; }

        protected override string? GetStoredEncryptedSecret(string secretKey)
        {
            return _userScopedSecrets.TryGetValue(secretKey, out var value) ? value : null;
        }

        protected override string? GetCurrentProcessEncryptedSecret(string secretKey)
        {
            return _machineScopedSecrets.TryGetValue(secretKey, out var value) ? value : null;
        }

        protected override void SetStoredEncryptedSecret(string secretKey, string encryptedSecret)
        {
            WriteCalls++;
            _userScopedSecrets[secretKey] = encryptedSecret;
        }

        protected override string Unprotect(string encryptedString, DataProtectionScope scope)
        {
            return encryptedString.Replace("protected::", string.Empty);
        }

        protected override string Protect(string plainText, DataProtectionScope scope)
        {
            ProtectCalls++;
            return $"protected::{plainText}";
        }
    }
}
