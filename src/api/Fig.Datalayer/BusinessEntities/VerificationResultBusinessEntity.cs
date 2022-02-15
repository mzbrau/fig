using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

public class VerificationResultBusinessEntity
{
    private string? _logsAsJson;
    
    public virtual Guid Id { get; set; }
    
    public virtual Guid ClientId { get; set; }
    
    public virtual string VerificationName { get; set; }
    
    public virtual bool Success { get; set; }
        
    public virtual string Message { get; set; }
    
    public virtual string? RequestingUser { get; set; }
    
    public virtual DateTime ExecutionTime { get; set; }

    public virtual List<string> Logs { get; set; }
    
    public virtual string? LogsAsJson
    {
        get
        {
            _logsAsJson = JsonConvert.SerializeObject(Logs);
            return _logsAsJson;
        }
        set
        {
            if (_logsAsJson != value && value != null)
                Logs = JsonConvert.DeserializeObject<List<string>>(value) ?? new List<string>();
        }
    }
}