using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Haukcode.Network
{
    public class LittleEndianBinaryWriter
    {
        private int writePosition = 0;
        private readonly Memory<byte> buffer;

        public LittleEndianBinaryWriter(Memory<byte> buffer)
        {
            this.buffer = buffer;
        }

        public int WrittenBytes => this.writePosition;

        public Memory<byte> Memory => this.buffer.Slice(this.writePosition);

        public void WriteByte(byte value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)value;
        }

        public void WriteInt16(short value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)value;
            span[this.writePosition++] = (byte)(value >> 8);
        }

        public void WriteUInt16(ushort value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)value;
            span[this.writePosition++] = (byte)(value >> 8);
        }

        public void WriteInt32(int value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)value;
            span[this.writePosition++] = (byte)(value >> 8);
            span[this.writePosition++] = (byte)(value >> 16);
            span[this.writePosition++] = (byte)(value >> 24);
        }

        public void WriteUInt32(uint value)
        {
            var span = buffer.Span;

            span[this.writePosition++] = (byte)value;
            span[this.writePosition++] = (byte)(value >> 8);
            span[this.writePosition++] = (byte)(value >> 16);
            span[this.writePosition++] = (byte)(value >> 24);
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

        public void WriteString(string value, int length)
        {
            //FIXME
            WriteBytes(Encoding.UTF8.GetBytes(value));
            WriteBytes(Enumerable.Repeat((byte)0, length - value.Length).ToArray());
        }

        private byte[] GuidToByteArray(Guid input)
        {
            var bytes = input.ToByteArray();

            return new byte[] {
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
            };
        }

        public void WriteGuid(Guid value)
        {
            // Fixme
            var bytes = GuidToByteArray(value);

            WriteBytes(bytes);
        }
    }
}
