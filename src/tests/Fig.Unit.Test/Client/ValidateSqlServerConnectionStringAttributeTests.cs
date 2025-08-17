using Fig.Client.Abstractions.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateSqlServerConnectionStringAttributeTests
{
    private ValidateSqlServerConnectionStringAttribute _attribute = null!;

    [SetUp]
    public void Setup()
    {
        _attribute = new ValidateSqlServerConnectionStringAttribute();
    }

    [Test]
    public void ApplyToTypes_ShouldReturnStringType()
    {
        // Act
        var types = _attribute.ApplyToTypes;

        // Assert
        Assert.That(types, Contains.Item(typeof(string)));
        Assert.That(types.Length, Is.EqualTo(1));
    }

    [Test]
    public void IsValid_WithValidConnectionString_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid("Server=localhost;Database=TestDB;Integrated Security=true;");

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValidConnectionStringUsingDataSource_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid("Data Source=localhost;Initial Catalog=TestDB;User ID=sa;Password=test;");

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValidConnectionStringUsingAttachDBFilename_ShouldReturnTrue()
    {
        // Act
        var result =
            _attribute.IsValid("Server=localhost;AttachDBFilename=C:\\Data\\MyDB.mdf;Integrated Security=true;");

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithMissingServer_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid("Database=TestDB;Integrated Security=true;");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("Connection string must contain a valid Data Source (Server)"));
    }

    [Test]
    public void IsValid_WithMissingDatabase_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid("Server=localhost;Integrated Security=true;");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2,
            Is.EqualTo("Connection string must contain either Initial Catalog (Database) or AttachDBFilename"));
    }

    [Test]
    public void IsValid_WithInvalidConnectionString_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid("Invalid connection string format");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Contains.Substring("Connection string must contain a valid Data Source (Server)"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not a valid SQL Server connection string"));
    }

    [Test]
    public void IsValid_WithEmptyString_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid("");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not a valid SQL Server connection string"));
    }

    [Test]
    public void IsValid_WithWhitespaceString_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid("   ");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("    is not a valid SQL Server connection string"));
    }

    [Test]
    public void IsValid_WithNonStringValue_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(123);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("123 is not a valid SQL Server connection string"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateSqlServerConnectionStringAttribute(includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid("invalid connection string");

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [Test]
    public void GetScript_ShouldReturnCorrectJavaScript()
    {
        // Act
        var script = _attribute.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value"));
        Assert.That(script, Contains.Substring("data source"));
        Assert.That(script, Contains.Substring("initial catalog"));
        Assert.That(script, Contains.Substring("attachdbfilename"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("Data Source (Server)"));
    }
}