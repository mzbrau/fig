using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Fig.Web.Services.Authentication;

/// <summary>
/// No-op provider so <see cref="CascadingAuthenticationState"/> can resolve in FigManaged mode.
/// Session state lives in <see cref="FigManagedWebAuthenticationModeService"/> / <see cref="IAccountService"/>.
/// </summary>
public class FigManagedAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> AnonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => AnonymousState;
}
