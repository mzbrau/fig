using System;
using Fig.WebHooks.Contracts;
using NUnit.Framework;

namespace Fig.Unit.Test.WebHooks;

[TestFixture]
public class WebHookContractTests
{
    [Test]
    public void SecurityEventDataContract_Constructor_SetsIsTestPropertyCorrectly()
    {
        // Arrange & Act - Normal event
        var normalEvent = new SecurityEventDataContract(
            "Login", DateTime.UtcNow, "user", true, "192.168.1.1", "host");

        // Arrange & Act - Test event
        var testEvent = new SecurityEventDataContract(
            "Login", DateTime.UtcNow, "user", true, "192.168.1.1", "host", isTest: true);

        // Assert
        Assert.That(normalEvent.IsTest, Is.False, "Normal event should have IsTest = false");
        Assert.That(testEvent.IsTest, Is.True, "Test event should have IsTest = true");
    }

    [Test]
    public void SettingValueChangedDataContract_Constructor_SetsIsTestPropertyCorrectly()
    {
        // Arrange & Act - Normal event
        var normalEvent = new SettingValueChangedDataContract(
            "Client", null, ["Setting1"], "user", "change", null);

        // Arrange & Act - Test event
        var testEvent = new SettingValueChangedDataContract(
            "Client", null, ["Setting1"], "user", "change", null, isTest: true);

        // Assert
        Assert.That(normalEvent.IsTest, Is.False, "Normal event should have IsTest = false");
        Assert.That(testEvent.IsTest, Is.True, "Test event should have IsTest = true");
    }

    [Test]
    public void ClientRegistrationDataContract_Constructor_SetsIsTestPropertyCorrectly()
    {
        // Arrange & Act - Normal event
        var normalEvent = new ClientRegistrationDataContract(
            "Client", null, ["Setting1"], RegistrationType.New, null);

        // Arrange & Act - Test event
        var testEvent = new ClientRegistrationDataContract(
            "Client", null, ["Setting1"], RegistrationType.New, null, isTest: true);

        // Assert
        Assert.That(normalEvent.IsTest, Is.False, "Normal event should have IsTest = false");
        Assert.That(testEvent.IsTest, Is.True, "Test event should have IsTest = true");
    }

    [Test]
    public void ClientStatusChangedDataContract_Constructor_SetsIsTestPropertyCorrectly()
    {
        // Arrange & Act - Normal event
        var normalEvent = new ClientStatusChangedDataContract(
            "Client", null, ConnectionEvent.Connected, DateTime.UtcNow, "192.168.1.1", "host", "v1", "v2", null);

        // Arrange & Act - Test event
        var testEvent = new ClientStatusChangedDataContract(
            "Client", null, ConnectionEvent.Connected, DateTime.UtcNow, "192.168.1.1", "host", "v1", "v2", null, isTest: true);

        // Assert
        Assert.That(normalEvent.IsTest, Is.False, "Normal event should have IsTest = false");
        Assert.That(testEvent.IsTest, Is.True, "Test event should have IsTest = true");
    }

    [Test]
    public void ClientHealthChangedDataContract_Constructor_SetsIsTestPropertyCorrectly()
    {
        // Arrange & Act - Normal event
        var normalEvent = new ClientHealthChangedDataContract(
            "Client", null, "host", "192.168.1.1", HealthStatus.Healthy, "v1", "v2", new HealthDetails(), null);

        // Arrange & Act - Test event
        var testEvent = new ClientHealthChangedDataContract(
            "Client", null, "host", "192.168.1.1", HealthStatus.Healthy, "v1", "v2", new HealthDetails(), null, isTest: true);

        // Assert
        Assert.That(normalEvent.IsTest, Is.False, "Normal event should have IsTest = false");
        Assert.That(testEvent.IsTest, Is.True, "Test event should have IsTest = true");
    }

    [Test]
    public void MinRunSessionsDataContract_Constructor_SetsIsTestPropertyCorrectly()
    {
        // Arrange & Act - Normal event
        var normalEvent = new MinRunSessionsDataContract(
            "Client", null, 1, RunSessionsEvent.BelowMinimum, null);

        // Arrange & Act - Test event
        var testEvent = new MinRunSessionsDataContract(
            "Client", null, 1, RunSessionsEvent.BelowMinimum, null, isTest: true);

        // Assert
        Assert.That(normalEvent.IsTest, Is.False, "Normal event should have IsTest = false");
        Assert.That(testEvent.IsTest, Is.True, "Test event should have IsTest = true");
    }
}