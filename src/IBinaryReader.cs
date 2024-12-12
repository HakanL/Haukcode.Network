using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Haukcode.Network
{
    public interface IBinaryReader
    {
        ReadOnlyMemory<byte> Memory { get; }

        int BytesRead { get; }

        int BytesLeft { get; }

        short ReadInt16();

        ushort ReadUInt16();

        int ReadInt32();

        uint ReadUInt32();

        byte[] ReadBytes(int bytes);

        byte[] ReadBytes();

        bool VerifyBytes(byte[] bytes);

        Guid ReadGuid();

        string ReadString(int bytes);

        string ReadString();

        byte ReadByte();

        ReadOnlyMemory<byte> ReadSlice(int bytes);

        ReadOnlyMemory<byte> ReadSlice();

        ushort ReadUInt16Reverse();

        short ReadInt16Reverse();

        void SkipBytes(int count);
    }
}
