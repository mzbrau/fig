using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fig.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ApiSettings _apiSettings;

    public UsersController(
        IUserService userService,
        IOptions<ApiSettings> apiSettings)
    {
        _userService = userService;
        _apiSettings = apiSettings.Value;
    }

    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate(AuthenticateRequestDataContract model)
    {
        if (_apiSettings.UseKeycloak)
        {
            return BadRequest("Authentication is handled by Keycloak when enabled.");
        }
        var response = await _userService.Authenticate(model);
        return Ok(response);
    }

    [Authorize(Role.Administrator)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequestDataContract model)
    {
        if (_apiSettings.UseKeycloak)
        {
            return StatusCode(StatusCodes.Status405MethodNotAllowed, "User registration is handled by Keycloak when enabled.");
        }
        var id = await _userService.Register(model);
        return Ok(id);
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Read operations can remain. If Keycloak is the source of truth, this might list users synced from Keycloak or be empty.
        var users = await _userService.GetAll();
        return Ok(users);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Read operations can remain.
        var user = await _userService.GetById(id);
        return Ok(user);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequestDataContract model)
    {
        if (_apiSettings.UseKeycloak)
        {
            // User updates (especially roles/passwords) should be managed in Keycloak.
            // Specific profile updates might be allowed if Fig stores additional user data.
            // For now, disabling modifications.
            return StatusCode(StatusCodes.Status405MethodNotAllowed, "User updates should be managed in Keycloak when enabled.");
        }
        await _userService.Update(id, model);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (_apiSettings.UseKeycloak)
        {
            return StatusCode(StatusCodes.Status405MethodNotAllowed, "User deletion is handled by Keycloak when enabled.");
        }
        await _userService.Delete(id);
        return Ok();
    }
}