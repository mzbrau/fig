using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Health;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class DatabaseHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenRepositorySucceeds()
    {
        var repository = new Mock<IApiStatusRepository>();
        repository
            .Setup(r => r.GetAllActive())
            .ReturnsAsync(new List<ApiStatusBusinessEntity>());

        var healthCheck = new DatabaseHealthCheck(repository.Object, NullLogger<DatabaseHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        repository.Verify(r => r.GetAllActive(), Times.Once);
    }

    [Test]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenRepositoryThrows()
    {
        var repository = new Mock<IApiStatusRepository>();
        repository
            .Setup(r => r.GetAllActive())
            .ThrowsAsync(new InvalidOperationException("db down"));

        var healthCheck = new DatabaseHealthCheck(repository.Object, NullLogger<DatabaseHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
        Assert.That(result.Description, Does.Contain("Database health check failed"));
    }
}
