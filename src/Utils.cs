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

            // See if there are any HyperV adapters
            adapters = GetCommonAdapters(excludeHyperV: false);
            firstEthernetAdapter = adapters.FirstOrDefault(x => x.Type == NetworkInterfaceType.Ethernet);
            if (firstEthernetAdapter != null)
            {
                var firstIpv4 = firstEthernetAdapter.AllIpv4Addresses.First();

                return (firstIpv4.IP, firstIpv4.NetMask, firstEthernetAdapter.PhysicalAddress);
            }

            // Then just any with IPv4 address
            var firstAdapter = adapters.FirstOrDefault(x => x.AllIpv4Addresses.Any());
            if (firstAdapter != null)
            {
                var firstIpv4 = firstAdapter.AllIpv4Addresses.First();

                return (firstIpv4.IP, firstIpv4.NetMask, firstAdapter.PhysicalAddress);
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

        /// <summary>
        /// Check if the address is a multicast address (224.0.0.0/4 for IPv4, ff00::/8 for IPv6)
        /// </summary>
        public static bool IsMulticast(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                return address.IsIPv6Multicast;

            byte[] addressBytes = address.GetAddressBytes();

            return (addressBytes[0] & 0xF0) == 0xE0;
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

        [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
        private static extern int setsockopt(int socket, int level, int optionName, byte[] optionValue, uint optionLength);

        // Linux: setsockopt(SOL_SOCKET, SO_BINDTODEVICE, "<ifname>\0")
        private const int LinuxSolSocket = 1;
        private const int LinuxSoBindToDevice = 25;

        // macOS: setsockopt(IPPROTO_IP, IP_BOUND_IF, <interface index>)
        private const int MacIpProtoIp = 0;
        private const int MacIpBoundIf = 25;

        /// <summary>
        /// Pin a sending socket to the network interface that owns <paramref name="localAddress"/>,
        /// so every packet leaves through that interface.
        ///
        /// Binding a socket to a local address only sets the source address on Linux and macOS (the
        /// weak host model): the outgoing interface still comes from the routing table. On a host
        /// with two interfaces on the same subnet, or when the other interface has the lower route
        /// metric, unicast from a socket bound to one interface's address leaves through the other
        /// one. Multicast is not affected (the kernel picks the interface of the source address),
        /// unicast is. Windows sends from the interface that owns the source address (strong host
        /// model), so nothing is needed there.
        ///
        /// Call it after <see cref="Socket.Bind"/>. Best effort: when the interface can't be found
        /// or the option is refused (SO_BINDTODEVICE needs CAP_NET_RAW on kernels before 5.7) the
        /// socket keeps working with routing-table egress, as it did before.
        /// </summary>
        /// <returns>The interface the socket was pinned to, or null when it was left as it was</returns>
        public static string? BindToInterfaceOfAddress(Socket socket, IPAddress localAddress)
        {
            if (localAddress == null || IPAddress.Any.Equals(localAddress) || IPAddress.IsLoopback(localAddress))
                return null;

            bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
            bool isMac = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

            if (!isLinux && !isMac)
                return null;

            try
            {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(x => x.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(localAddress)));

                if (networkInterface == null)
                    return null;

                // Socket.SetRawSocketOption is not in netstandard2.1, and SetSocketOption only
                // passes options it knows, so go to libc directly
                int fd = socket.Handle.ToInt32();
                int result;

                if (isLinux)
                {
                    byte[] name = Encoding.ASCII.GetBytes(networkInterface.Name + "\0");
                    result = setsockopt(fd, LinuxSolSocket, LinuxSoBindToDevice, name, (uint)name.Length);
                }
                else
                {
                    byte[] index = BitConverter.GetBytes(networkInterface.GetIPProperties().GetIPv4Properties().Index);
                    result = setsockopt(fd, MacIpProtoIp, MacIpBoundIf, index, (uint)index.Length);
                }

                if (result != 0)
                {
                    Debug.WriteLine($"Unable to pin the socket to {networkInterface.Name}: errno {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");

                    return null;
                }

                return networkInterface.Name;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to pin the socket to the interface of {localAddress}: {ex.Message}");

                return null;
            }
        }
    }
}
