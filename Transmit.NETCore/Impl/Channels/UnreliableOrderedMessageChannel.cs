using System;
using Transmit.Utils;

namespace Transmit
{
    internal sealed class UnreliableOrderedMessageChannel : MessageChannel
    {
        private readonly ReliableConfig config;
        private readonly ReliablePacketController packetController;

        private ushort nextSequence;

        public UnreliableOrderedMessageChannel(int channelId) : base(channelId)
        {
            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => { TransmitCallback(buffer, size); };
            config.ProcessPacketCallback = ProcessPacket;

            packetController = new ReliablePacketController(config, DateTime.Now.GetTotalSeconds());
        }

        public override void Reset()
        {
            nextSequence = 0;
            packetController.Reset();
        }

        public override void Update(double newTime)
        {
            packetController.Update(newTime);
        }

        public override void ReceivePacket(ref byte[] buffer, int bufferLength)
        {
            packetController.ReceivePacket(buffer, bufferLength);
        }

        public override void SendMessage(ref byte[] buffer, int bufferLength)
        {
            packetController.SendPacket(buffer, bufferLength, (byte) ChannelID);
        }

        private void ProcessPacket(ushort sequence, byte[] buffer, int length)
        {
            // only process a packet if it is the next packet we expect, or it is newer.
            if (sequence == nextSequence || PacketIo.SequenceGreaterThan(sequence, nextSequence))
            {
                nextSequence = (ushort) (sequence + 1);
                ReceiveCallback(buffer, length);
            }
        }
    }
}