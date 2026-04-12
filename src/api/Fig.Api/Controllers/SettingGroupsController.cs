using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingGroups;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("settinggroups")]
public class SettingGroupsController : ControllerBase
{
    private readonly ILogger<SettingGroupsController> _logger;
    private readonly ISettingGroupService _settingGroupService;

    public SettingGroupsController(ILogger<SettingGroupsController> logger, ISettingGroupService settingGroupService)
    {
        _logger = logger;
        _settingGroupService = settingGroupService;
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAllGroups()
    {
        var groups = await _settingGroupService.GetAllGroups();
        return Ok(groups);
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(Guid id)
    {
        var group = await _settingGroupService.GetGroup(id);
        return Ok(group);
    }

    [Authorize(Role.Administrator)]
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] SettingGroupDataContract group)
    {
        var result = await _settingGroupService.CreateGroup(group);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] SettingGroupDataContract group)
    {
        var result = await _settingGroupService.UpdateGroup(id, group);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        await _settingGroupService.DeleteGroup(id);
        return Ok();
    }
}
