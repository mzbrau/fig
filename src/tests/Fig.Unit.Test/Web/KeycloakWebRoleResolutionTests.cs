using System.Security.Claims;
using Fig.Contracts.Authentication;
using Fig.Web;
using Fig.Web.Services.Authentication;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class KeycloakWebRoleResolutionTests
{
    [Test]
    public void ResolveRole_ShouldGrantRoleFromConfiguredResourceAccessPath()
    {
        var settings = CreateSettings(roleClaimPaths: ["resource_access.fig.roles"]);
        var principal = CreatePrincipal(
            ("resource_access", """{"fig":{"roles":["User"]}}"""));

        var role = KeycloakWebAuthenticationModeService.ResolveRole(principal, settings);

        Assert.That(role, Is.EqualTo(Role.User));
    }

    [Test]
    public void ResolveRole_ShouldGrantRoleFromConfiguredGroupsPath()
    {
        var settings = CreateSettings(roleClaimPaths: ["groups"]);
        var principal = CreatePrincipal(
            ("groups", """["/fig/Administrator"]"""));

        var role = KeycloakWebAuthenticationModeService.ResolveRole(principal, settings);

        Assert.That(role, Is.EqualTo(Role.Administrator));
    }

    [Test]
    public void ResolveRole_ShouldIgnoreAdministratorOnUnconfiguredResourceAccessClient()
    {
        var settings = CreateSettings(roleClaimPaths: ["resource_access.fig.roles"]);
        var principal = CreatePrincipal(
            ("resource_access", """{"other-client":{"roles":["Administrator"]},"fig":{"roles":["User"]}}"""));

        var role = KeycloakWebAuthenticationModeService.ResolveRole(principal, settings);

        Assert.That(role, Is.EqualTo(Role.User));
    }

    [Test]
    public void ResolveRole_ShouldReturnNullWhenOnlyUnconfiguredResourceAccessClientHasRole()
    {
        var settings = CreateSettings(roleClaimPaths: ["resource_access.fig.roles"]);
        var principal = CreatePrincipal(
            ("resource_access", """{"other-client":{"roles":["Administrator"]}}"""),
            ("groups", """["/fig/Administrator"]"""),
            ("realm_access", """{"roles":["Administrator"]}"""));

        var role = KeycloakWebAuthenticationModeService.ResolveRole(principal, settings);

        Assert.That(role, Is.Null);
    }

    private static WebKeycloakAuthenticationSettings CreateSettings(List<string> roleClaimPaths)
    {
        return new WebKeycloakAuthenticationSettings
        {
            RoleClaimPaths = roleClaimPaths
        };
    }

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}
