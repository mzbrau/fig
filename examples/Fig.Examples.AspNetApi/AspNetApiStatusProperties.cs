using Fig.Client.Abstractions.StatusProperties;

namespace Fig.Examples.AspNetApi;

/// <summary>
/// Demo bag for Custom Status Properties.
/// In Fig.Web Connected Clients: Highlight properties appear in the collapsed column;
/// other ShowInUi properties appear only when the row is expanded; ShowInUi=false is REST/MCP only.
/// Drive values via StatusPropertiesUpdater or Swagger (/StatusProperties).
/// </summary>
public class AspNetApiStatusProperties
{
    // --- Highlighted (collapsed Connected Clients column) ---

    [StatusProperty(DisplayName = "Last Tick", Highlight = true, Order = 1)]
    public DateTime? LastTickUtc { get; set; }

    [StatusProperty(DisplayName = "Tick Count", Highlight = true, Order = 2)]
    public long TickCount { get; set; }

    [StatusProperty(DisplayName = "Phase", Highlight = true, Order = 3)]
    public WorkerPhase Phase { get; set; } = WorkerPhase.Idle;

    [StatusProperty(DisplayName = "Usage", Highlight = true, Order = 4)]
    public string Usage { get; set; } = "NORMAL";

    // --- Expand panel only (ShowInUi default, not highlighted) ---

    [StatusProperty(DisplayName = "Uptime")]
    public TimeSpan? Uptime { get; set; }

    [StatusProperty(DisplayName = "Healthy")]
    public bool IsHealthy { get; set; } = true;

    [StatusProperty(DisplayName = "Queue Depth", Order = 10)]
    public int QueueDepth { get; set; }

    [StatusProperty(DisplayName = "CPU Sample")]
    public double CpuSample { get; set; }

    [StatusProperty(DisplayName = "Unit Cost")]
    public decimal UnitCost { get; set; }

    [StatusProperty(DisplayName = "Last Sync Offset")]
    public DateTimeOffset? LastSyncOffset { get; set; }

    [StatusProperty(DisplayName = "Business Date")]
    public DateOnly? BusinessDate { get; set; }

    [StatusProperty(DisplayName = "Shift Start")]
    public TimeOnly? ShiftStart { get; set; }

    [StatusProperty(DisplayName = "Session Id")]
    public Guid SessionId { get; set; }

    [StatusProperty(DisplayName = "Last Error")]
    public DateTime? LastErrorUtc { get; set; }

    [StatusProperty(DisplayName = "Extra Context")]
    public string? ContextJson { get; set; }

    // Attribute omitted: defaults (ShowInUi=true, Highlight=false, DisplayName=property name)
    public string Region { get; set; } = "local";

    // --- API / MCP only (hidden from Fig.Web and CSV) ---

    [StatusProperty(ShowInUi = false)]
    public string? InternalRunId { get; set; }

    [StatusProperty(DisplayName = "Correlation", ShowInUi = false)]
    public Guid? CorrelationId { get; set; }
}

public enum WorkerPhase
{
    Idle,
    WarmingUp,
    Processing,
    Draining,
    Faulted
}
