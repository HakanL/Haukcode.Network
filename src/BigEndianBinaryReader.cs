using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Haukcode.Network
{
    public class BigEndianBinaryReader : IBinaryReader
    {
        private int readPosition = 0;
        private readonly ReadOnlyMemory<byte> buffer;

        public BigEndianBinaryReader(ReadOnlyMemory<byte> buffer)
        {
            this.buffer = buffer;
        }

        public int BytesRead => this.readPosition;

        public int BytesLeft => this.buffer.Length - this.readPosition;

        public ReadOnlyMemory<byte> Memory => this.buffer[this.readPosition..];

        public short ReadInt16()
        {
            var span = this.buffer.Span;
            byte b1 = span[this.readPosition++];
            byte b2 = span[this.readPosition++];

            return (short)((b1 << 8) | b2);
        }

        public ushort ReadUInt16()
        {
            var span = this.buffer.Span;
            byte b1 = span[this.readPosition++];
            byte b2 = span[this.readPosition++];

            return (ushort)((b1 << 8) | b2);
        }

        public short ReadInt16Reverse()
        {
            var span = this.buffer.Span;
            byte b2 = span[this.readPosition++];
            byte b1 = span[this.readPosition++];

            return (short)((b1 << 8) | b2);
        }

        public ushort ReadUInt16Reverse()
        {
            var span = this.buffer.Span;
            byte b2 = span[this.readPosition++];
            byte b1 = span[this.readPosition++];

            return (ushort)((b1 << 8) | b2);
        }

        public int ReadInt32()
        {
            var span = this.buffer.Span;
            byte b1 = span[this.readPosition++];
            byte b2 = span[this.readPosition++];
            byte b3 = span[this.readPosition++];
            byte b4 = span[this.readPosition++];

            return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
        }

        public uint ReadUInt32()
        {
            var span = this.buffer.Span;
            byte b1 = span[this.readPosition++];
            byte b2 = span[this.readPosition++];
            byte b3 = span[this.readPosition++];
            byte b4 = span[this.readPosition++];

            return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
        }

        public ReadOnlyMemory<byte> ReadSlice(int bytes)
        {
            var span = this.buffer.Slice(this.readPosition, bytes);

            this.readPosition += bytes;

            return span;
        }

        public ReadOnlyMemory<byte> ReadSlice()
        {
            var span = this.buffer[this.readPosition..];

            this.readPosition += span.Length;

            return span;
        }

        public void SkipBytes(int count)
        {
            this.readPosition += count;
        }

        public byte[] ReadBytes(int bytes)
        {
            return ReadSlice(bytes).ToArray();
        }

        public byte[] ReadBytes()
        {
            return ReadSlice().ToArray();
        }

        public bool VerifyBytes(byte[] bytes)
        {
            var span = this.buffer.Slice(this.readPosition, bytes.Length).Span;
            this.readPosition += bytes.Length;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (span[i] != bytes[i])
                    return false;
            }

            return true;
        }

        public Guid ReadGuid()
        {
            var result = new Guid(this.buffer[this.readPosition..(this.readPosition + 16)].Span);

            this.readPosition += 16;

            return result;
        }

        public string ReadString(int bytes)
        {
            var span = this.buffer.Slice(this.readPosition, bytes).Span;
            int terminatorIndex = span.IndexOf((byte)0);
            if (terminatorIndex == -1)
                terminatorIndex = bytes;

            this.readPosition += bytes;

            return Encoding.UTF8.GetString(span[..terminatorIndex]);
        }

        public string ReadString()
        {
            var span = this.buffer.Slice(this.readPosition).Span;
            int terminatorIndex = span.IndexOf((byte)0);
            if (terminatorIndex == -1)
                terminatorIndex = this.buffer.Length - this.readPosition;

            this.readPosition += terminatorIndex + 1;

            return Encoding.UTF8.GetString(span[..terminatorIndex]);
        }

        public byte ReadByte()
        {
            return this.buffer.Span[this.readPosition++];
        }
    }
}
