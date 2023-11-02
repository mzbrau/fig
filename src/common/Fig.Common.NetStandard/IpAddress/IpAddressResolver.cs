using System.Net;
using System.Net.Sockets;

namespace Fig.Common.NetStandard.IpAddress
{
    public class IpAddressResolver : IIpAddressResolver
    {
        public string Resolve()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();

            return string.Empty;
        }
    }
}