using Fig.Api;
using Fig.Api.Authorization.UserAuth;
using Fig.Contracts.Authentication;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class AuthenticationSettingsValidatorTests
{
    [Test]
    public void Validate_ShouldAllowDefaultFigManagedModeWithoutKeycloakSettings()
    {
        var settings = new ApiSettings
        {
            DbConnectionString = "Data Source=:memory:",
            Secret = "secret",
            Authentication = new AuthenticationSettings()
        };

        Assert.DoesNotThrow(() => AuthenticationSettingsValidator.Validate(settings));
    }

    [Test]
    public void Validate_ShouldRequireAudienceInKeycloakMode()
    {
        var settings = CreateKeycloakSettings();
        settings.Authentication.Keycloak.Audience = "";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthenticationSettingsValidator.Validate(settings));

        Assert.That(exception!.Message, Does.Contain("Audience"));
    }

    [Test]
    public void Validate_ShouldRequireRoleMappingsInKeycloakMode()
    {
        var settings = CreateKeycloakSettings();
        settings.Authentication.Keycloak.RoleMappings.Remove(Role.User.ToString());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthenticationSettingsValidator.Validate(settings));

        Assert.That(exception!.Message, Does.Contain("RoleMappings:User"));
    }

    [Test]
    public void Validate_ShouldRequireNonNullRoleMappingValuesInKeycloakMode()
    {
        var settings = CreateKeycloakSettings();
        settings.Authentication.Keycloak.RoleMappings[Role.User.ToString()] = null!;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthenticationSettingsValidator.Validate(settings));

        Assert.That(exception!.Message, Does.Contain("RoleMappings:User"));
    }

    [Test]
    public void Validate_ShouldAllowCompleteKeycloakSettings()
    {
        var settings = CreateKeycloakSettings();

        Assert.DoesNotThrow(() => AuthenticationSettingsValidator.Validate(settings));
    }

    private static ApiSettings CreateKeycloakSettings()
    {
        return new ApiSettings
        {
            DbConnectionString = "Data Source=:memory:",
            Secret = "secret",
            Authentication = new AuthenticationSettings
            {
                Mode = AuthMode.Keycloak,
                Keycloak = new KeycloakAuthenticationSettings
                {
                    Authority = "https://keycloak.example.com/realms/fig",
                    Audience = "fig-api",
                    RoleClaimPaths = ["groups"],
                    AllowedClassificationsClaim = "fig_allowed_classifications",
                    ClientFilterClaim = "fig_client_filter",
                    AdminRoleName = Role.Administrator.ToString()
                }
            }
        };
    }
}
