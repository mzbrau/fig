using System.Text;
using Fig.Contracts.Health;

namespace Fig.Api.ExtensionMethods;

public static class HealthDataContractExtensions
{
    public static string Summary(this HealthDataContract health)
    {
        var builder = new StringBuilder();
        foreach (var component in health.Components)
        {
            builder.AppendLine($"{component.Name}: {component.Status}");
            if (component.Status != FigHealthStatus.Healthy)
            {
                builder.AppendLine($"  Message: {component.Message}");
            }
        }
        
        return builder.ToString();
    }
}