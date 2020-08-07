using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ReliableNetcode.Utils
{
    /// <inheritdoc />
    /// <summary>
    ///     A simple stream implementation for reading/writing from/to byte arrays which can be reused
    /// </summary>
    internal sealed class ByteStream : Stream
    {
        private byte[] _srcByteArray;

        public override long Position { get; set; }

        public override long Length => _srcByteArray.Length;

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanSeek => true;

        /// <summary>
        ///     Set a new byte array for this stream to read from
        /// </summary>
        public void SetStreamSource(byte[] sourceBuffer)
        {
            _srcByteArray = sourceBuffer;
            Position = 0;
        }

        public byte[] ReadBytes(int length)
        {
            var bytes = new byte[length];
            Read(bytes, 0, length);

            return bytes;
        }

        public char ReadChar()
        {
            var c = 0;

            for (var i = 0; i < sizeof(char); i++) c |= ReadByte() << (i << 3);

            return (char) c;
        }

        public char[] ReadChars(int length)
        {
            var chars = new char[length];

            for (var i = 0; i < length; i++)
                chars[i] = ReadChar();

            return chars;
        }

        public string ReadString()
        {
            var len = ReadUInt32();
            var chars = ReadChars((int) len);
            return new string(chars);
        }

        public short ReadInt16()
        {
            var c = 0;

            for (var i = 0; i < sizeof(short); i++) c |= ReadByte() << (i << 3);

            return (short) c;
        }

        public int ReadInt32()
        {
            var c = 0;

            for (var i = 0; i < sizeof(int); i++) c |= ReadByte() << (i << 3);

            return c;
        }

        public long ReadInt64()
        {
            long c = 0;

            for (var i = 0; i < sizeof(long); i++) c |= (long) ReadByte() << (i << 3);

            return c;
        }

        public ushort ReadUInt16()
        {
            ushort c = 0;

            for (var i = 0; i < sizeof(ushort); i++) c |= (ushort) (ReadByte() << (i << 3));

            return c;
        }

        public uint ReadUInt32()
        {
            uint c = 0;

            for (var i = 0; i < sizeof(uint); i++) c |= (uint) ReadByte() << (i << 3);

            return c;
        }

        public ulong ReadUInt64()
        {
            ulong c = 0;

            for (var i = 0; i < sizeof(ulong); i++) c |= (ulong) ReadByte() << (i << 3);

            return c;
        }

        public float ReadSingle()
        {
            var val = ReadUInt32();
            var union = new UnionVal();
            union.intVal = val;

            return union.floatVal;
        }

        public double ReadDouble()
        {
            var val = ReadUInt64();
            var union = new UnionVal();
            union.longVal = val;

            return union.doubleVal;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readBytes = 0;
            var pos = Position;
            var len = Length;
            for (var i = 0; i < count && pos < len; i++)
            {
                buffer[i + offset] = _srcByteArray[pos++];
                readBytes++;
            }

            Position = pos;
            return readBytes;
        }

        public new byte ReadByte()
        {
            var pos = Position;
            var val = _srcByteArray[pos++];
            Position = pos;

            return val;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
                WriteByte(buffer[i + offset]);
        }

        public override void WriteByte(byte value)
        {
            var pos = Position;
            _srcByteArray[pos++] = value;
            Position = pos;
        }

        public void Write(byte val)
        {
            WriteByte(val);
        }

        public void Write(byte[] val)
        {
            Write(val, 0, val.Length);
        }

        public void Write(char val)
        {
            var uval = val;
            for (var i = 0; i < sizeof(char); i++)
            {
                WriteByte((byte) (uval & 0xFF));
                uval >>= 8;
            }
        }

        public void Write(char[] val)
        {
            for (var i = 0; i < val.Length; i++) Write(val[i]);
        }

        public void Write(string val)
        {
            Write((uint) val.Length);
            for (var i = 0; i < val.Length; i++) Write(val[i]);
        }

        public void Write(short val)
        {
            for (var i = 0; i < sizeof(short); i++)
            {
                WriteByte((byte) (val & 0xFF));
                val >>= 8;
            }
        }

        public void Write(int val)
        {
            for (var i = 0; i < sizeof(int); i++)
            {
                WriteByte((byte) (val & 0xFF));
                val >>= 8;
            }
        }

        public void Write(long val)
        {
            for (var i = 0; i < sizeof(long); i++)
            {
                WriteByte((byte) (val & 0xFF));
                val >>= 8;
            }
        }

        public void Write(ushort val)
        {
            for (var i = 0; i < sizeof(ushort); i++)
            {
                WriteByte((byte) (val & 0xFF));
                val >>= 8;
            }
        }

        public void Write(uint val)
        {
            for (var i = 0; i < sizeof(uint); i++)
            {
                WriteByte((byte) (val & 0xFF));
                val >>= 8;
            }
        }

        public void Write(ulong val)
        {
            for (var i = 0; i < sizeof(ulong); i++)
            {
                WriteByte((byte) (val & 0xFF));
                val >>= 8;
            }
        }

        public void Write(float val)
        {
            var union = new UnionVal();
            union.floatVal = val;

            Write(union.intVal);
        }

        public void Write(double val)
        {
            var union = new UnionVal();
            union.doubleVal = val;

            Write(union.longVal);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                Position = offset;
            else if (origin == SeekOrigin.Current)
                Position += offset;
            else
                Position = Length - offset - 1;

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UnionVal
        {
            [FieldOffset(0)] public uint intVal;

            [FieldOffset(0)] public float floatVal;

            [FieldOffset(0)] public ulong longVal;

            [FieldOffset(0)] public double doubleVal;
        }
    }
}