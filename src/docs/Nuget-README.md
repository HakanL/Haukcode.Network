# Haukcode.Network

A .NET library providing common network utilities and efficient binary I/O operations for network programming.

## Quick Start

### Installation

```bash
dotnet add package Haukcode.Network
```

## Features

- **Network Adapter Discovery** - Query network interfaces, IP addresses, and MAC addresses
- **Binary I/O** - Efficient big-endian and little-endian binary readers/writers
- **Network Utilities** - Multicast addresses, broadcast addresses, socket configuration

## Usage Examples

### Get Network Adapters

```csharp
using Haukcode.Network;

// Get first available IP address
var (ipAddress, netMask, macAddress) = Utils.GetFirstBindAddress();

// Get all adapters
var adapters = Utils.GetCommonAdapters();
foreach (var adapter in adapters)
{
    Console.WriteLine($"{adapter.DisplayName}: {adapter.AllIpv4Addresses[0].IP}");
}
```

### Binary I/O (Network Byte Order)

```csharp
using Haukcode.Network;

// Write binary data
var buffer = new byte[1024];
var writer = new BigEndianBinaryWriter(buffer);
writer.WriteUInt16(0x1234);
writer.WriteUInt32(0x12345678);
writer.WriteString("Hello", 10);

// Read binary data
var reader = new BigEndianBinaryReader(buffer);
ushort value16 = reader.ReadUInt16();
uint value32 = reader.ReadUInt32();
string text = reader.ReadString(10);
```

### Network Utilities

```csharp
using Haukcode.Network;
using System.Net;

// Generate multicast address
IPAddress multicast = Utils.GetMulticastAddress(1); // 239.255.0.1

// Calculate broadcast address
IPAddress broadcast = Utils.GetBroadcastAddress(
    IPAddress.Parse("192.168.1.100"),
    IPAddress.Parse("255.255.255.0")); // 192.168.1.255

// Configure socket
var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
Utils.SetSocketOptions(socket);
```

## Key APIs

**Network Utilities:**
- `Utils.GetCommonAdapters()` - Get ethernet and WiFi adapters
- `Utils.GetFirstBindAddress()` - Get first available IP address
- `Utils.GetMulticastAddress(universeId)` - Generate multicast address
- `Utils.GetBroadcastAddress(ip, netmask)` - Calculate broadcast address
- `Utils.SetSocketOptions(socket)` - Configure socket options

**Binary I/O:**
- `BigEndianBinaryReader` / `BigEndianBinaryWriter` - Network byte order
- `LittleEndianBinaryReader` / `LittleEndianBinaryWriter` - Little-endian
- Supports: Int16, UInt16, Int32, UInt32, bytes, strings, GUIDs
- Memory-efficient with `Memory<T>` and `Span<T>`

## Documentation

For complete documentation, examples, and API reference, visit:
https://github.com/HakanL/Haukcode.Network

## License

MIT License - Copyright (c) 2024 Hakan Lindestaf
