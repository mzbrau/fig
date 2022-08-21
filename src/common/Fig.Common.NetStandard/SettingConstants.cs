namespace Fig.Common;

public class SettingConstants
{
    private const string ConstantIdentifier = "${{{0}}}";
    public static string MachineName => string.Format(ConstantIdentifier, "MachineName");
    public static string Domain => string.Format(ConstantIdentifier,  "Domain");
    public static string User => string.Format(ConstantIdentifier,  "User");
    public static string IpAddress => string.Format(ConstantIdentifier,  "IPAddress");
    public static string ProcessorCount => string.Format(ConstantIdentifier,  "ProcessorCount");
    public static string OsVersion => string.Format(ConstantIdentifier,  "OSVersion");
    
}