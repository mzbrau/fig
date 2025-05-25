using Fig.Contracts.Health;
using Fig.WebHooks.Contracts;

namespace Fig.Api.Converters;

public class WebHookHealthConverter : IWebHookHealthConverter
{
    public HealthDetails Convert(HealthDataContract dataContract)
    {
        return new HealthDetails
        {
            Status = Convert(dataContract.Status),
            Components = dataContract.Components.Select(ConvertComponent).ToList()
        };
    }

    public HealthStatus Convert(FigHealthStatus dataContract)
    {
        return dataContract switch
        {
            FigHealthStatus.Healthy => HealthStatus.Healthy,
            FigHealthStatus.Degraded => HealthStatus.Degraded,
            FigHealthStatus.Unhealthy => HealthStatus.Unhealthy,
            _ => throw new ArgumentOutOfRangeException(nameof(dataContract), dataContract, null)
        };
    }
    
    private ComponentHealthDetails ConvertComponent(ComponentHealthDataContract component)
    {
        return new ComponentHealthDetails(
            component.Name,
            Convert(component.Status),
            component.Message);
    }
}