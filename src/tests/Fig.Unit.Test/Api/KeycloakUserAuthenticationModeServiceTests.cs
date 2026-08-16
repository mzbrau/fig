using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fig.Api;
using Fig.Api.Authorization.UserAuth;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class KeycloakUserAuthenticationModeServiceTests
{
    [Test]
    public void ResolveAllowedClassifications_AdminWithoutClaim_ReturnsAllClassifications()
    {
        var settings = CreateSettings();
        var principal = CreatePrincipal();

        var classifications = KeycloakUserAuthenticationModeService.ResolveAllowedClassifications(
            principal, Role.Administrator, settings);

        Assert.That(classifications, Is.EquivalentTo(Enum.GetValues<Classification>()));
    }

    [Test]
    public void ResolveAllowedClassifications_NonAdminWithoutClaim_ReturnsNull()
    {
        var settings = CreateSettings();
        var principal = CreatePrincipal();

        var classifications = KeycloakUserAuthenticationModeService.ResolveAllowedClassifications(
            principal, Role.User, settings);

        Assert.That(classifications, Is.Null);
    }

    [Test]
    public void ResolveAllowedClassifications_CommaSeparatedClaim_ParsesValues()
    {
        var settings = CreateSettings();
        var principal = CreatePrincipal(("fig_allowed_classifications", "Technical, Functional"));

        var classifications = KeycloakUserAuthenticationModeService.ResolveAllowedClassifications(
            principal, Role.User, settings);

        Assert.That(classifications, Is.EquivalentTo([Classification.Technical, Classification.Functional]));
    }

    [Test]
    public void ResolveAllowedClassifications_JsonArrayClaim_ParsesValues()
    {
        var settings = CreateSettings();
        var principal = CreatePrincipal(("fig_allowed_classifications", "[\"Special\",\"Technical\"]"));

        var classifications = KeycloakUserAuthenticationModeService.ResolveAllowedClassifications(
            principal, Role.ReadOnly, settings);

        Assert.That(classifications, Is.EquivalentTo([Classification.Special, Classification.Technical]));
    }

    [Test]
    public void ResolveAllowedClassifications_InvalidJson_ReturnsNull()
    {
        var settings = CreateSettings();
        var principal = CreatePrincipal(("fig_allowed_classifications", "[not-json"));

        var classifications = KeycloakUserAuthenticationModeService.ResolveAllowedClassifications(
            principal, Role.User, settings);

        Assert.That(classifications, Is.Null);
    }

    [Test]
    public void ResolveAllowedClassifications_UnknownValuesOnly_ReturnsNull()
    {
        var settings = CreateSettings();
        var principal = CreatePrincipal(("fig_allowed_classifications", "NotARealClassification"));

        var classifications = KeycloakUserAuthenticationModeService.ResolveAllowedClassifications(
            principal, Role.User, settings);

        Assert.That(classifications, Is.Null);
    }

    [Test]
    public void ResolveRole_ShouldGrantRoleFromLegacyRoleClaimPath()
    {
        var settings = new KeycloakAuthenticationSettings
        {
            RoleClaimPaths = [],
            RoleClaimPath = "realm_access.roles",
            AdditionalRoleClaimPath = null
        };
        var token = CreateToken(payload =>
        {
            payload["realm_access"] = new Dictionary<string, object>
            {
                ["roles"] = new[] { Role.Dashboard.ToString() }
            };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.EqualTo(Role.Dashboard));
    }

    [Test]
    public void ResolveRole_ShouldPreferAdministratorOverLowerRoles()
    {
        var settings = CreateSettings(roleClaimPaths: ["groups", "realm_access.roles"]);
        var userOnlyToken = CreateToken(payload =>
        {
            payload["groups"] = new[] { $"/fig/{Role.User}" };
        });
        Assert.That(KeycloakUserAuthenticationModeService.ResolveRole(userOnlyToken, settings), Is.EqualTo(Role.User));

        var token = CreateToken(payload =>
        {
            payload["groups"] = new[] { $"/fig/{Role.User}" };
            payload["realm_access"] = new Dictionary<string, object>
            {
                ["roles"] = new[] { Role.Administrator.ToString() }
            };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.EqualTo(Role.Administrator));
    }

    [Test]
    public void ResolveRole_ShouldUseAdditionalRoleClaimPath()
    {
        var settings = new KeycloakAuthenticationSettings
        {
            RoleClaimPaths = [],
            RoleClaimPath = string.Empty,
            AdditionalRoleClaimPath = "resource_access.fig.roles"
        };
        var token = CreateToken(payload =>
        {
            payload["resource_access"] = new Dictionary<string, object>
            {
                ["fig"] = new Dictionary<string, object>
                {
                    ["roles"] = new[] { Role.LookupService.ToString() }
                }
            };
        });

        var role = KeycloakUserAuthenticationModeService.ResolveRole(token, settings);

        Assert.That(role, Is.EqualTo(Role.LookupService));
    }

    [TestCase(".*", true)]
    [TestCase("^client-.*$", true)]
    [TestCase("[invalid", false)]
    [TestCase("(", false)]
    public void IsValidRegex_ShouldValidatePattern(string pattern, bool expected)
    {
        Assert.That(KeycloakUserAuthenticationModeService.IsValidRegex(pattern), Is.EqualTo(expected));
    }

    [Test]
    public void ExtractBearerToken_ReturnsTokenFromAuthorizationHeader()
    {
        var jwt = CreateSignedJwt(("sub", "user-1"), ("preferred_username", "alice"));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {jwt}";

        var token = KeycloakUserAuthenticationModeService.ExtractBearerToken(context);

        Assert.That(token, Is.EqualTo(jwt));
    }

    [Test]
    public void ExtractBearerToken_IsCaseInsensitiveForBearerPrefix()
    {
        var jwt = CreateSignedJwt(("sub", "user-2"));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"bearer {jwt}";

        var token = KeycloakUserAuthenticationModeService.ExtractBearerToken(context);

        Assert.That(token, Is.EqualTo(jwt));
    }

    [Test]
    public void ExtractBearerToken_ReturnsNull_WhenHeaderMissingOrNotBearer()
    {
        var missing = new DefaultHttpContext();
        var basic = new DefaultHttpContext();
        basic.Request.Headers.Authorization = "Basic abc";

        Assert.That(KeycloakUserAuthenticationModeService.ExtractBearerToken(missing), Is.Null);
        Assert.That(KeycloakUserAuthenticationModeService.ExtractBearerToken(basic), Is.Null);
    }

    [Test]
    public void ExtractBearerToken_TrimsWhitespaceAroundToken()
    {
        var jwt = CreateSignedJwt(("sub", "user-3"));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer   {jwt}   ";

        var token = KeycloakUserAuthenticationModeService.ExtractBearerToken(context);

        Assert.That(token, Is.EqualTo(jwt));
    }

    private static KeycloakAuthenticationSettings CreateSettings(List<string>? roleClaimPaths = null)
    {
        return new KeycloakAuthenticationSettings
        {
            RoleClaimPaths = roleClaimPaths ?? ["groups"],
            RoleClaimPath = string.Empty,
            AdditionalRoleClaimPath = null,
            AllowedClassificationsClaim = "fig_allowed_classifications",
            ClientFilterClaim = "fig_client_filter"
        };
    }

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value))));
    }

    private static JwtSecurityToken CreateToken(Action<JwtPayload> configurePayload)
    {
        var payload = new JwtPayload();
        configurePayload(payload);
        return new JwtSecurityToken(new JwtHeader(), payload);
    }

    private static string CreateSignedJwt(params (string Type, string Value)[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("unit-test-signing-key-32-chars!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "https://keycloak.test/realms/fig",
            audience: "fig-api",
            claims: claims.Select(c => new Claim(c.Type, c.Value)),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
