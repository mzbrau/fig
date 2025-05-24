using System.Threading.Tasks;
using Fig.Client.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using Fig.Unit.Test.TestInfrastructure;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigConfigurationHealthCheckTests
{
    [Test]
    public async Task ShallValidateAtPropertyLevelValidation_Passes()
    {
        var options = new TestOptionsMonitor<SimpleSettings>(new SimpleSettings { DigitsOnly = "12345", NotEmpty = "abc" });
        var healthCheck = new FigConfigurationHealthCheck<SimpleSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task PropertyLevelValidation_Fails()
    {
        var options = new TestOptionsMonitor<SimpleSettings>(new SimpleSettings { DigitsOnly = "abc", NotEmpty = "" });
        var healthCheck = new FigConfigurationHealthCheck<SimpleSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("DigitsOnly").And.Contain("NotEmpty"));
    }

    [Test]
    public async Task ClassLevelValidation_AppliesToAllExceptPropertyLevel()
    {
        var options = new TestOptionsMonitor<ClassLevelSettings>(new ClassLevelSettings { Name = "Jo", Description = "De", Special = "Banana" });
        var healthCheck = new FigConfigurationHealthCheck<ClassLevelSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("Name").And.Contain("Description").And.Contain("Special"));
    }

    [Test]
    public async Task ClassLevelValidation_PassesWhenAllValid()
    {
        var options = new TestOptionsMonitor<ClassLevelSettings>(new ClassLevelSettings { Name = "John", Description = "Desc", Special = "Apple" });
        var healthCheck = new FigConfigurationHealthCheck<ClassLevelSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task NestedSetting_Validation_Passes()
    {
        var options = new TestOptionsMonitor<DeepNestedSettings>(new DeepNestedSettings { Parent = new NestedParent { Child = new NestedChild { Code = "99" } } });
        var healthCheck = new FigConfigurationHealthCheck<DeepNestedSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task NestedSetting_Validation_Fails()
    {
        var options = new TestOptionsMonitor<DeepNestedSettings>(new DeepNestedSettings { Parent = new NestedParent { Child = new NestedChild { Code = "9" } } });
        var healthCheck = new FigConfigurationHealthCheck<DeepNestedSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("Parent.Child.Code"));
    }

    [Test]
    public async Task MultiLevelNested_Validation_Fails()
    {
        var options = new TestOptionsMonitor<MultiLevelSettings>(new MultiLevelSettings { Nested = new MultiLevelNested { Level2 = new Level2 { Level3 = new Level3 { Value = "Y" } } } });
        var healthCheck = new FigConfigurationHealthCheck<MultiLevelSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("Nested.Level2.Level3.Value"));
    }

    [Test]
    public async Task MultiLevelNested_Validation_Passes()
    {
        var options = new TestOptionsMonitor<MultiLevelSettings>(new MultiLevelSettings { Nested = new MultiLevelNested { Level2 = new Level2 { Level3 = new Level3 { Value = "XXX" } } } });
        var healthCheck = new FigConfigurationHealthCheck<MultiLevelSettings>(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }
}