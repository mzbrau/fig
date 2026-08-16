using Fig.Contracts.ImportExport;
using Fig.Web.Models.ImportExport;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class ImportResultStatusTests
{
    [Test]
    public void DescribeGroupImport_NullResult_IsFailureWithoutMarkingSuccess()
    {
        var (message, succeeded) = ImportResultStatus.DescribeGroupImport(null);

        Assert.That(succeeded, Is.False);
        Assert.That(message, Does.Contain("see notification"));
        Assert.That(message, Does.Not.Contain("successfully"));
    }

    [Test]
    public void DescribeGroupImport_ErrorMessage_IsFailure()
    {
        var (message, succeeded) = ImportResultStatus.DescribeGroupImport(new ImportResultDataContract
        {
            ErrorMessage = "boom"
        });

        Assert.That(succeeded, Is.False);
        Assert.That(message, Is.EqualTo("Import failed: boom"));
    }

    [Test]
    public void DescribeGroupImport_Success_AllowsMarkGroupsChanged()
    {
        var (message, succeeded) = ImportResultStatus.DescribeGroupImport(new ImportResultDataContract());

        Assert.That(succeeded, Is.True);
        Assert.That(message, Is.EqualTo("Import completed successfully."));
    }

    [Test]
    public void DescribeSettingsImportHttpFailure_MentionsNotification()
    {
        Assert.That(ImportResultStatus.DescribeSettingsImportHttpFailure(), Does.Contain("see notification"));
    }
}
