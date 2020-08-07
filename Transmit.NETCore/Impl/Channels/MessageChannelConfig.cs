using System;

namespace Transmit
{
    public struct MessageChannelConfig
    {
        // for debug purposes
        public string Name;

        // general parameters
        public int HeaderSize;
        public int MaxPayloadSize;
        public int MaxPacketSize;
        public int ReceivedPacketBufferSize;
        public int SentPacketBufferSize;

        // fragmentation parameters
        public int FragmentSize;
        public int FragmentThreshold;
        public int MaxFragments;
        public int FragmentReassemblyBufferSize;

        // smooth factors
        public float RTTSmoothFactor;
        public float BandwidthSmoothingFactor;
        public float PacketLossSmoothingFactor;

        // callbacks
        public Action<ushort> AckPacketCallback;
        public Action<ushort, byte[], int> ProcessPacketCallback;

        public static MessageChannelConfig Default => new MessageChannelConfig
        {
            Name = "default",
            HeaderSize = 28,
            MaxPayloadSize = 1024, // to fit MTU of network
            MaxPacketSize = 1024 + 28,
            ReceivedPacketBufferSize = 256,
            SentPacketBufferSize = 256,
            FragmentSize = 1024,
            FragmentThreshold = 1024,
            MaxFragments = 64,
            FragmentReassemblyBufferSize = 64,
            RTTSmoothFactor = .25f,
            BandwidthSmoothingFactor = .1f,
            PacketLossSmoothingFactor = .1f
        };
    }
}