using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate(AuthenticateRequestDataContract model)
    {
        var response = await _userService.Authenticate(model);
        return Ok(response);
    }

    [Authorize(Role.Administrator)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequestDataContract model)
    {
        var id = await _userService.Register(model);
        return Ok(id);
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAll();
        return Ok(users);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetById(id);
        return Ok(user);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequestDataContract model)
    {
        await _userService.Update(id, model);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.Delete(id);
        return Ok();
    }
}