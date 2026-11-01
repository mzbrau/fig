using Fig.Api.Datalayer;
using NUnit.Framework;

namespace Fig.Unit.Test.Datalayer;

[TestFixture]
public class DatabaseProviderDetectorTests
{
    [TestCase("Data Source=fig.db;Version=3;New=True", DatabaseProvider.Sqlite)]
    [TestCase("Data Source=test.sqlite", DatabaseProvider.Sqlite)]
    [TestCase("Data Source=:memory:", DatabaseProvider.Sqlite)]
    [TestCase("URI=file:test.db", DatabaseProvider.Sqlite)]
    [TestCase("Server=localhost;Database=Fig;User Id=sa;Password=x;", DatabaseProvider.SqlServer)]
    [TestCase("Server=localhost;Initial Catalog=Fig;Trusted_Connection=true;", DatabaseProvider.SqlServer)]
    [TestCase("Server=localhost;Initial Catalog=postgres_archive;Trusted_Connection=true;", DatabaseProvider.SqlServer)]
    [TestCase("Server=localhost;Database=Fig;User Id=sa;Password=postgres123;", DatabaseProvider.SqlServer)]
    [TestCase("Server=localhost;Database=Fig;Application Name=postgres-sync;", DatabaseProvider.SqlServer)]
    [TestCase("Host=localhost;Port=5432;Database=fig;Username=fig;Password=x", DatabaseProvider.PostgreSql)]
    [TestCase("Host=db;Database=fig;Username=fig;Password=x", DatabaseProvider.PostgreSql)]
    [TestCase("Server=localhost;Port=5432;Database=fig;User Id=fig;Password=x", DatabaseProvider.PostgreSql)]
    [TestCase("Server=postgres-host;Database=fig;User Id=fig;Password=x", DatabaseProvider.PostgreSql)]
    public void Detect_ShouldResolveProviderFromConnectionString(string connectionString, DatabaseProvider expected)
    {
        var result = DatabaseProviderDetector.Detect(connectionString);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Detect_ShouldHonorExplicitOverride()
    {
        var result = DatabaseProviderDetector.Detect(
            "Server=localhost;Database=Fig;",
            DatabaseProvider.PostgreSql);

        Assert.That(result, Is.EqualTo(DatabaseProvider.PostgreSql));
    }

    [Test]
    public void Detect_ShouldThrow_WhenConnectionStringMissingAndNoOverride()
    {
        Assert.That(
            () => DatabaseProviderDetector.Detect(null),
            Throws.ArgumentException);
    }
}
