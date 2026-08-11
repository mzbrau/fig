using Fig.Client.Abstractions.StatusProperties;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Examples.AspNetApi.Controllers;

/// <summary>
/// Interactive endpoints for testing Custom Status Properties via Swagger.
/// Changes appear on Fig Connected Clients after the next client status poll.
/// </summary>
[ApiController]
[Route("[controller]")]
public class StatusPropertiesController : ControllerBase
{
    private readonly IFigStatusProperties<AspNetApiStatusProperties> _statusProperties;
    private readonly ILogger<StatusPropertiesController> _logger;

    public StatusPropertiesController(
        IFigStatusProperties<AspNetApiStatusProperties> statusProperties,
        ILogger<StatusPropertiesController> logger)
    {
        _statusProperties = statusProperties;
        _logger = logger;
    }

    /// <summary>Returns a clone of the current in-memory status properties.</summary>
    [HttpGet]
    public ActionResult<AspNetApiStatusProperties> Get()
    {
        return Ok(_statusProperties.Current);
    }

    /// <summary>Sets the worker phase (and optionally a highlight-related region) via Set.</summary>
    [HttpPost("set-phase")]
    public ActionResult<AspNetApiStatusProperties> SetPhase([FromBody] SetPhaseRequest request)
    {
        _statusProperties.Set(x => x.Phase, request.Phase);
        if (!string.IsNullOrWhiteSpace(request.Region))
            _statusProperties.Set(x => x.Region, request.Region);

        _logger.LogInformation("StatusProperties Set phase={Phase} region={Region}", request.Phase, request.Region);
        return Ok(_statusProperties.Current);
    }

    /// <summary>Updates several fields in one call via Update.</summary>
    [HttpPost("update")]
    public ActionResult<AspNetApiStatusProperties> Update([FromBody] UpdateStatusRequest request)
    {
        _statusProperties.Update(x =>
        {
            if (request.IsHealthy.HasValue)
                x.IsHealthy = request.IsHealthy.Value;
            if (request.QueueDepth.HasValue)
                x.QueueDepth = request.QueueDepth.Value;
            if (request.CpuSample.HasValue)
                x.CpuSample = request.CpuSample.Value;
            if (request.UnitCost.HasValue)
                x.UnitCost = request.UnitCost.Value;
            if (request.ContextJson is not null)
                x.ContextJson = request.ContextJson;
            if (request.Region is not null)
                x.Region = request.Region;
            x.LastTickUtc = DateTime.UtcNow;
        });

        _logger.LogInformation("StatusProperties Update applied");
        return Ok(_statusProperties.Current);
    }

    /// <summary>Sets Usage with a matching hex text colour via Set(value, textColor).</summary>
    [HttpPost("set-usage")]
    public ActionResult<AspNetApiStatusProperties> SetUsage([FromBody] SetUsageRequest request)
    {
        var level = string.IsNullOrWhiteSpace(request.Usage) ? "NORMAL" : request.Usage.Trim().ToUpperInvariant();
        var color = request.TextColor ?? level switch
        {
            "HIGH" => "#E53935",
            "LOW" => "#43A047",
            _ => "#FB8C00"
        };

        _statusProperties.Set(x => x.Usage, level, color);
        _logger.LogInformation("StatusProperties Set usage={Usage} textColor={TextColor}", level, color);
        return Ok(_statusProperties.Current);
    }

    /// <summary>Clears LastErrorUtc via Clear (nullable → null).</summary>
    [HttpPost("clear-error")]
    public ActionResult<AspNetApiStatusProperties> ClearError()
    {
        _statusProperties.Clear(x => x.LastErrorUtc);
        _logger.LogInformation("StatusProperties Clear LastErrorUtc");
        return Ok(_statusProperties.Current);
    }
}

public sealed class SetPhaseRequest
{
    public WorkerPhase Phase { get; set; } = WorkerPhase.Processing;
    public string? Region { get; set; }
}

public sealed class SetUsageRequest
{
    public string Usage { get; set; } = "HIGH";
    public string? TextColor { get; set; }
}

public sealed class UpdateStatusRequest
{
    public bool? IsHealthy { get; set; }
    public int? QueueDepth { get; set; }
    public double? CpuSample { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ContextJson { get; set; }
    public string? Region { get; set; }
}
