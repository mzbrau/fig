using Fig.Client.Abstractions.DisplayScripts;
using Fig.Client.Abstractions.Validation;
using Fig.Client.Testing.Scripts;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

public class DisplayScriptLibraryTests
{
    private DisplayScriptTestRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        _runner = new DisplayScriptTestRunner();
    }

    private string Substitute(string libraryScript, string settingName)
    {
        return DisplayScriptPath.SubstitutePlaceholder(libraryScript, settingName)!;
    }

    #region MillisecondsToHumanReadableTime

    [Test]
    public void MillisecondsToHumanReadableTime_Zero_ShowsZeroMilliseconds()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Timeout", 0);
        var script = Substitute(DisplayScriptLibrary.MillisecondsToHumanReadableTime, "Timeout");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Timeout");
        Assert.That(setting!.InformationText, Is.EqualTo("0 milliseconds"));
    }

    [Test]
    public void MillisecondsToHumanReadableTime_LargeValue_ShowsAllComponents()
    {
        // Arrange - 3661001ms = 1 hour 1 minute 1 second 1 ms
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Timeout", 3661001);
        var script = Substitute(DisplayScriptLibrary.MillisecondsToHumanReadableTime, "Timeout");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Timeout");
        Assert.That(setting!.InformationText, Is.EqualTo("1 hour 1 minute 1 second 1 ms"));
    }

    [Test]
    public void MillisecondsToHumanReadableTime_90000_ShowsMinuteAndSeconds()
    {
        // Arrange - 90000ms = 1 minute 30 seconds
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Timeout", 90000);
        var script = Substitute(DisplayScriptLibrary.MillisecondsToHumanReadableTime, "Timeout");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Timeout");
        Assert.That(setting!.InformationText, Is.EqualTo("1 minute 30 seconds"));
    }

    [Test]
    public void MillisecondsToHumanReadableTime_Negative_SetsNull()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Timeout", -1);
        var script = Substitute(DisplayScriptLibrary.MillisecondsToHumanReadableTime, "Timeout");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Timeout");
        Assert.That(setting!.InformationText, Is.Null);
    }

    #endregion

    #region SecondsToHumanReadableTime

    [Test]
    public void SecondsToHumanReadableTime_Zero_ShowsZeroSeconds()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Interval", 0);
        var script = Substitute(DisplayScriptLibrary.SecondsToHumanReadableTime, "Interval");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Interval");
        Assert.That(setting!.InformationText, Is.EqualTo("0 seconds"));
    }

    [Test]
    public void SecondsToHumanReadableTime_3661_ShowsHourMinuteSecond()
    {
        // Arrange - 3661s = 1 hour 1 minute 1 second
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Interval", 3661);
        var script = Substitute(DisplayScriptLibrary.SecondsToHumanReadableTime, "Interval");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Interval");
        Assert.That(setting!.InformationText, Is.EqualTo("1 hour 1 minute 1 second"));
    }

    [Test]
    public void SecondsToHumanReadableTime_90_ShowsMinuteAndSeconds()
    {
        // Arrange - 90s = 1 minute 30 seconds
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Interval", 90);
        var script = Substitute(DisplayScriptLibrary.SecondsToHumanReadableTime, "Interval");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Interval");
        Assert.That(setting!.InformationText, Is.EqualTo("1 minute 30 seconds"));
    }

    [Test]
    public void SecondsToHumanReadableTime_Negative_SetsNull()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Interval", -5);
        var script = Substitute(DisplayScriptLibrary.SecondsToHumanReadableTime, "Interval");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Interval");
        Assert.That(setting!.InformationText, Is.Null);
    }

    #endregion

    #region ValidateIpAddress

    [Test]
    [TestCase("192.168.1.1")]
    [TestCase("0.0.0.0")]
    [TestCase("255.255.255.255")]
    public void ValidateIpAddress_ValidAddress_IsValid(string ipAddress)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("IpAddress", ipAddress);
        var script = Substitute(DisplayScriptLibrary.ValidateIpAddress, "IpAddress");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("IpAddress");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    [TestCase("256.1.1.1")]
    [TestCase("1.2.3")]
    [TestCase("abc.def.ghi.jkl")]
    [TestCase("")]
    public void ValidateIpAddress_InvalidAddress_IsNotValid(string ipAddress)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("IpAddress", ipAddress);
        var script = Substitute(DisplayScriptLibrary.ValidateIpAddress, "IpAddress");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("IpAddress");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region ValidatePort

    [Test]
    [TestCase(80)]
    [TestCase(1)]
    [TestCase(65535)]
    public void ValidatePort_ValidPort_IsValid(int port)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Port", port);
        var script = Substitute(DisplayScriptLibrary.ValidatePort, "Port");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Port");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    [TestCase(0)]
    [TestCase(65536)]
    [TestCase(-1)]
    public void ValidatePort_InvalidPort_IsNotValid(int port)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("Port", port);
        var script = Substitute(DisplayScriptLibrary.ValidatePort, "Port");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Port");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region ValidateWindowsFilename

    [Test]
    public void ValidateWindowsFilename_ValidFilename_IsValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "myfile.txt");
        var script = Substitute(DisplayScriptLibrary.ValidateWindowsFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    public void ValidateWindowsFilename_IllegalChars_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "file<name");
        var script = Substitute(DisplayScriptLibrary.ValidateWindowsFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("illegal characters"));
    }

    [Test]
    public void ValidateWindowsFilename_ReservedName_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "CON");
        var script = Substitute(DisplayScriptLibrary.ValidateWindowsFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("reserved"));
    }

    [Test]
    public void ValidateWindowsFilename_TrailingDot_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "file.");
        var script = Substitute(DisplayScriptLibrary.ValidateWindowsFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("dot or space"));
    }

    [Test]
    public void ValidateWindowsFilename_Empty_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "");
        var script = Substitute(DisplayScriptLibrary.ValidateWindowsFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
    }

    #endregion

    #region ValidateLinuxFilename

    [Test]
    public void ValidateLinuxFilename_ValidFilename_IsValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "myfile.txt");
        var script = Substitute(DisplayScriptLibrary.ValidateLinuxFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    public void ValidateLinuxFilename_ForwardSlash_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "path/file");
        var script = Substitute(DisplayScriptLibrary.ValidateLinuxFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("forward slash"));
    }

    [Test]
    public void ValidateLinuxFilename_Empty_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", "");
        var script = Substitute(DisplayScriptLibrary.ValidateLinuxFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
    }

    [Test]
    public void ValidateLinuxFilename_ExceedsMaxLength_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Filename", new string('a', 256));
        var script = Substitute(DisplayScriptLibrary.ValidateLinuxFilename, "Filename");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Filename");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("255"));
    }

    #endregion

    #region ValidateUrl

    [Test]
    [TestCase("https://example.com")]
    [TestCase("http://localhost:8080/path")]
    public void ValidateUrl_ValidUrl_IsValid(string url)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Url", url);
        var script = Substitute(DisplayScriptLibrary.ValidateUrl, "Url");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Url");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    [TestCase("ftp://example.com")]
    [TestCase("not a url")]
    [TestCase("")]
    public void ValidateUrl_InvalidUrl_IsNotValid(string url)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Url", url);
        var script = Substitute(DisplayScriptLibrary.ValidateUrl, "Url");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Url");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region ValidateEmail

    [Test]
    [TestCase("user@example.com")]
    [TestCase("user@sub.example.com")]
    public void ValidateEmail_ValidEmail_IsValid(string email)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Email", email);
        var script = Substitute(DisplayScriptLibrary.ValidateEmail, "Email");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Email");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    [TestCase("notanemail")]
    [TestCase("")]
    public void ValidateEmail_InvalidEmail_IsNotValid(string email)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Email", email);
        var script = Substitute(DisplayScriptLibrary.ValidateEmail, "Email");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Email");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region ValidateJson

    [Test]
    [TestCase("{\"key\": \"value\"}")]
    [TestCase("[1,2,3]")]
    public void ValidateJson_ValidJson_IsValid(string json)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("JsonConfig", json);
        var script = Substitute(DisplayScriptLibrary.ValidateJson, "JsonConfig");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("JsonConfig");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    [TestCase("not json")]
    [TestCase("")]
    public void ValidateJson_InvalidJson_IsNotValid(string json)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("JsonConfig", json);
        var script = Substitute(DisplayScriptLibrary.ValidateJson, "JsonConfig");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("JsonConfig");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region BytesToHumanReadableSize

    [Test]
    public void BytesToHumanReadableSize_Zero_ShowsZeroBytes()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("FileSize", 0);
        var script = Substitute(DisplayScriptLibrary.BytesToHumanReadableSize, "FileSize");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("FileSize");
        Assert.That(setting!.InformationText, Is.EqualTo("0 bytes"));
    }

    [Test]
    public void BytesToHumanReadableSize_One_ShowsSingleByte()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("FileSize", 1);
        var script = Substitute(DisplayScriptLibrary.BytesToHumanReadableSize, "FileSize");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("FileSize");
        Assert.That(setting!.InformationText, Is.EqualTo("= 1 byte"));
    }

    [Test]
    public void BytesToHumanReadableSize_1024_Shows1KB()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("FileSize", 1024);
        var script = Substitute(DisplayScriptLibrary.BytesToHumanReadableSize, "FileSize");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("FileSize");
        Assert.That(setting!.InformationText, Is.EqualTo("= 1 KB"));
    }

    [Test]
    public void BytesToHumanReadableSize_1536_ShowsApprox1Point5KB()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("FileSize", 1536);
        var script = Substitute(DisplayScriptLibrary.BytesToHumanReadableSize, "FileSize");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("FileSize");
        Assert.That(setting!.InformationText, Is.EqualTo("= 1.5 KB"));
    }

    [Test]
    public void BytesToHumanReadableSize_1GB_Shows1GB()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("FileSize", 1073741824);
        var script = Substitute(DisplayScriptLibrary.BytesToHumanReadableSize, "FileSize");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("FileSize");
        Assert.That(setting!.InformationText, Is.EqualTo("= 1 GB"));
    }

    [Test]
    public void BytesToHumanReadableSize_Negative_SetsNull()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddIntegerSetting("FileSize", -100);
        var script = Substitute(DisplayScriptLibrary.BytesToHumanReadableSize, "FileSize");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("FileSize");
        Assert.That(setting!.InformationText, Is.Null);
    }

    #endregion

    #region ValidateHostname

    [Test]
    [TestCase("example.com")]
    [TestCase("localhost")]
    [TestCase("sub.domain.example.com")]
    public void ValidateHostname_ValidHostname_IsValid(string hostname)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Hostname", hostname);
        var script = Substitute(DisplayScriptLibrary.ValidateHostname, "Hostname");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Hostname");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    [TestCase("-invalid.com")]
    [TestCase("")]
    public void ValidateHostname_InvalidHostname_IsNotValid(string hostname)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Hostname", hostname);
        var script = Substitute(DisplayScriptLibrary.ValidateHostname, "Hostname");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Hostname");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region ValidateCidr

    [Test]
    [TestCase("192.168.1.0/24")]
    [TestCase("10.0.0.0/8")]
    public void ValidateCidr_ValidCidr_IsValid(string cidr)
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Cidr", cidr);
        var script = Substitute(DisplayScriptLibrary.ValidateCidr, "Cidr");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Cidr");
        Assert.That(setting!.IsValid, Is.True);
    }

    [Test]
    public void ValidateCidr_NoPrefix_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Cidr", "192.168.1.0");
        var script = Substitute(DisplayScriptLibrary.ValidateCidr, "Cidr");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Cidr");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("/"));
    }

    [Test]
    public void ValidateCidr_PrefixTooLarge_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Cidr", "192.168.1.0/33");
        var script = Substitute(DisplayScriptLibrary.ValidateCidr, "Cidr");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Cidr");
        Assert.That(setting!.IsValid, Is.False);
        Assert.That(setting.ValidationExplanation, Does.Contain("32"));
    }

    [Test]
    public void ValidateCidr_Empty_IsNotValid()
    {
        // Arrange
        var client = _runner.CreateTestClient("TestClient");
        client.AddStringSetting("Cidr", "");
        var script = Substitute(DisplayScriptLibrary.ValidateCidr, "Cidr");

        // Act
        _runner.RunScript(script, client);

        // Assert
        var setting = client.GetSetting("Cidr");
        Assert.That(setting!.IsValid, Is.False);
    }

    #endregion
}
