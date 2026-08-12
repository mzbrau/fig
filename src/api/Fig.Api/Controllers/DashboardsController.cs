using Fig.Api.Attributes;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.Dashboards;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("dashboards")]
public class DashboardsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly, Role.Dashboard)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var dashboards = await _dashboardService.GetAll();
        return Ok(dashboards);
    }

    [Authorize(Role.Administrator, Role.User, Role.ReadOnly, Role.Dashboard)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var dashboard = await _dashboardService.Get(id);
        return Ok(dashboard);
    }

    [Authorize(Role.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DashboardDataContract dashboard)
    {
        var result = await _dashboardService.Create(dashboard);
        return Ok(result);
    }

    [Authorize(Role.Administrator)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] DashboardDataContract dashboard,
        [FromQuery] bool forceOverwrite = false)
    {
        try
        {
            var result = await _dashboardService.Update(id, dashboard, forceOverwrite);
            return Ok(result);
        }
        catch (DashboardConcurrencyException ex)
        {
            return Conflict(ex.Current);
        }
    }

    [Authorize(Role.Administrator)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _dashboardService.Delete(id);
        return Ok();
    }
}
