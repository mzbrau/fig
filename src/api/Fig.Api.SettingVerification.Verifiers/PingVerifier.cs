using System.Net.NetworkInformation;
using Fig.Api.SettingVerification.Sdk;

namespace Fig.Api.SettingVerification.Verifiers;

public class PingVerifier : ISettingVerifier
{
    public string Name => "PingVerifier";

    public string Description => "Pings an address using the default ping implementation. " +
                                 "If the address responds, it is a pass, otherwise fail.";
    
    public VerificationResult RunVerification(params object[] parameters)
    {
        try
        {
            var address = parameters.IndexAs<string>(0);
            return PerformPing(address);
        }
        catch (Exception)
        {
            return VerificationResult.IncorrectParameters();
        }
    }

    private VerificationResult PerformPing(string address)
    {
        var result = new VerificationResult();

        var pingSender = new Ping();
        result.AddLog($"Pinging address {address}...");
        var reply = pingSender.Send(address!);
        if (reply.Status == IPStatus.Success)
        {
            result.AddLog($"Address: {reply.Address}");
            result.AddLog($"RoundTrip time: {reply.RoundtripTime}");
            result.AddLog($"Time to live: {reply.Options?.Ttl}");
            result.AddLog($"Don't fragment: {reply.Options?.DontFragment}");
            result.AddLog($"Buffer size: {reply.Buffer.Length}");
            result.Success = true;
        }
        else
        {
            result.AddLog($"Address: {reply.Status}");
        }

        return result;
    }
}