using Fig.Client.ConfigurationProvider;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class RunSessionTests
{
    [SetUp]
    public void SetUp()
    {
        RunSession.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        RunSession.Clear();
    }

    [Test]
    public void GetId_WithSameClientName_ReturnsSameId()
    {
        var first = RunSession.GetId("client-a");
        var second = RunSession.GetId("client-a");

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void GetId_WithDifferentClientNames_ReturnsDifferentIds()
    {
        var first = RunSession.GetId("client-a");
        var second = RunSession.GetId("client-b");

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void Release_AfterLastAcquire_RemovesSession()
    {
        var first = RunSession.Acquire("client-a");

        RunSession.Release("client-a");
        var second = RunSession.GetId("client-a");

        Assert.That(RunSession.Count, Is.EqualTo(1));
        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void Release_WithMultipleAcquires_KeepsSessionUntilLastRelease()
    {
        var first = RunSession.Acquire("client-a");
        var second = RunSession.Acquire("client-a");

        RunSession.Release("client-a");
        var afterFirstRelease = RunSession.GetId("client-a");
        RunSession.Release("client-a");
        var afterSecondRelease = RunSession.GetId("client-a");

        Assert.That(second, Is.EqualTo(first));
        Assert.That(afterFirstRelease, Is.EqualTo(first));
        Assert.That(afterSecondRelease, Is.Not.EqualTo(first));
    }

    [Test]
    public async Task GetId_WhenCalledConcurrently_ReturnsSameId()
    {
        var ids = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => RunSession.GetId("client-a"))));

        Assert.That(ids.Distinct().Count(), Is.EqualTo(1));
    }
}

