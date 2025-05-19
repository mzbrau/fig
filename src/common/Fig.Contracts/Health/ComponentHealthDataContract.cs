namespace Fig.Contracts.Health;

public record ComponentHealthDataContract(string Name, FigHealthStatus Status, string? Message)
{
    public string Name { get; } = Name;
    
    public FigHealthStatus Status { get; } = Status;
    
    public string? Message { get; } = Message;
}