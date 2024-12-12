using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Haukcode.Network
{
    public interface IBinaryWriter
    {
        int BytesWritten { get; }

        Memory<byte> Memory { get; }

        void WriteByte(byte value);

        void WriteInt16(short value);

        void WriteUInt16(ushort value);

        void WriteInt16Reverse(short value);

        void WriteUInt16Reverse(ushort value);

        void WriteInt32(int value);

        void WriteUInt32(uint value);

        void WriteBytes(byte[] bytes);

        void WriteBytes(ReadOnlyMemory<byte> bytes);

        void WriteString(string value, int length);

        void WriteGuid(Guid value);
    }
}
