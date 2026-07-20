using Fig.Contracts.Authentication;
using Fig.Web;
using Fig.Web.Services.Authentication;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class WebAuthenticationSettingsValidatorTests
{
    [Test]
    public void Validate_ShouldAllowDefaultFigManagedModeWithoutKeycloakSettings()
    {
        var settings = new WebSettings();

        Assert.DoesNotThrow(() => WebAuthenticationSettingsValidator.Validate(settings));
    }

    [Test]
    public void Validate_ShouldRequireClientIdInKeycloakMode()
    {
        var settings = CreateKeycloakSettings();
        settings.Authentication.Keycloak.ClientId = "";

        var exception = Assert.Throws<ApplicationException>(() =>
            WebAuthenticationSettingsValidator.Validate(settings));

        Assert.That(exception!.Message, Does.Contain("ClientId"));
    }

    [Test]
    public void Validate_ShouldRequireRoleMappingsInKeycloakMode()
    {
        var settings = CreateKeycloakSettings();
        settings.Authentication.Keycloak.RoleMappings.Remove(Role.User.ToString());

        var exception = Assert.Throws<ApplicationException>(() =>
            WebAuthenticationSettingsValidator.Validate(settings));

        Assert.That(exception!.Message, Does.Contain("RoleMappings:User"));
    }

    [Test]
    public void Validate_ShouldRequireNonNullRoleMappingValuesInKeycloakMode()
    {
        var settings = CreateKeycloakSettings();
        settings.Authentication.Keycloak.RoleMappings[Role.User.ToString()] = null!;

        var exception = Assert.Throws<ApplicationException>(() =>
            WebAuthenticationSettingsValidator.Validate(settings));

        Assert.That(exception!.Message, Does.Contain("RoleMappings:User"));
    }

    [Test]
    public void Validate_ShouldAllowCompleteKeycloakSettings()
    {
        var settings = CreateKeycloakSettings();

        Assert.DoesNotThrow(() => WebAuthenticationSettingsValidator.Validate(settings));
    }

    private static WebSettings CreateKeycloakSettings()
    {
        return new WebSettings
        {
            Authentication = new WebAuthenticationSettings
            {
                Mode = WebAuthMode.Keycloak,
                Keycloak = new WebKeycloakAuthenticationSettings
                {
                    Authority = "https://keycloak.example.com/realms/fig",
                    ClientId = "fig-web",
                    RoleClaimPaths = ["groups"],
                    AllowedClassificationsClaim = "fig_allowed_classifications",
                    AdminRoleName = Role.Administrator.ToString()
                }
            }
        };
    }
}
