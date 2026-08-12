using System.IdentityModel.Tokens.Jwt;
using Fig.Api;
using Fig.Api.Authorization.UserAuth;
using Fig.Contracts.Authentication;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class KeycloakRoleResolutionTests
{
    [Test]
    public void ResolveRole_ShouldGrantRoleFromConfiguredResourceAccessPath()
    {
        var settings = CreateSettings(roleClaimPaths: ["resource_access.fig.roles"]);
        var token = CreateToken(payload =>
        {
            payload["resource_access"] = new Dictionary<string, object>
            {
                ["fig"] = new Dictionary<string, object>
                {
                    ["roles"] = new[] { Role.User.ToString() }
                }
            };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.EqualTo(Role.User));
    }

    [Test]
    public void ResolveRole_ShouldGrantRoleFromConfiguredGroupsPath()
    {
        var settings = CreateSettings(roleClaimPaths: ["groups"]);
        var token = CreateToken(payload =>
        {
            payload["groups"] = new[] { $"/fig/{Role.Administrator}" };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.EqualTo(Role.Administrator));
    }

    [Test]
    public void ResolveRole_ShouldIgnoreAdministratorOnUnconfiguredResourceAccessClient()
    {
        var settings = CreateSettings(roleClaimPaths: ["resource_access.fig.roles"]);
        var token = CreateToken(payload =>
        {
            payload["resource_access"] = new Dictionary<string, object>
            {
                ["other-client"] = new Dictionary<string, object>
                {
                    ["roles"] = new[] { Role.Administrator.ToString() }
                },
                ["fig"] = new Dictionary<string, object>
                {
                    ["roles"] = new[] { Role.User.ToString() }
                }
            };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.EqualTo(Role.User));
    }

    [Test]
    public void ResolveRole_ShouldReturnNullWhenOnlyUnconfiguredResourceAccessClientHasRole()
    {
        var settings = CreateSettings(roleClaimPaths: ["resource_access.fig.roles"]);
        var token = CreateToken(payload =>
        {
            payload["resource_access"] = new Dictionary<string, object>
            {
                ["other-client"] = new Dictionary<string, object>
                {
                    ["roles"] = new[] { Role.Administrator.ToString() }
                }
            };
            payload["groups"] = new[] { $"/fig/{Role.Administrator}" };
            payload["realm_access"] = new Dictionary<string, object>
            {
                ["roles"] = new[] { Role.Administrator.ToString() }
            };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.Null);
    }

    private static KeycloakAuthenticationSettings CreateSettings(List<string> roleClaimPaths)
    {
        return new KeycloakAuthenticationSettings
        {
            RoleClaimPaths = roleClaimPaths,
            // Clear legacy paths so tests assert RoleClaimPaths alone as the boundary.
            RoleClaimPath = string.Empty,
            AdditionalRoleClaimPath = null
        };
    }

    private static JwtSecurityToken CreateToken(Action<JwtPayload> configurePayload)
    {
        var payload = new JwtPayload();
        configurePayload(payload);
        return new JwtSecurityToken(new JwtHeader(), payload);
    }
}
