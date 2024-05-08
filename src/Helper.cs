using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Haukcode.Network
{
    public static class Helper
    {
        public static IEnumerable<Adapter> GetAddressesFromInterfaceType(NetworkInterfaceType[] interfaceTypes, bool excludeHyperV = true)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && interfaceTypes.Contains(adapter.NetworkInterfaceType) &&
                    (adapter.OperationalStatus == OperationalStatus.Up || adapter.OperationalStatus == OperationalStatus.Unknown))
                {
#if DEBUG
                    if (adapter.Name.Contains("Docker"))
                        // Skip Docker virtual adapters
                        continue;
#endif
                    var result = new Adapter(adapter);
                    if (excludeHyperV && result.IsHyperV)
                        continue;

                    // Only include adapters with IPv4 address(es)
                    if (!result.AllIpv4Addresses.Any())
                        continue;

                    yield return result;
                }
            }
        }

        /// <summary>
        /// Return list of ethernet and WiFi network adapters
        /// </summary>
        /// <returns>List of name and IPAddress</returns>
        public static IList<(string AdapterName, string Description, IPAddress IPAddress, IPAddress NetMask)> GetCommonInterfaces(bool excludeHyperV = true)
        {
            var list = new List<(string AdapterName, string Description, IPAddress IPAddress, IPAddress NetMask)>();

            foreach (var adapter in GetCommonAdapters(excludeHyperV))
                list.AddRange(adapter.AllIpv4Addresses.Select(x => (adapter.Name, adapter.Description, x.IP, x.NetMask)));

            return list;
        }

        /// <summary>
        /// Return list of ethernet and WiFi network adapters
        /// </summary>
        /// <returns>List of name and IPAddress</returns>
        public static IList<Adapter> GetCommonAdapters(bool excludeHyperV = true)
        {
            var adapters = GetAddressesFromInterfaceType(new NetworkInterfaceType[] { NetworkInterfaceType.Ethernet, NetworkInterfaceType.Wireless80211 }, excludeHyperV);

            return adapters.ToList();
        }

        /// <summary>
        /// Find first matching local IPAddress, first ethernet, then WiFi
        /// </summary>
        /// <returns>Local IPAddress, netmask, mac</returns>
        public static (IPAddress? IPAddress, IPAddress? NetMask, byte[]? MacAddress) GetFirstBindAddress()
        {
            var adapters = GetCommonAdapters();

            // Try Ethernet first
            var firstEthernetAdapter = adapters.FirstOrDefault(x => x.Type == NetworkInterfaceType.Ethernet);
            if (firstEthernetAdapter != null)
            {
                var firstIpv4 = firstEthernetAdapter.AllIpv4Addresses.First();

                return (firstIpv4.IP, firstIpv4.NetMask, firstEthernetAdapter.PhysicalAddress);
            }

            // Then Wifi
            var firstWifiAdapter = adapters.FirstOrDefault(x => x.Type == NetworkInterfaceType.Wireless80211);
            if (firstWifiAdapter != null)
            {
                var firstIpv4 = firstWifiAdapter.AllIpv4Addresses.First();

                return (firstIpv4.IP, firstIpv4.NetMask, firstWifiAdapter.PhysicalAddress);
            }

            return (null, null, null);
        }
    }
}
