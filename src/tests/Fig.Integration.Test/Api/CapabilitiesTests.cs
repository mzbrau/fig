using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Capabilities;
using Fig.Test.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class CapabilitiesTests : IntegrationTestBase
{
    [Test]
    public async Task GetCapabilities_ReturnsOk_WithoutAuthentication()
    {
        var result = await ApiClient.Get<FigCapabilitiesDataContract>("/capabilities", authenticate: false);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetCapabilities_ReturnsApiVersion()
    {
        var result = await ApiClient.Get<FigCapabilitiesDataContract>("/capabilities", authenticate: false);

        Assert.That(result!.ApiVersion, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetCapabilities_IncludesExpectedFeatureTokens()
    {
        var result = await ApiClient.Get<FigCapabilitiesDataContract>("/capabilities", authenticate: false);

        Assert.That(result!.SupportedFeatures, Is.Not.Null);
        Assert.That(result.SupportedFeatures, Contains.Item("deferredDescriptionRegistration"));
        Assert.That(result.SupportedFeatures, Contains.Item("requestCompression"));
    }

    [Test]
    public async Task GetCapabilities_SupportedFeaturesAreNotEmpty()
    {
        var result = await ApiClient.Get<FigCapabilitiesDataContract>("/capabilities", authenticate: false);

        Assert.That(result!.SupportedFeatures.Any(), Is.True);
    }
}
