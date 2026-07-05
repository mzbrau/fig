using System;
using System.IO;
using Fig.Client.RegistrationChecksum;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class RegistrationChecksumStoreTests
{
    private readonly RegistrationChecksumStore _store = new();
    private readonly List<string> _pathsToDelete = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var path in _pathsToDelete)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public void GetFilePath_WithoutInstance_UsesClientNameChecksumExtension()
    {
        var clientName = $"ChecksumTest_{Guid.NewGuid():N}";
        _pathsToDelete.Add(_store.GetFilePath(clientName, null));

        Assert.That(_store.GetFilePath(clientName, null), Does.EndWith($"{Path.DirectorySeparatorChar}{clientName}.checksum"));
    }

    [Test]
    public void GetFilePath_WithInstance_IncludesInstanceInFileName()
    {
        var clientName = $"ChecksumTest_{Guid.NewGuid():N}";
        _pathsToDelete.Add(_store.GetFilePath(clientName, "Production"));

        Assert.That(_store.GetFilePath(clientName, "Production"), Does.EndWith($"{Path.DirectorySeparatorChar}{clientName.Length}_{clientName}_Production.checksum"));
    }

    [Test]
    public void GetFilePath_SanitizesClientName()
    {
        var clientName = $"My Client {Guid.NewGuid():N}";
        var expectedFileName = clientName.Replace(" ", string.Empty) + ".checksum";
        _pathsToDelete.Add(_store.GetFilePath(clientName, null));

        Assert.That(_store.GetFilePath(clientName, null), Does.EndWith($"{Path.DirectorySeparatorChar}{expectedFileName}"));
    }

    [Test]
    public void SaveAndGet_RoundTripsChecksum()
    {
        var clientName = $"ChecksumTest_{Guid.NewGuid():N}";
        _pathsToDelete.Add(_store.GetFilePath(clientName, null));

        _store.Save(clientName, null, "abc123");
        var stored = _store.Get(clientName, null);

        Assert.That(stored, Is.EqualTo("abc123"));
    }

    [Test]
    public void Delete_RemovesStoredChecksum()
    {
        var clientName = $"ChecksumTest_{Guid.NewGuid():N}";
        var path = _store.GetFilePath(clientName, null);
        _pathsToDelete.Add(path);
        _store.Save(clientName, null, "abc123");

        _store.Delete(clientName, null);

        Assert.That(_store.Get(clientName, null), Is.Null);
        Assert.That(File.Exists(path), Is.False);
    }
}
