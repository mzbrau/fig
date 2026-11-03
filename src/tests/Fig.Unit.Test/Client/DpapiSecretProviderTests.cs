using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Client.Contracts;
using Fig.Client.SecretProvider.Dpapi;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class DpapiSecretProviderTests
{
    [Test]
    public async Task GetSecret_WhenMachineScopedSecretExists_ReusesPersistedSecret()
    {
        var machineScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::existing-secret"
        };

        var provider = new TestDpapiSecretProvider(true, new Dictionary<string, string>(), machineScopedSecrets);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("existing-secret"));
        Assert.That(provider.ProtectCalls, Is.EqualTo(0));
        Assert.That(provider.MachineWriteCalls, Is.EqualTo(0));
        Assert.That(provider.UserWriteCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSecret_WhenSecretMissing_PersistsToMachineScope()
    {
        var machineScopedSecrets = new Dictionary<string, string>();
        var firstProvider = new TestDpapiSecretProvider(true, new Dictionary<string, string>(), machineScopedSecrets);

        var firstSecret = await firstProvider.GetSecret("Test Client");

        var secondProvider = new TestDpapiSecretProvider(true, new Dictionary<string, string>(), machineScopedSecrets);
        var secondSecret = await secondProvider.GetSecret("Test Client");

        Assert.That(machineScopedSecrets, Contains.Key("FIG_TESTCLIENT_SECRET"));
        Assert.That(secondSecret, Is.EqualTo(firstSecret));
        Assert.That(firstProvider.MachineWriteCalls, Is.EqualTo(1));
        Assert.That(firstProvider.UserWriteCalls, Is.EqualTo(0));
        Assert.That(firstProvider.ProtectScopes, Is.EqualTo(new[] { DataProtectionScope.LocalMachine }));
        Assert.That(secondProvider.ProtectCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSecret_WhenBothUserAndMachineSecretsExist_PrefersMachineScopedSecret()
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

        Assert.That(secret, Is.EqualTo("machine-secret"));
        Assert.That(provider.ProtectCalls, Is.EqualTo(0));
        Assert.That(provider.MachineWriteCalls, Is.EqualTo(0));
        Assert.That(provider.UserWriteCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSecret_WhenOnlyUserScopedSecretExists_ReprotectsWithLocalMachineAndWarns()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var userScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "userprotected::user-secret"
        };
        var machineScopedSecrets = new Dictionary<string, string>();
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets, machineScopedSecrets);
        provider.AddLogger(loggerFactory.Object);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("user-secret"));
        Assert.That(machineScopedSecrets, Contains.Key("FIG_TESTCLIENT_SECRET"));
        Assert.That(machineScopedSecrets["FIG_TESTCLIENT_SECRET"], Is.EqualTo("protected::user-secret"));
        Assert.That(provider.MachineWriteCalls, Is.EqualTo(1));
        Assert.That(provider.ProtectScopes, Is.EqualTo(new[] { DataProtectionScope.LocalMachine }));
        AssertWarningLogged(logger, "user-scoped environment variable");
    }

    [Test]
    public async Task GetSecret_WhenOnlyUserScopedSecretExistsAndMachineWriteFails_ContinuesWithUserSecret()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var userScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::user-secret"
        };
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets)
        {
            FailMachineWrites = true
        };
        provider.AddLogger(loggerFactory.Object);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("user-secret"));
        Assert.That(provider.MachineWriteCalls, Is.EqualTo(0));
        Assert.That(provider.ProtectScopes, Is.EqualTo(new[] { DataProtectionScope.LocalMachine }));
        AssertWarningLogged(logger, "Unable to persist user-scoped DPAPI secret to machine scope");
    }

    [Test]
    public async Task GetSecret_WhenUnreadableMachineSecretAndValidUserSecret_FallsBackToUserWithoutOverwritingMachine()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var userScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::user-secret"
        };
        var machineScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "unreadable::foreign-account-secret"
        };
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets, machineScopedSecrets);
        provider.AddLogger(loggerFactory.Object);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("user-secret"));
        Assert.That(machineScopedSecrets["FIG_TESTCLIENT_SECRET"], Is.EqualTo("unreadable::foreign-account-secret"));
        Assert.That(provider.MachineWriteCalls, Is.EqualTo(0));
        AssertWarningLogged(logger, "Falling back to user-scoped lookup without modifying the Machine value");
    }

    [Test]
    public async Task GetSecret_WhenMachineReadThrows_FallsBackToUserScopedSecret()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var userScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "protected::user-secret"
        };
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets)
        {
            FailMachineReads = true
        };
        provider.AddLogger(loggerFactory.Object);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("user-secret"));
        AssertWarningLogged(logger, "Failed to read machine-scoped DPAPI secret");
    }

    [Test]
    public async Task GetSecret_WhenSecretMissingAndMachineWriteFails_FallsBackToUserScope()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var userScopedSecrets = new Dictionary<string, string>();
        var provider = new TestDpapiSecretProvider(true, userScopedSecrets)
        {
            FailMachineWrites = true
        };
        provider.AddLogger(loggerFactory.Object);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(userScopedSecrets, Contains.Key("FIG_TESTCLIENT_SECRET"));
        Assert.That(provider.UserWriteCalls, Is.EqualTo(1));
        Assert.That(provider.MachineWriteCalls, Is.EqualTo(0));
        Assert.That(provider.ProtectScopes, Is.EqualTo(new[] { DataProtectionScope.LocalMachine }));
        Assert.That(secret, Is.Not.Null.And.Not.Empty);
        AssertWarningLogged(logger, "Falling back to user scope");
    }

    [Test]
    public async Task GetSecret_WhenSecretMissingAndAutoCreateDisabled_Throws()
    {
        var provider = new TestDpapiSecretProvider(false, new Dictionary<string, string>());

        var ex = await Assert.ThrowsAsync<SecretNotFoundException>(async () => await provider.GetSecret("Test Client"));

        Assert.That(ex!.Message, Does.Contain("machine or user environment variable"));
    }

    [Test]
    public async Task GetSecret_WhenCurrentUserEncryptedSecretExists_FallsBackAndWarns()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var machineScopedSecrets = new Dictionary<string, string>
        {
            ["FIG_TESTCLIENT_SECRET"] = "userprotected::legacy-secret"
        };
        var provider = new TestDpapiSecretProvider(true, new Dictionary<string, string>(), machineScopedSecrets);
        provider.AddLogger(loggerFactory.Object);

        var secret = await provider.GetSecret("Test Client");

        Assert.That(secret, Is.EqualTo("legacy-secret"));
        Assert.That(provider.UnprotectScopes, Is.EqualTo(new[]
        {
            DataProtectionScope.LocalMachine,
            DataProtectionScope.CurrentUser
        }));
        AssertWarningLogged(logger, "CurrentUser scope");
    }

    [Test]
    public void AppSettingsEncryptionProvider_Encrypt_UsesLocalMachineScope()
    {
        var provider = new TestDpapiSecretProvider(true, new Dictionary<string, string>());
        var encryptionProvider = (IAppSettingsEncryptionProvider)provider;

        var encrypted = encryptionProvider.Encrypt("plain-secret");

        Assert.That(encrypted, Is.EqualTo("protected::plain-secret"));
        Assert.That(provider.ProtectScopes, Is.EqualTo(new[] { DataProtectionScope.LocalMachine }));
    }

    [Test]
    public void AppSettingsEncryptionProvider_Decrypt_UsesLocalMachineThenCurrentUserFallback()
    {
        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var provider = new TestDpapiSecretProvider(true, new Dictionary<string, string>());
        provider.AddLogger(loggerFactory.Object);
        var encryptionProvider = (IAppSettingsEncryptionProvider)provider;

        var decrypted = encryptionProvider.Decrypt("userprotected::legacy-secret");

        Assert.That(decrypted, Is.EqualTo("legacy-secret"));
        Assert.That(provider.UnprotectScopes, Is.EqualTo(new[]
        {
            DataProtectionScope.LocalMachine,
            DataProtectionScope.CurrentUser
        }));
        AssertWarningLogged(logger, "CurrentUser scope");
    }

    [Test]
    public void ApiSettings_GetDecryptedSecret_PrefersLocalMachineScope()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("DPAPI is Windows-only");

        var plainText = Guid.NewGuid().ToString("N");
        var encrypted = Convert.ToBase64String(
            ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.LocalMachine));

        var settings = new ApiSettings
        {
            Secret = encrypted,
            SecretsDpapiEncrypted = true,
            DbConnectionString = "unused"
        };

        Assert.That(settings.GetDecryptedSecret(), Is.EqualTo(plainText));
    }

    [Test]
    public void ApiSettings_GetDecryptedSecret_FallsBackToCurrentUserScope()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("DPAPI is Windows-only");

        var plainText = Guid.NewGuid().ToString("N");
        var encrypted = Convert.ToBase64String(
            ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser));

        var settings = new ApiSettings
        {
            Secret = encrypted,
            SecretsDpapiEncrypted = true,
            DbConnectionString = "unused"
        };

        Assert.That(settings.GetDecryptedSecret(), Is.EqualTo(plainText));
    }

    [Test]
    public void UnprotectWithFallback_RealDpapi_CurrentUserCiphertextDecryptsWithoutRequiringScopeMatch()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("DPAPI is Windows-only");

        // Documents Windows CryptUnprotectData behavior: scope is ignored on unprotect, so
        // CurrentUser ciphertext owned by this account decrypts even when LocalMachine is requested.
        // The catch-based migration warning may therefore not fire for same-user CurrentUser blobs.
        var plainText = Guid.NewGuid().ToString("N");
        var encrypted = Convert.ToBase64String(
            ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser));

        var logger = new Mock<ILogger<DpapiSecretProvider>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        var provider = new RealDpapiSecretProvider();
        provider.AddLogger(loggerFactory.Object);
        var encryptionProvider = (IAppSettingsEncryptionProvider)provider;

        var decrypted = encryptionProvider.Decrypt(encrypted);

        Assert.That(decrypted, Is.EqualTo(plainText));
    }

    private static void AssertWarningLogged(Mock<ILogger<DpapiSecretProvider>> logger, string messageFragment)
    {
        Assert.That(
            logger.Invocations.Any(invocation =>
                invocation.Method.Name == nameof(ILogger.Log) &&
                invocation.Arguments.Count > 2 &&
                Equals(invocation.Arguments[0], LogLevel.Warning) &&
                invocation.Arguments[2]?.ToString()?.Contains(messageFragment) == true),
            Is.True,
            $"Expected a warning containing '{messageFragment}'");
    }

    private sealed class RealDpapiSecretProvider : DpapiSecretProvider
    {
        public RealDpapiSecretProvider()
            : base(false)
        {
        }
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

        public int MachineWriteCalls { get; private set; }

        public int UserWriteCalls { get; private set; }

        public bool FailMachineWrites { get; set; }

        public bool FailMachineReads { get; set; }

        public List<DataProtectionScope> ProtectScopes { get; } = new();

        public List<DataProtectionScope> UnprotectScopes { get; } = new();

        protected override string? GetUserScopedEncryptedSecret(string secretKey)
        {
            return _userScopedSecrets.TryGetValue(secretKey, out var value) ? value : null;
        }

        protected override string? GetMachineScopedEncryptedSecret(string secretKey)
        {
            if (FailMachineReads)
                throw new UnauthorizedAccessException("Access denied reading machine environment");

            return _machineScopedSecrets.TryGetValue(secretKey, out var value) ? value : null;
        }

        protected override void SetUserScopedEncryptedSecret(string secretKey, string encryptedSecret)
        {
            UserWriteCalls++;
            _userScopedSecrets[secretKey] = encryptedSecret;
        }

        protected override void SetMachineScopedEncryptedSecret(string secretKey, string encryptedSecret)
        {
            if (FailMachineWrites)
                throw new UnauthorizedAccessException("Access denied");

            MachineWriteCalls++;
            _machineScopedSecrets[secretKey] = encryptedSecret;
        }

        protected override string Unprotect(string encryptedString, DataProtectionScope scope)
        {
            UnprotectScopes.Add(scope);

            if (encryptedString.StartsWith("unreadable::", StringComparison.Ordinal))
                throw new SecretNotFoundException("Ciphertext cannot be decrypted by this account");

            if (encryptedString.StartsWith("userprotected::", StringComparison.Ordinal))
            {
                if (scope == DataProtectionScope.LocalMachine)
                    throw new SecretNotFoundException("Not LocalMachine ciphertext");

                return encryptedString["userprotected::".Length..];
            }

            if (scope == DataProtectionScope.CurrentUser)
                throw new SecretNotFoundException("Not CurrentUser ciphertext");

            return encryptedString.Replace("protected::", string.Empty);
        }

        protected override string Protect(string plainText, DataProtectionScope scope)
        {
            ProtectCalls++;
            ProtectScopes.Add(scope);
            return $"protected::{plainText}";
        }
    }
}
