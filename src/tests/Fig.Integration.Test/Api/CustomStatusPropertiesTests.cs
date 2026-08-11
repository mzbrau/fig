using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Contracts.Status;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class CustomStatusPropertiesTests : IntegrationTestBase
{
    [Test]
    public async Task ShallPersistAndReturnCustomStatusProperties()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var runSessionId = Guid.NewGuid();

        var statusRequest = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        statusRequest.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("LastSyncUtc", CustomStatusValueType.DateTime,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                displayName: "Last Sync", highlight: true, order: 1),
            new CustomStatusPropertyDataContract("QueueDepth", CustomStatusValueType.Long, 42L,
                displayName: "Queue", highlight: true, order: 2),
            new CustomStatusPropertyDataContract("Usage", CustomStatusValueType.String, "HIGH",
                displayName: "Usage", highlight: true, order: 4, textColor: "#E53935"),
            new CustomStatusPropertyDataContract("IsDraining", CustomStatusValueType.Boolean, false,
                displayName: "Drain", highlight: true, order: 3),
            new CustomStatusPropertyDataContract("AverageLatency", CustomStatusValueType.TimeSpan, "00:00:01.5000000",
                displayName: "Avg latency"),
            new CustomStatusPropertyDataContract("CorrelationId", CustomStatusValueType.String, "abc-123",
                showInUi: false)
        ]);

        await GetStatus(settings.ClientName, secret, statusRequest);

        var statuses = (await GetAllStatuses()).ToList();
        var session = statuses.SelectMany(c => c.RunSessions)
            .Single(s => s.RunSessionId == runSessionId);

        Assert.That(session.CustomProperties, Is.Not.Null);
        Assert.That(session.CustomProperties!.Properties, Has.Count.EqualTo(6));
        Assert.That(session.CustomProperties.Properties.Single(p => p.Name == "QueueDepth").Value, Is.EqualTo(42L).Or.EqualTo(42));
        Assert.That(session.CustomProperties.Properties.Single(p => p.Name == "Usage").TextColor, Is.EqualTo("#E53935"));
        Assert.That(session.CustomProperties.Properties.Single(p => p.Name == "CorrelationId").ShowInUi, Is.False);

        var lightweight = (await GetCustomStatusProperties()).ToList();
        Assert.That(lightweight, Has.Count.EqualTo(1));
        Assert.That(lightweight[0].ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(lightweight[0].CustomProperties!.Properties, Has.Count.EqualTo(6));

        var byClient = (await GetCustomStatusProperties(settings.ClientName)).ToList();
        Assert.That(byClient, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ShallReplaceCustomPropertiesOnEachPollWithoutServerSideMerge()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var runSessionId = Guid.NewGuid();

        var first = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        first.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("A", CustomStatusValueType.String, "one"),
            new CustomStatusPropertyDataContract("B", CustomStatusValueType.String, "two")
        ]);
        await GetStatus(settings.ClientName, secret, first);

        var second = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        second.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("A", CustomStatusValueType.String, "updated"),
            new CustomStatusPropertyDataContract("C", CustomStatusValueType.Boolean, true)
        ]);
        await GetStatus(settings.ClientName, secret, second);

        var session = (await GetAllStatuses()).SelectMany(c => c.RunSessions)
            .Single(s => s.RunSessionId == runSessionId);
        var names = session.CustomProperties!.Properties.Select(p => p.Name).OrderBy(n => n).ToList();
        Assert.That(names, Is.EqualTo(new[] { "A", "C" }));
        Assert.That(session.CustomProperties.Properties.Single(p => p.Name == "A").Value, Is.EqualTo("updated"));
    }

    [Test]
    public async Task ShallLeaveCustomPropertiesUnchangedWhenNullOnPoll()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var runSessionId = Guid.NewGuid();

        var withProps = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        withProps.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("KeepMe", CustomStatusValueType.String, "yes")
        ]);
        await GetStatus(settings.ClientName, secret, withProps);

        var withoutProps = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        withoutProps.CustomProperties = null;
        await GetStatus(settings.ClientName, secret, withoutProps);

        var session = (await GetAllStatuses()).SelectMany(c => c.RunSessions)
            .Single(s => s.RunSessionId == runSessionId);
        Assert.That(session.CustomProperties!.Properties.Single().Name, Is.EqualTo("KeepMe"));
    }

    [Test]
    public async Task ShallClearCustomPropertiesWhenEmptyListSent()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var runSessionId = Guid.NewGuid();

        var withProps = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        withProps.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("Temp", CustomStatusValueType.String, "x")
        ]);
        await GetStatus(settings.ClientName, secret, withProps);

        var clear = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        clear.CustomProperties = new CustomStatusPropertiesDataContract([]);
        await GetStatus(settings.ClientName, secret, clear);

        var session = (await GetAllStatuses()).SelectMany(c => c.RunSessions)
            .Single(s => s.RunSessionId == runSessionId);
        Assert.That(session.CustomProperties!.Properties, Is.Empty);
    }

    [Test]
    public async Task ShallRejectOversizedCustomStatusProperties()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var statusRequest = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        statusRequest.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("Huge", CustomStatusValueType.String,
                new string('x', CustomStatusPropertiesLimits.MaxStringValueLength + 1))
        ]);

        var statusCode = await PutStatusExpectingFailure(settings.ClientName, secret, statusRequest);
        Assert.That(statusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ShallRoundTripRepresentativeScalarTypes()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var runSessionId = Guid.NewGuid();
        var guid = Guid.NewGuid();

        var statusRequest = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        statusRequest.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("Latency", CustomStatusValueType.TimeSpan, "01:02:03"),
            new CustomStatusPropertyDataContract("Offset", CustomStatusValueType.DateTimeOffset,
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            new CustomStatusPropertyDataContract("Id", CustomStatusValueType.Guid, guid.ToString("D")),
            new CustomStatusPropertyDataContract("Mode", CustomStatusValueType.Enum, "Active",
                enumTypeName: "ProcessingMode"),
            new CustomStatusPropertyDataContract("Amount", CustomStatusValueType.Decimal, "123.45")
        ]);

        await GetStatus(settings.ClientName, secret, statusRequest);

        var props = (await GetCustomStatusProperties(settings.ClientName)).Single().CustomProperties!.Properties;
        Assert.That(props.Single(p => p.Name == "Latency").ValueType, Is.EqualTo(CustomStatusValueType.TimeSpan));
        Assert.That(props.Single(p => p.Name == "Id").Value?.ToString(), Is.EqualTo(guid.ToString("D")));
        Assert.That(props.Single(p => p.Name == "Mode").Value, Is.EqualTo("Active"));
        Assert.That(props.Single(p => p.Name == "Amount").Value?.ToString(), Is.EqualTo("123.45"));
    }

    [Test]
    public async Task ShallFilterCustomPropertiesByEffectiveSessionInstance()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var runSessionId = Guid.NewGuid();
        const string instanceName = "prod";

        // Status poll with instance falls back to the no-instance registration and stores InstanceName on the session.
        var statusRequest = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true,
            runSessionId: runSessionId);
        statusRequest.CustomProperties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("Region", CustomStatusValueType.String, "eu-west")
        ]);
        await GetStatus(settings.ClientName, secret, statusRequest, instance: instanceName);

        var byInstance = (await GetCustomStatusProperties(settings.ClientName, instanceName)).ToList();
        Assert.That(byInstance, Has.Count.EqualTo(1));
        Assert.That(byInstance[0].Instance, Is.EqualTo(instanceName));
        Assert.That(byInstance[0].CustomProperties!.Properties.Single().Value, Is.EqualTo("eu-west"));

        var otherInstance = (await GetCustomStatusProperties(settings.ClientName, "other")).ToList();
        Assert.That(otherInstance, Is.Empty);
    }

    private async Task<IEnumerable<CustomStatusSessionPropertiesDataContract>> GetCustomStatusProperties(
        string? clientName = null, string? instance = null)
    {
        var uri = clientName is null
            ? "statuses/properties"
            : $"statuses/{Uri.EscapeDataString(clientName)}/properties";
        if (instance is not null)
            uri += $"?instance={Uri.EscapeDataString(instance)}";

        var result = await ApiClient.Get<IEnumerable<CustomStatusSessionPropertiesDataContract>>(uri);
        return result ?? [];
    }

    private async Task<HttpStatusCode> PutStatusExpectingFailure(string clientName, string clientSecret,
        StatusRequestDataContract status)
    {
        var json = JsonConvert.SerializeObject(status);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var uri = $"statuses/{Uri.EscapeDataString(clientName)}";
        using var response = await httpClient.PutAsync(uri, data);
        return response.StatusCode;
    }
}
