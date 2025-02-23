using System.Net;
using System.Net.Sockets;

namespace Fig.Common.NetStandard.IpAddress
{
    public class IpAddressResolver : IIpAddressResolver
    {
        public string Resolve()
        {
            IPAddress[] addresses;
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                addresses = host.AddressList;
            }
            catch (SocketException)
            {
                addresses = Dns.GetHostAddresses("localhost");
            }
            
            foreach (var ip in addresses)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            
            return string.Empty;
        }
    }
}