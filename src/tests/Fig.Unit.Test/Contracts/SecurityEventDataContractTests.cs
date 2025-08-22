using System;
using Fig.WebHooks.Contracts;
using NUnit.Framework;

namespace Fig.Unit.Test.Contracts;

[TestFixture]
public class SecurityEventDataContractTests
{
    [Test]
    public void SecurityEventDataContract_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var eventType = "Login";
        var timestamp = DateTime.UtcNow;
        var username = "testuser";
        var success = true;
        var ipAddress = "192.168.1.1";
        var hostname = "testhost";

        // Act
        var securityEvent = new SecurityEventDataContract(
            eventType, timestamp, username, success, ipAddress, hostname);

        // Assert
        Assert.That(securityEvent.EventType, Is.EqualTo(eventType));
        Assert.That(securityEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(securityEvent.Username, Is.EqualTo(username));
        Assert.That(securityEvent.Success, Is.EqualTo(success));
        Assert.That(securityEvent.IpAddress, Is.EqualTo(ipAddress));
        Assert.That(securityEvent.Hostname, Is.EqualTo(hostname));
        Assert.That(securityEvent.FailureReason, Is.Null);
    }

    [Test]
    public void SecurityEventDataContract_Constructor_WithFailureReason_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var eventType = "Login";
        var timestamp = DateTime.UtcNow;
        var username = "testuser";
        var success = false;
        var ipAddress = "192.168.1.1";
        var hostname = "testhost";
        var failureReason = "Invalid password";

        // Act
        var securityEvent = new SecurityEventDataContract(
            eventType, timestamp, username, success, ipAddress, hostname, failureReason);

        // Assert
        Assert.That(securityEvent.EventType, Is.EqualTo(eventType));
        Assert.That(securityEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(securityEvent.Username, Is.EqualTo(username));
        Assert.That(securityEvent.Success, Is.EqualTo(success));
        Assert.That(securityEvent.IpAddress, Is.EqualTo(ipAddress));
        Assert.That(securityEvent.Hostname, Is.EqualTo(hostname));
        Assert.That(securityEvent.FailureReason, Is.EqualTo(failureReason));
    }

    [Test]
    public void SecurityEventDataContract_Constructor_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var eventType = "Login";
        var timestamp = DateTime.UtcNow;

        // Act
        var securityEvent = new SecurityEventDataContract(
            eventType, timestamp, null, false, null, null, "User not found");

        // Assert
        Assert.That(securityEvent.EventType, Is.EqualTo(eventType));
        Assert.That(securityEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(securityEvent.Username, Is.Null);
        Assert.That(securityEvent.Success, Is.False);
        Assert.That(securityEvent.IpAddress, Is.Null);
        Assert.That(securityEvent.Hostname, Is.Null);
        Assert.That(securityEvent.FailureReason, Is.EqualTo("User not found"));
    }
}