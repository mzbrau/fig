namespace Fig.Web.Models.Setting;

public class ChangeDetailsModel
{
    public string Message { get; set; } = string.Empty;
    
    public DateTime? ApplyAtUtc { get; set; }
    
    public DateTime? RevertAtUtc { get; set; }
}