using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Fig.Test.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
[NonParallelizable]
public class ReleaseHighlightsTests : IntegrationTestBase
{
    [Test]
    public async Task ShallPersistViewedHighlightsPerAdministrator()
    {
        var request = new ReleaseHighlightViewedDataContract("3.5", "custom-groups");

        var created = await ApiClient.Post<ReleaseHighlightViewDataContract>("/releasehighlights/viewed", request);
        var progress = await ApiClient.Get<ReleaseHighlightProgressDataContract>("/releasehighlights");

        Assert.That(created, Is.Not.Null);
        Assert.That(progress, Is.Not.Null);
        Assert.That(progress!.ViewedHighlights.Any(x => x.ReleaseVersion == "3.5" && x.FeatureKey == "custom-groups"),
            Is.True);

        var otherAdmin = NewUser("otherAdmin", role: Role.Administrator);
        await CreateUser(otherAdmin);
        var loginResult = await Login(otherAdmin.Username, otherAdmin.Password ?? throw new InvalidOperationException("Password is required"));

        var otherAdminProgress = await ApiClient.Get<ReleaseHighlightProgressDataContract>(
            "/releasehighlights",
            tokenOverride: $"Bearer {loginResult.Token}");

        Assert.That(otherAdminProgress, Is.Not.Null);
        Assert.That(otherAdminProgress!.ViewedHighlights, Is.Empty);
    }

    [Test]
    public async Task ShallNotDuplicateViewedHighlightsForSameAdministrator()
    {
        var request = new ReleaseHighlightViewedDataContract("3.4", "mcp-server");

        var first = await ApiClient.Post<ReleaseHighlightViewDataContract>("/releasehighlights/viewed", request);
        var second = await ApiClient.Post<ReleaseHighlightViewDataContract>("/releasehighlights/viewed", request);
        var progress = await ApiClient.Get<ReleaseHighlightProgressDataContract>("/releasehighlights");

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(progress, Is.Not.Null);
        Assert.That(progress!.ViewedHighlights.Count(x => x.ReleaseVersion == "3.4" && x.FeatureKey == "mcp-server"),
            Is.EqualTo(1));
        Assert.That(second!.ViewedAtUtc, Is.EqualTo(first!.ViewedAtUtc));
    }

    [Test]
    public async Task ShallOnlyAllowReleaseHighlightEndpointsFromAdministrators()
    {
        var user = NewUser("regularUser", role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is required"));
        var bearerToken = $"Bearer {loginResult.Token}";

        var getResponse = await ApiClient.GetRaw("/releasehighlights", bearerToken);
        var postResponse = await ApiClient.Post<HttpResponseMessage>(
            "/releasehighlights/viewed",
            new ReleaseHighlightViewedDataContract("3.5", "notification-history"),
            tokenOverride: bearerToken,
            validateSuccess: false);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(postResponse, Is.Not.Null);
        Assert.That(postResponse!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
