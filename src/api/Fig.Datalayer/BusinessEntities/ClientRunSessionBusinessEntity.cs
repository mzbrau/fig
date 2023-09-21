using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class ClientRunSessionBusinessEntity
{
    private string? _memoryAnalysisAsJson;
    
    public virtual Guid Id { get; init; }

    public virtual Guid RunSessionId { get; init; }

    public virtual DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public virtual bool? LiveReload { get; set; }

    public virtual double? PollIntervalMs { get; set; }

    public virtual double UptimeSeconds { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual string FigVersion { get; set; } = default!;

    public virtual string ApplicationVersion { get; set; } = default!;

    public virtual bool OfflineSettingsEnabled { get; set; }

    public virtual bool SupportsRestart { get; set; }

    public virtual bool RestartRequested { get; set; }
    
    public virtual bool RestartRequiredToApplySettings { get; set; }

    public virtual string RunningUser { get; set; } = default!;

    public virtual long MemoryUsageBytes { get; set; }
    
    public virtual bool HasConfigurationError { get; set; }

    public virtual MemoryUsageAnalysisBusinessEntity? MemoryAnalysis { get; set; }

    public virtual string? MemoryAnalysisAsJson
    {
        get
        {
            if (MemoryAnalysis is null)
                return null;
            
            _memoryAnalysisAsJson = JsonConvert.SerializeObject(MemoryAnalysis);
            return _memoryAnalysisAsJson;
        }
        set
        {
            if (_memoryAnalysisAsJson != value && value is not null)
                MemoryAnalysis = JsonConvert.DeserializeObject<MemoryUsageAnalysisBusinessEntity>(value);
        }
    }

    public virtual ICollection<MemoryUsageBusinessEntity> HistoricalMemoryUsage { get; set; } =
        new List<MemoryUsageBusinessEntity>();
}