using System.Collections.Generic;

namespace Fig.Contracts.Health;

public class HealthDataContract
{
    public FigHealthStatus Status { get; set; }
    public List<ComponentHealthDataContract> Components { get; set; } = new();
}