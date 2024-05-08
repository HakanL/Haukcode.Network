using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Haukcode.Network
{
    public class Adapter
    {
        private readonly NetworkInterface networkInterface;

        public NetworkInterfaceType Type => this.networkInterface.NetworkInterfaceType;

        public string Id => this.networkInterface.Id;

        public string Name => this.networkInterface.Name;

        public string Description => this.networkInterface.Description;

        public byte[] PhysicalAddress { get; private set; }

        public string DisplayName
        {
            get
            {
                if (Name == Description)
                    return Name;
                else
                    return $"{Name} ({Description})";
            }
        }

        public bool IsHyperV => PhysicalAddress?.Length == 6 && PhysicalAddress[0] == 0x00 && PhysicalAddress[1] == 0x15 && PhysicalAddress[2] == 0x5D;

        public IList<(IPAddress IP, IPAddress NetMask)> AllIpv4Addresses => this.networkInterface.GetIPProperties().UnicastAddresses
            .Where(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(x => (x.Address, x.IPv4Mask))
            .ToList();

        public Adapter(NetworkInterface input)
        {
            this.networkInterface = input;

            PhysicalAddress = input.GetPhysicalAddress().GetAddressBytes();
        }
    }
}
