using System.IO;
using Fig.Api.DataImport;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ImportFolderPathResolverTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void TryValidate_WhenPathMissing_ReturnsFalse(string? path)
    {
        Assert.That(ImportFolderPathResolver.TryValidate(path, out var resolved), Is.False);
        Assert.That(resolved, Is.Empty);
    }

    [Test]
    public void TryValidate_WhenRelativePath_ReturnsFalse()
    {
        Assert.That(ImportFolderPathResolver.TryValidate("imports/relative", out var resolved), Is.False);
        Assert.That(resolved, Is.Empty);
    }

    [Test]
    public void TryValidate_WhenAbsolutePath_ReturnsFullPath()
    {
        var path = Path.Combine(Path.GetTempPath(), "fig-import-validate-" + Guid.NewGuid().ToString("N"));

        Assert.That(ImportFolderPathResolver.TryValidate(path, out var resolved), Is.True);
        Assert.That(resolved, Is.EqualTo(Path.GetFullPath(path)));
        Assert.That(Directory.Exists(resolved), Is.False);
    }

    [Test]
    public void TryValidate_ExpandsEnvironmentVariables()
    {
        var variable = "FIG_IMPORT_TEST_" + Guid.NewGuid().ToString("N");
        var folder = Path.Combine(Path.GetTempPath(), "fig-import-env-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(variable, folder);
        try
        {
            var configured = Path.Combine($"%{variable}%", "nested");
            Assert.That(ImportFolderPathResolver.TryValidate(configured, out var resolved), Is.True);
            Assert.That(resolved, Is.EqualTo(Path.GetFullPath(Path.Combine(folder, "nested"))));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Test]
    public void TryResolve_CreatesDirectoryWhenMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), "fig-import-resolve-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.That(Directory.Exists(path), Is.False);
            Assert.That(ImportFolderPathResolver.TryResolve(path, out var resolved), Is.True);
            Assert.That(resolved, Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(Directory.Exists(resolved), Is.True);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public void TryResolve_WhenPathInvalid_ReturnsFalse()
    {
        Assert.That(ImportFolderPathResolver.TryResolve("relative-only", out var resolved), Is.False);
        Assert.That(resolved, Is.Empty);
    }
}
