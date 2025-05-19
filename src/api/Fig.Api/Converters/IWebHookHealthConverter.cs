using Fig.Contracts.Health;
using Fig.WebHooks.Contracts;

namespace Fig.Api.Converters;

public interface IWebHookHealthConverter
{
    HealthDetails Convert(HealthDataContract dataContract);

    HealthStatus Convert(FigHealthStatus dataContract);
}