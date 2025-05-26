using Fig.Api.Authorization;
using Fig.Api.Exceptions;
using Fig.Api.Services;

using Fig.Api.Authorization;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiSettings _apiSettings;

    public AuthMiddleware(RequestDelegate next, IOptions<ApiSettings> apiSettings)
    {
        _next = next;
        _apiSettings = apiSettings.Value;
    }

    public async Task Invoke(HttpContext context,
        IUserService userService,
        IEnumerable<IAuthenticatedService> authenticatedServices,
        ITokenHandler tokenHandler)
    {
        if (_apiSettings.UseKeycloak)
        {
            if (context.User.Identity is { IsAuthenticated: true })
            {
                var subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usernameClaim = context.User.FindFirst(ClaimTypes.Name)?.Value ??
                                    context.User.FindFirst("preferred_username")?.Value;
                
                // Attempt to get roles. Keycloak might put them in 'realm_access.roles' or directly as 'role' claims.
                // This is a simplified approach. Real-world might need parsing JSON from a claim or handling multiple role claims.
                var roleClaim = context.User.FindAll(ClaimTypes.Role).FirstOrDefault()?.Value ??
                                context.User.FindFirst("client_role")?.Value ?? // Common for client-specific roles
                                context.User.FindFirst("realm_role")?.Value; // Common for realm roles

                if (subClaim != null && usernameClaim != null)
                {
                    // Create a GUID from the 'sub' claim. Hashing is one way to get a deterministic GUID.
                    // For simplicity, using a basic hash. A more robust hashing algorithm might be preferred.
                    Guid userId;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(subClaim));
                        var guidBytes = new byte[16];
                        Array.Copy(hash, guidBytes, 16);
                        userId = new Guid(guidBytes);
                    }

                    Role figRole = Role.User; // Default role
                    if (roleClaim != null && Enum.TryParse<Role>(roleClaim, true, out var parsedRole))
                    {
                        figRole = parsedRole;
                    }
                    else if (roleClaim != null)
                    {
                        // Log or handle unknown role claim if necessary
                        Console.WriteLine($"Warning: Unmapped role claim '{roleClaim}' for user '{usernameClaim}'. Defaulting to 'User'.");
                    }

                    var user = new UserBusinessEntity
                    {
                        Id = userId,
                        Username = usernameClaim,
                        Role = figRole,
                        // Keycloak users don't have these Fig-specific fields managed in Fig's DB directly
                        // when Keycloak is the source of truth. These might be left default or mapped if
                        // corresponding claims exist in the Keycloak token.
                        FirstName = context.User.FindFirst(ClaimTypes.GivenName)?.Value,
                        LastName = context.User.FindFirst(ClaimTypes.Surname)?.Value,
                        Password = null, // Password is not managed by Fig
                        PasswordChangeRequired = false 
                    };

                    context.Items["User"] = user;
                    foreach (var service in authenticatedServices)
                        service.SetAuthenticatedUser(user);
                }
            }
        }
        else
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var userId = tokenHandler.Validate(token);
                if (userId != null)
                {
                    // attach user to context on successful jwt validation
                    var user = await userService.GetById(userId.Value);
                    context.Items["User"] = user;
                    foreach (var service in authenticatedServices)
                        service.SetAuthenticatedUser(user);
                }
            }
            catch (UnknownUserException)
            {
                throw new UnauthorizedAccessException("User in provided authorization token is not known");
            }
        }

        await _next(context);
    }
}