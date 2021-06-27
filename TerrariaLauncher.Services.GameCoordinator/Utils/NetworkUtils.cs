using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class NetworkUtils
    {
        public static async Task<IPAddress> GetIPv4(string host)
        {
            if (IPAddress.TryParse(host, out var parsedIpAddress))
            {
                if (parsedIpAddress.AddressFamily != AddressFamily.InterNetwork)
                {
                    throw new FormatException($"[{host}] is not IPv4 address.");
                }

                return parsedIpAddress;
            }
            else
            {
                var ipHostEntry = await Dns.GetHostEntryAsync(host);
                foreach (var ipAddress in ipHostEntry.AddressList)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ipAddress;
                    }
                }

                throw new FormatException($"Could not resolve IPv4 from '{host}'.");
            }
        }
    }
}
