﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Transmit.Utils
{
    /// <inheritdoc />
    /// <summary>
    ///     Helper class for a quick non-allocating way to read or write from/to temporary byte arrays as streams
    /// </summary>
    public sealed class ByteArrayReaderWriter : IDisposable
    {
        private static readonly Queue<ByteArrayReaderWriter> ReaderPool = new Queue<ByteArrayReaderWriter>();

        private readonly ByteStream _readStream;
        private readonly ByteStream _writeStream;

        public ByteArrayReaderWriter()
        {
            _readStream = new ByteStream();
            _writeStream = new ByteStream();
        }

        internal long ReadPosition => _readStream.Position;

        internal bool IsDoneReading => _readStream.Position >= _readStream.Length;

        internal long WritePosition => _writeStream.Position;

        public void Dispose()
        {
            Release(this);
        }

        /// <summary>
        ///     Get a reader/writer for the given byte array
        /// </summary>
        internal static ByteArrayReaderWriter Get(byte[] byteArray)
        {
            ByteArrayReaderWriter reader = null;

            lock (ReaderPool)
            {
                if (ReaderPool.Count > 0)
                    reader = ReaderPool.Dequeue();
                else
                    reader = new ByteArrayReaderWriter();
            }

            reader.SetStream(byteArray);
            return reader;
        }

        /// <summary>
        ///     Release a reader/writer to the pool
        /// </summary>
        private static void Release(ByteArrayReaderWriter reader)
        {
            lock (ReaderPool)
            {
                ReaderPool.Enqueue(reader);
            }
        }

        private void SetStream(byte[] byteArray)
        {
            _readStream.SetStreamSource(byteArray);
            _writeStream.SetStreamSource(byteArray);
        }

        internal void SeekRead(long pos)
        {
            _readStream.Seek(pos, SeekOrigin.Begin);
        }

        internal void SeekWrite(long pos)
        {
            _writeStream.Seek(pos, SeekOrigin.Begin);
        }

        internal void Write(byte val)
        {
            _writeStream.Write(val);
        }

        internal void Write(byte[] val)
        {
            _writeStream.Write(val);
        }

        internal void Write(char val)
        {
            _writeStream.Write(val);
        }

        internal void Write(char[] val)
        {
            _writeStream.Write(val);
        }

        internal void Write(string val)
        {
            _writeStream.Write(val);
        }

        internal void Write(short val)
        {
            _writeStream.Write(val);
        }

        internal void Write(int val)
        {
            _writeStream.Write(val);
        }

        internal void Write(long val)
        {
            _writeStream.Write(val);
        }

        internal void Write(ushort val)
        {
            _writeStream.Write(val);
        }

        internal void Write(uint val)
        {
            _writeStream.Write(val);
        }

        internal void Write(ulong val)
        {
            _writeStream.Write(val);
        }

        internal void Write(float val)
        {
            _writeStream.Write(val);
        }

        internal void Write(double val)
        {
            _writeStream.Write(val);
        }

        internal void WriteASCII(char[] chars)
        {
            for (int i = 0, len = chars.Length; i < len; i++)
            {
                var asciiCode = (byte) (chars[i] & 0xFF);
                Write(asciiCode);
            }
        }

        internal void WriteASCII(string str)
        {
            for (int i = 0, len = str.Length; i < len; i++)
            {
                var asciiCode = (byte) (str[i] & 0xFF);
                Write(asciiCode);
            }
        }

        internal void WriteBuffer(byte[] buffer, int length)
        {
            for (var i = 0; i < length; i++)
                Write(buffer[i]);
        }

        internal byte ReadByte()
        {
            return _readStream.ReadByte();
        }

        internal byte[] ReadBytes(int length)
        {
            return _readStream.ReadBytes(length);
        }

        internal char ReadChar()
        {
            return _readStream.ReadChar();
        }

        internal char[] ReadChars(int length)
        {
            return _readStream.ReadChars(length);
        }

        internal string ReadString()
        {
            return _readStream.ReadString();
        }

        internal short ReadInt16()
        {
            return _readStream.ReadInt16();
        }

        internal int ReadInt32()
        {
            return _readStream.ReadInt32();
        }

        internal long ReadInt64()
        {
            return _readStream.ReadInt64();
        }

        internal ushort ReadUInt16()
        {
            return _readStream.ReadUInt16();
        }

        internal uint ReadUInt32()
        {
            return _readStream.ReadUInt32();
        }

        internal ulong ReadUInt64()
        {
            return _readStream.ReadUInt64();
        }

        internal float ReadSingle()
        {
            return _readStream.ReadSingle();
        }

        internal double ReadDouble()
        {
            return _readStream.ReadDouble();
        }

        internal void ReadASCIICharsIntoBuffer(char[] buffer, int length)
        {
            for (var i = 0; i < length; i++)
                buffer[i] = (char) ReadByte();
        }

        internal void ReadBytesIntoBuffer(byte[] buffer, int length)
        {
            for (var i = 0; i < length; i++)
                buffer[i] = ReadByte();
        }
    }
}