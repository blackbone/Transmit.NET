using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    internal class SentPacketData
    {
        public bool acked;
        public uint packetBytes;
        public double time;
    }

    internal class ReceivedPacketData
    {
        public uint packetBytes;
        public double time;
    }

    internal class FragmentReassemblyData
    {
        public ushort Ack;
        public uint AckBits;
        public bool[] FragmentReceived = new bool[256];
        public int HeaderOffset;
        public int NumFragmentsReceived;
        public int NumFragmentsTotal;
        public int PacketBytes;
        public ByteBuffer PacketDataBuffer = new ByteBuffer();
        public ushort Sequence;

        public void StoreFragmentData(byte channelID, ushort sequence, ushort ack, uint ackBits, int fragmentID,
            int fragmentSize, byte[] fragmentData, int fragmentBytes)
        {
            var copyOffset = 0;

            if (fragmentID == 0)
            {
                var packetHeader = BufferPool.GetBuffer(Defines.MAX_PACKET_HEADER_BYTES);
                var headerBytes = PacketIO.WritePacketHeader(packetHeader, channelID, sequence, ack, ackBits);
                HeaderOffset = Defines.MAX_PACKET_HEADER_BYTES - headerBytes;

                if (PacketDataBuffer.Length < Defines.MAX_PACKET_HEADER_BYTES + fragmentSize)
                    PacketDataBuffer.SetSize(Defines.MAX_PACKET_HEADER_BYTES + fragmentSize);

                PacketDataBuffer.BufferCopy(packetHeader, 0, HeaderOffset, headerBytes);
                copyOffset = headerBytes;

                fragmentBytes -= headerBytes;

                BufferPool.ReturnBuffer(packetHeader);
            }

            var writePos = Defines.MAX_PACKET_HEADER_BYTES + fragmentID * fragmentSize;
            var end = writePos + fragmentBytes;

            if (PacketDataBuffer.Length < end)
                PacketDataBuffer.SetSize(end);

            if (fragmentID == NumFragmentsTotal - 1)
                PacketBytes = (NumFragmentsTotal - 1) * fragmentSize + fragmentBytes;

            PacketDataBuffer.BufferCopy(fragmentData, copyOffset,
                Defines.MAX_PACKET_HEADER_BYTES + fragmentID * fragmentSize, fragmentBytes);
        }
    }
}