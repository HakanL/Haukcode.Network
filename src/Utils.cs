using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Haukcode.Network
{
    public static class Utils
    {
        public const byte MULTICAST_BYTE_1 = (byte)239;
        public const byte MULTICAST_BYTE_2 = (byte)255;

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

        /// <summary>
        /// Get Multicast address from universe id
        /// </summary>
        /// <param name="universeId">Universe Id</param>
        /// <returns></returns>
        public static IPAddress GetMulticastAddress(ushort universeId)
        {
            byte highUniverseId = (byte)(universeId >> 8);
            byte lowUniverseId = (byte)(universeId & 0xFF);
            var multicastAddress = new IPAddress(new byte[] { MULTICAST_BYTE_1, MULTICAST_BYTE_2, highUniverseId, lowUniverseId });

            return multicastAddress;
        }

        public static bool IsMulticast(IPAddress address)
        {
            byte[] addressBytes = address.GetAddressBytes();

            return addressBytes[0] == MULTICAST_BYTE_1 && addressBytes[1] == MULTICAST_BYTE_2;
        }

        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddress);
        }

        /// <summary>
        /// Sets the non-exclusive, reuse and ICMP options
        /// </summary>
        /// <param name="socket">Socket</param>
        public static void SetSocketOptions(Socket socket)
        {
            // Set the SIO_UDP_CONNRESET ioctl to true for this UDP socket. If this UDP socket
            //    ever sends a UDP packet to a remote destination that exists but there is
            //    no socket to receive the packet, an ICMP port unreachable message is returned
            //    to the sender. By default, when this is received the next operation on the
            //    UDP socket that send the packet will receive a SocketException. The native
            //    (Winsock) error that is received is WSAECONNRESET (10054). Since we don't want
            //    to wrap each UDP socket operation in a try/except, we'll disable this error
            //    for the socket with this ioctl call.
            try
            {
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

                byte[] optionInValue = { Convert.ToByte(false) };
                byte[] optionOutValue = new byte[4];
                socket.IOControl((int)SIO_UDP_CONNRESET, optionInValue, optionOutValue);
            }
            catch
            {
                Debug.WriteLine("Unable to set SIO_UDP_CONNRESET, maybe not supported.");
            }

            socket.ExclusiveAddressUse = false;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }
    }
}
