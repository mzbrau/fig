namespace Fig.Web;

public class WebSettings
{
    public string ApiUri { get; set; } = "https://localhost:7281";
    
    public string? SentryDsn { get; set; }

    public double SentrySampleRate { get; set; } = 0;
    
    public string? Environment { get; set; }

    public bool SentryInUse => !string.IsNullOrWhiteSpace(SentryDsn);
}