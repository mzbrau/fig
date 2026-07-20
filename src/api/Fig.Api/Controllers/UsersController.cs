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
    private readonly IOptions<ApiSettings> _apiSettings;
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService,
        IOptions<ApiSettings> apiSettings)
    {
        _userService = userService;
        _apiSettings = apiSettings;
    }

    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate(AuthenticateRequestDataContract model)
    {
        if (IsKeycloakMode())
            return NotFound();

        var response = await _userService.Authenticate(model);
        return Ok(response);
    }

    [Authorize(Role.Administrator)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequestDataContract model)
    {
        if (IsKeycloakMode())
            return NotFound();

        var id = await _userService.Register(model);
        return Ok(id);
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (IsKeycloakMode())
            return NotFound();

        var users = await _userService.GetAll();
        return Ok(users);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (IsKeycloakMode())
            return NotFound();

        var user = await _userService.GetById(id);
        return Ok(user);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequestDataContract model)
    {
        if (IsKeycloakMode())
            return NotFound();

        await _userService.Update(id, model);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (IsKeycloakMode())
            return NotFound();

        await _userService.Delete(id);
        return Ok();
    }

    private bool IsKeycloakMode()
    {
        return _apiSettings.Value.Authentication.Mode == AuthMode.Keycloak;
    }
}