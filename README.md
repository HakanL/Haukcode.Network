# Haukcode.Network [![NuGet Version](http://img.shields.io/nuget/v/Haukcode.Network.svg?style=flat)](https://www.nuget.org/packages/Haukcode.Network/)

A .NET library providing common network utilities and efficient binary I/O operations for network programming.

## Features

### Network Utilities
- **Network Adapter Discovery** - Enumerate and query network interfaces with detailed information
- **IP Address Management** - Get IPv4 addresses, netmasks, and MAC addresses from adapters
- **Multicast Support** - Generate and validate multicast addresses
- **Broadcast Addresses** - Calculate broadcast addresses from IP and netmask
- **Socket Configuration** - Helper methods for socket options (reuse, non-exclusive, ICMP)

### Binary I/O
- **Big-Endian Binary Reader/Writer** - Efficient binary serialization in network byte order
- **Little-Endian Binary Reader/Writer** - Binary serialization in little-endian format
- **Memory-Efficient** - Built on `Memory<T>` and `Span<T>` for zero-copy operations
- **Type Support** - Int16, UInt16, Int32, UInt32, bytes, strings, GUIDs

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Haukcode.Network
```

Or via Package Manager Console:

```powershell
Install-Package Haukcode.Network
```

## Usage

### Network Adapter Discovery

#### Get All Common Network Adapters (Ethernet and WiFi)

```csharp
using Haukcode.Network;
using System;

// Get all ethernet and WiFi adapters
var adapters = Utils.GetCommonAdapters();

foreach (var adapter in adapters)
{
    Console.WriteLine($"Adapter: {adapter.DisplayName}");
    Console.WriteLine($"  Type: {adapter.Type}");
    Console.WriteLine($"  MAC: {BitConverter.ToString(adapter.PhysicalAddress)}");
    
    foreach (var (ip, netmask) in adapter.AllIpv4Addresses)
    {
        Console.WriteLine($"  IP: {ip}, Netmask: {netmask}");
    }
}
```

#### Get First Available Bind Address

```csharp
using Haukcode.Network;
using System;

// Get the first available IP address (prefers Ethernet over WiFi)
var (ipAddress, netMask, macAddress) = Utils.GetFirstBindAddress();

if (ipAddress != null)
{
    Console.WriteLine($"Bind to: {ipAddress}");
    Console.WriteLine($"Netmask: {netMask}");
    Console.WriteLine($"MAC: {BitConverter.ToString(macAddress)}");
}
```

#### Get Network Interfaces with IP Details

```csharp
using Haukcode.Network;

// Get list of interface names with IP addresses
var interfaces = Utils.GetCommonInterfaces(excludeHyperV: true);

foreach (var (adapterName, description, ipAddress, netMask) in interfaces)
{
    Console.WriteLine($"{adapterName} ({description}): {ipAddress}/{netMask}");
}
```

### Multicast and Broadcast Addresses

#### Generate Multicast Address

```csharp
using Haukcode.Network;
using System.Net;

// Generate a multicast address from a universe ID
ushort universeId = 1;
IPAddress multicastAddr = Utils.GetMulticastAddress(universeId);
// Result: 239.255.0.1

Console.WriteLine($"Multicast address for universe {universeId}: {multicastAddr}");

// Check if an address is a multicast address
bool isMulticast = Utils.IsMulticast(multicastAddr);
```

#### Calculate Broadcast Address

```csharp
using Haukcode.Network;
using System.Net;

IPAddress ipAddress = IPAddress.Parse("192.168.1.100");
IPAddress subnetMask = IPAddress.Parse("255.255.255.0");

IPAddress broadcastAddr = Utils.GetBroadcastAddress(ipAddress, subnetMask);
// Result: 192.168.1.255

Console.WriteLine($"Broadcast address: {broadcastAddr}");
```

### Socket Configuration

```csharp
using Haukcode.Network;
using System.Net.Sockets;

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

// Configure socket with recommended options for UDP networking:
// - Non-exclusive address use
// - Address reuse enabled
// - ICMP unreachable suppressed (Windows)
Utils.SetSocketOptions(socket);
```

### Binary Reading and Writing

#### Big-Endian Binary I/O (Network Byte Order)

```csharp
using Haukcode.Network;
using System;

// Writing binary data in big-endian format
var buffer = new byte[1024];
var writer = new BigEndianBinaryWriter(buffer);

writer.WriteUInt16(0x1234);           // Write 2-byte value
writer.WriteUInt32(0x12345678);       // Write 4-byte value
writer.WriteString("Hello", 10);       // Write string with fixed length
writer.WriteGuid(Guid.NewGuid());      // Write GUID
writer.WriteByte(0xFF);                // Write single byte

Console.WriteLine($"Wrote {writer.BytesWritten} bytes");

// Reading binary data in big-endian format
var reader = new BigEndianBinaryReader(buffer);

ushort value16 = reader.ReadUInt16();
uint value32 = reader.ReadUInt32();
string text = reader.ReadString(10);
Guid guid = reader.ReadGuid();
byte byteValue = reader.ReadByte();

Console.WriteLine($"Read {reader.BytesRead} bytes, {reader.BytesLeft} bytes remaining");
```

#### Little-Endian Binary I/O

```csharp
using Haukcode.Network;
using System;

// Writing in little-endian format
var buffer = new byte[256];
var writer = new LittleEndianBinaryWriter(buffer);

writer.WriteInt16(-1234);
writer.WriteUInt32(0xDEADBEEF);
writer.WriteBytes(new byte[] { 0x01, 0x02, 0x03 });

// Reading in little-endian format
var reader = new LittleEndianBinaryReader(buffer);

short signedValue = reader.ReadInt16();
uint unsignedValue = reader.ReadUInt32();
byte[] bytes = reader.ReadBytes(3);
```

#### Working with Memory Slices

```csharp
using Haukcode.Network;
using System;

var data = new byte[100];
var reader = new BigEndianBinaryReader(data);

// Read a slice of data without copying
ReadOnlyMemory<byte> slice = reader.ReadSlice(20);

// Skip bytes
reader.SkipBytes(10);

// Verify expected byte sequence
bool isValid = reader.VerifyBytes(new byte[] { 0x00, 0x01, 0x02 });

// Get remaining unread data
ReadOnlyMemory<byte> remaining = reader.Memory;
```

#### Reverse Byte Order Reading

```csharp
using Haukcode.Network;

var buffer = new byte[4];
var reader = new BigEndianBinaryReader(buffer);

// Read UInt16 in reverse byte order (useful for mixed endianness)
ushort valueReversed = reader.ReadUInt16Reverse();
```

## API Reference

### Utils Class

| Method | Description |
|--------|-------------|
| `GetCommonAdapters(bool excludeHyperV = true)` | Returns ethernet and WiFi network adapters |
| `GetCommonInterfaces(bool excludeHyperV = true)` | Returns list of adapter names with IP addresses |
| `GetFirstBindAddress()` | Gets first available IP address (prefers Ethernet) |
| `GetMulticastAddress(ushort universeId)` | Generates multicast address from universe ID (239.255.x.x) |
| `IsMulticast(IPAddress address)` | Checks if address is a multicast address |
| `GetBroadcastAddress(IPAddress address, IPAddress subnetMask)` | Calculates broadcast address |
| `SetSocketOptions(Socket socket)` | Configures socket with non-exclusive, reuse, and ICMP options |

### Adapter Class

| Property | Description |
|----------|-------------|
| `Type` | Network interface type (Ethernet, Wireless80211, etc.) |
| `Id` | Unique identifier for the adapter |
| `Name` | Adapter name |
| `Description` | Adapter description |
| `DisplayName` | Formatted display name |
| `PhysicalAddress` | MAC address bytes |
| `IsHyperV` | True if this is a Hyper-V virtual adapter |
| `AllIpv4Addresses` | List of all IPv4 addresses and netmasks |

### IBinaryReader Interface

| Method | Description |
|--------|-------------|
| `ReadByte()` | Read single byte |
| `ReadInt16()` / `ReadUInt16()` | Read 16-bit integer |
| `ReadInt32()` / `ReadUInt32()` | Read 32-bit integer |
| `ReadInt16Reverse()` / `ReadUInt16Reverse()` | Read 16-bit integer with reversed byte order |
| `ReadBytes(int count)` | Read byte array |
| `ReadString(int length)` | Read null-terminated string with max length |
| `ReadString()` | Read null-terminated string |
| `ReadGuid()` | Read GUID (16 bytes) |
| `ReadSlice(int bytes)` | Read memory slice without copying |
| `SkipBytes(int count)` | Skip bytes |
| `VerifyBytes(byte[] expected)` | Verify expected byte sequence |

| Property | Description |
|----------|-------------|
| `BytesRead` | Number of bytes read so far |
| `BytesLeft` | Number of bytes remaining |
| `Memory` | Remaining unread memory |

### IBinaryWriter Interface

| Method | Description |
|--------|-------------|
| `WriteByte(byte value)` | Write single byte |
| `WriteInt16(short value)` / `WriteUInt16(ushort value)` | Write 16-bit integer |
| `WriteInt32(int value)` / `WriteUInt32(uint value)` | Write 32-bit integer |
| `WriteInt16Reverse(short value)` / `WriteUInt16Reverse(ushort value)` | Write 16-bit integer with reversed byte order |
| `WriteBytes(byte[] bytes)` | Write byte array |
| `WriteString(string value, int length)` | Write string with fixed length padding |
| `WriteGuid(Guid value)` | Write GUID (16 bytes) |
| `WriteZeros(int count)` | Write zeros |

| Property | Description |
|----------|-------------|
| `BytesWritten` | Number of bytes written |
| `Memory` | Underlying memory buffer |

## Target Framework

- .NET Standard 2.1

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Hakan Lindestaf

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Repository

https://github.com/HakanL/Haukcode.Network
