using Fig.Web.ExtensionMethods;

namespace Fig.Web.Models.Events;

public class EventLogModel
{
    public DateTime Timestamp { get; set; }

    public string? ClientName { get; set; }

    public string? Instance { get; set; }

    public string? SettingName { get; set; }

    public string EventType { get; set; } = null!;

    public string? OriginalValue { get; set; }

    public string? NewValue { get; set; }

    public string? AuthenticatedUser { get; set; }

    public string? VerificationName { get; set; }
    
    public string? Message { get; set; }

    public string? IpAddress { get; set; }

    public string? Hostname { get; set; }
    
    public string ToCsv()
    {
        return $"{Timestamp:yyyy-MM-dd HH:mm:ss}," +
               $"{ClientName.EscapeAndQuote()}," +
               $"{Instance.EscapeAndQuote()}," +
               $"{SettingName.EscapeAndQuote()}," +
               $"{EventType.EscapeAndQuote()}," +
               $"{OriginalValue.EscapeAndQuote()}," +
               $"{NewValue.EscapeAndQuote()}," +
               $"{AuthenticatedUser.EscapeAndQuote()}," +
               $"{VerificationName.EscapeAndQuote()}," +
               $"{Message.EscapeAndQuote()}," +
               $"{IpAddress.EscapeAndQuote()}," +
               $"{Hostname.EscapeAndQuote()}";
    }
    
    public static string CsvHeaders()
    {
        return "Timestamp,ClientName,Instance,SettingName,EventType,OriginalValue,NewValue,AuthenticatedUser,VerificationName,Message,IpAddress,Hostname";
    }
}