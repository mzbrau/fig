using System;
using Fig.Api.WebHooks;
using NUnit.Framework;

namespace Fig.Unit.Test.WebHooks;

[TestFixture]
public class SecurityEventWebHookDataTests
{
    [Test]
    public void SecurityEventWebHookData_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var eventType = "Login";
        var timestamp = DateTime.UtcNow;
        var username = "testuser";
        var success = true;
        var ipAddress = "192.168.1.1";
        var hostname = "testhost";
        var failureReason = "Failed reason";

        // Act
        var webHookData = new SecurityEventWebHookData(
            eventType, timestamp, username, success, ipAddress, hostname, failureReason);

        // Assert
        Assert.That(webHookData.EventType, Is.EqualTo(eventType));
        Assert.That(webHookData.Timestamp, Is.EqualTo(timestamp));
        Assert.That(webHookData.Username, Is.EqualTo(username));
        Assert.That(webHookData.Success, Is.EqualTo(success));
        Assert.That(webHookData.IpAddress, Is.EqualTo(ipAddress));
        Assert.That(webHookData.Hostname, Is.EqualTo(hostname));
        Assert.That(webHookData.FailureReason, Is.EqualTo(failureReason));
    }

    [Test]
    public void SecurityEventWebHookData_Constructor_WithoutFailureReason_SetsDefaultNull()
    {
        // Arrange
        var eventType = "Login";
        var timestamp = DateTime.UtcNow;
        var username = "testuser";
        var success = true;
        var ipAddress = "192.168.1.1";
        var hostname = "testhost";

        // Act
        var webHookData = new SecurityEventWebHookData(
            eventType, timestamp, username, success, ipAddress, hostname);

        // Assert
        Assert.That(webHookData.EventType, Is.EqualTo(eventType));
        Assert.That(webHookData.Timestamp, Is.EqualTo(timestamp));
        Assert.That(webHookData.Username, Is.EqualTo(username));
        Assert.That(webHookData.Success, Is.EqualTo(success));
        Assert.That(webHookData.IpAddress, Is.EqualTo(ipAddress));
        Assert.That(webHookData.Hostname, Is.EqualTo(hostname));
        Assert.That(webHookData.FailureReason, Is.Null);
    }
}