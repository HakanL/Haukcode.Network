using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Haukcode.Network
{
    public class BigEndianBinaryWriter : IBinaryWriter
    {
        private int writePosition = 0;
        private readonly Memory<byte> buffer;

        public BigEndianBinaryWriter(Memory<byte> buffer)
        {
            this.buffer = buffer;
        }

        public int BytesWritten => this.writePosition;

        public Memory<byte> Memory => this.buffer.Slice(this.writePosition);

        public void WriteByte(byte value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)value;
        }

        public void WriteInt16(short value)
        {
            var span = buffer.Span;

            // High byte, Low byte
            span[this.writePosition++] = (byte)(value >> 8);
            span[this.writePosition++] = (byte)value;
        }

        public void WriteUInt16(ushort value)
        {
            var span = buffer.Span;

            // High byte, Low byte
            span[this.writePosition++] = (byte)(value >> 8);
            span[this.writePosition++] = (byte)value;
        }

        public void WriteInt16Reverse(short value)
        {
            var span = buffer.Span;

            // Low byte, High byte
            span[this.writePosition++] = (byte)value;
            span[this.writePosition++] = (byte)(value >> 8);
        }

        public void WriteUInt16Reverse(ushort value)
        {
            var span = buffer.Span;

            // Low byte, High byte
            span[this.writePosition++] = (byte)value;
            span[this.writePosition++] = (byte)(value >> 8);
        }

        public void WriteInt32(int value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)(value >> 24);
            span[this.writePosition++] = (byte)(value >> 16);
            span[this.writePosition++] = (byte)(value >> 8);
            span[this.writePosition++] = (byte)value;
        }

        public void WriteUInt32(uint value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)(value >> 24);
            span[this.writePosition++] = (byte)(value >> 16);
            span[this.writePosition++] = (byte)(value >> 8);
            span[this.writePosition++] = (byte)value;
        }

        public void WriteBytes(byte[] bytes)
        {
            bytes.CopyTo(this.buffer[this.writePosition..].Span);

            this.writePosition += bytes.Length;
        }

        public void WriteBytes(ReadOnlyMemory<byte> bytes)
        {
            bytes.Span.CopyTo(this.buffer[this.writePosition..].Span);

            this.writePosition += bytes.Length;
        }

        public void WriteZeros(int count)
        {
            // Clear the section of the buffer directly
            this.buffer[this.writePosition..(this.writePosition + count)].Span.Clear();

            this.writePosition += count;
        }

        public void WriteString(string value, int length)
        {
            // Encode the string directly into the buffer
            int bytesWritten = Encoding.UTF8.GetBytes(value, this.buffer[this.writePosition..(this.writePosition + length)].Span);

            // Fill the remaining bytes with zero
            this.buffer[(this.writePosition + bytesWritten)..(this.writePosition + length)].Span.Clear();

            this.writePosition += length;
        }

        public void WriteGuid(Guid value)
        {
            byte[] bytes = value.ToByteArray();

            WriteBytes(new byte[] {
                bytes[3],
                bytes[2],
                bytes[1],
                bytes[0],

                bytes[5],
                bytes[4],

                bytes[7],
                bytes[6],

                bytes[8],
                bytes[9],

                bytes[10],
                bytes[11],
                bytes[12],
                bytes[13],
                bytes[14],
                bytes[15]
            });
        }
    }
}
