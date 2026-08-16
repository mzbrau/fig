using System;
using System.IO;
using Fig.Api.Utils;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class TempFileTests
{
    [Test]
    public void Constructor_WritesDataToTempPath()
    {
        var fileName = $"fig-temp-{Guid.NewGuid():N}.bin";
        var data = new byte[] { 1, 2, 3, 4 };

        using var tempFile = new TempFile(data, fileName);

        Assert.That(File.Exists(tempFile.FilePath), Is.True);
        Assert.That(File.ReadAllBytes(tempFile.FilePath), Is.EqualTo(data));
        Assert.That(Path.GetFileName(tempFile.FilePath), Is.EqualTo(fileName));
    }

    [Test]
    public void Dispose_DeletesFileAndInvalidatesPath()
    {
        var fileName = $"fig-temp-{Guid.NewGuid():N}.bin";
        var tempFile = new TempFile([9, 8, 7], fileName);
        var path = tempFile.FilePath;

        tempFile.Dispose();

        Assert.That(File.Exists(path), Is.False);
        Assert.Throws<ObjectDisposedException>(() => _ = tempFile.FilePath);
    }

    [Test]
    public void Constructor_Throws_WhenDataIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new TempFile(null!, "file.bin"));
    }

    [Test]
    public void Constructor_Throws_WhenFileNameEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => new TempFile([1], string.Empty));
    }
}
