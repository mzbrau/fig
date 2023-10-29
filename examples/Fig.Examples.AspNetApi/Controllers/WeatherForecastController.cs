using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fig.Examples.AspNetApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IOptionsMonitor<Settings> _settings;
    private readonly IOptionsMonitor<OtherSettings> _otherSettings;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IOptionsMonitor<Settings> settings, IOptionsMonitor<OtherSettings> otherSettings)
    {
        _logger = logger;
        _settings = settings;
        _otherSettings = otherSettings;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        _logger.LogInformation("Getting weather forecast");
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                Location = _settings.CurrentValue.Location
            })
            .ToArray();
    }
}