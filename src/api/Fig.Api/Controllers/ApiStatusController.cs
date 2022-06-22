using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController, Route("apistatus")]
public class ApisController : ControllerBase
{
    private readonly IApiStatusService _apiStatusService;

    public ApisController(IApiStatusService apiStatusService)
    {
        _apiStatusService = apiStatusService;
    }
    
    [Authorize( Role.Administrator)]
    [HttpGet]
    public IActionResult GetAll()
    {
        var allStatuses = _apiStatusService.GetAll();
        return Ok(allStatuses);
    }
}