using System;

namespace ReliableNetcode.Utils
{
    internal sealed class ByteBuffer
    {
        public ByteBuffer()
        {
            InternalBuffer = null;
            Length = 0;
        }

        public ByteBuffer(int size = 0)
        {
            InternalBuffer = new byte[size];
            this.Length = size;
        }

        public int Length { get; private set; }

        public byte[] InternalBuffer { get; private set; }

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index > Length) throw new IndexOutOfRangeException();

                return InternalBuffer[index];
            }
            set
            {
                if (index < 0 || index > Length) throw new IndexOutOfRangeException();

                InternalBuffer[index] = value;
            }
        }

        public void SetSize(int newSize)
        {
            if (InternalBuffer == null || InternalBuffer.Length < newSize)
            {
                var newBuffer = new byte[newSize];

                if (InternalBuffer != null)
                    Buffer.BlockCopy(InternalBuffer, 0, newBuffer, 0, InternalBuffer.Length);

                InternalBuffer = newBuffer;
            }

            Length = newSize;
        }

        public void BufferCopy(byte[] source, int src, int dest, int length)
        {
            Buffer.BlockCopy(source, src, InternalBuffer, dest, length);
        }

        public void BufferCopy(ByteBuffer source, int src, int dest, int length)
        {
            Buffer.BlockCopy(source.InternalBuffer, src, InternalBuffer, dest, length);
        }
    }
}