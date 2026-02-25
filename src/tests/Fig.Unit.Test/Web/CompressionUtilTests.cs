using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Fig.Web.Utils;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class CompressionUtilTests
{
    [Test]
    public void CompressToZip_MultipleEntries_ShouldIncludeAllEntries()
    {
        var entries = new Dictionary<string, string>
        {
            ["ClientA.json"] = "{\"name\":\"A\"}",
            ["ClientB.json"] = "{\"name\":\"B\"}"
        };

        var zipBytes = CompressionUtil.CompressToZip(entries);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.That(archive.Entries.Select(e => e.FullName), Is.EquivalentTo(entries.Keys));
    }

    [Test]
    public void CompressToZip_MultipleEntries_EntriesShouldContainProvidedContent()
    {
        var entries = new Dictionary<string, string>
        {
            ["ClientA.json"] = "{\"name\":\"A\"}",
            ["ClientB.json"] = "{\"name\":\"B\"}"
        };

        var zipBytes = CompressionUtil.CompressToZip(entries);

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var expected in entries)
        {
            var entry = archive.GetEntry(expected.Key);
            Assert.That(entry, Is.Not.Null);
            using var reader = new StreamReader(entry!.Open(), Encoding.UTF8);
            var content = reader.ReadToEnd();
            Assert.That(content, Is.EqualTo(expected.Value));
        }
    }

    [Test]
    public void CountJsonEntriesInZip_ShouldReturnJsonEntryCount()
    {
        var entries = new Dictionary<string, string>
        {
            ["ClientA.json"] = "{\"name\":\"A\"}",
            ["ClientB.json"] = "{\"name\":\"B\"}"
        };

        var zipBytes = CompressionUtil.CompressToZip(entries);
        var count = CompressionUtil.CountJsonEntriesInZip(zipBytes);

        Assert.That(count, Is.EqualTo(2));
    }
}
