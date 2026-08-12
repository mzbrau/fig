using Fig.Web;
using Fig.Web.Services.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class OidcProviderOptionsConfiguratorTests
{
    [Test]
    public void Apply_ShouldNotAddHintOrPrompt_WhenDefaults()
    {
        var options = new OidcProviderOptions();
        var settings = new WebKeycloakAuthenticationSettings();

        OidcProviderOptionsConfigurator.Apply(options, settings);

        Assert.That(options.AdditionalProviderParameters.ContainsKey("kc_idp_hint"), Is.False);
        Assert.That(options.AdditionalProviderParameters.ContainsKey("prompt"), Is.False);
    }

    [Test]
    public void Apply_ShouldAddKcIdpHint_WhenEnabledAndHintConfigured()
    {
        var options = new OidcProviderOptions();
        var settings = new WebKeycloakAuthenticationSettings
        {
            EnableIdentityProviderHint = true,
            IdentityProviderHint = " entra-id "
        };

        OidcProviderOptionsConfigurator.Apply(options, settings);

        Assert.That(options.AdditionalProviderParameters["kc_idp_hint"], Is.EqualTo("entra-id"));
    }

    [Test]
    public void Apply_ShouldNotAddKcIdpHint_WhenDisabled()
    {
        var options = new OidcProviderOptions();
        var settings = new WebKeycloakAuthenticationSettings
        {
            EnableIdentityProviderHint = false,
            IdentityProviderHint = "entra-id"
        };

        OidcProviderOptionsConfigurator.Apply(options, settings);

        Assert.That(options.AdditionalProviderParameters.ContainsKey("kc_idp_hint"), Is.False);
    }

    [Test]
    public void Apply_ShouldNotAddKcIdpHint_WhenHintWhitespace()
    {
        var options = new OidcProviderOptions();
        var settings = new WebKeycloakAuthenticationSettings
        {
            EnableIdentityProviderHint = true,
            IdentityProviderHint = "   "
        };

        OidcProviderOptionsConfigurator.Apply(options, settings);

        Assert.That(options.AdditionalProviderParameters.ContainsKey("kc_idp_hint"), Is.False);
    }

    [Test]
    public void Apply_ShouldAddPrompt_WhenLoginPromptConfigured()
    {
        var options = new OidcProviderOptions();
        var settings = new WebKeycloakAuthenticationSettings
        {
            LoginPrompt = " login "
        };

        OidcProviderOptionsConfigurator.Apply(options, settings);

        Assert.That(options.AdditionalProviderParameters["prompt"], Is.EqualTo("login"));
    }
}
