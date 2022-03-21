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
    public IActionResult Authenticate(AuthenticateRequestDataContract model)
    {
        var response = _userService.Authenticate(model);
        return Ok(response);
    }

    [Authorize(Role.Administrator)]
    [HttpPost("register")]
    public IActionResult Register(RegisterUserRequestDataContract model)
    {
        var id = _userService.Register(model);
        return Ok(id);
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _userService.GetAll();
        return Ok(users);
    }

    [Authorize(Role.Administrator)]
    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var user = _userService.GetById(id);
        return Ok(user);
    }

    [Authorize(Role.Administrator, Role.User)]
    [HttpPut("{id}")]
    public IActionResult Update(Guid id, UpdateUserRequestDataContract model)
    {
        _userService.Update(id, model);
        return Ok();
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _userService.Delete(id);
        return Ok();
    }
}