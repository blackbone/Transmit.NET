using System;
using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    internal sealed class UnreliableMessageChannel : MessageChannel
    {
        private readonly ReliableConfig config;
        private readonly ReliablePacketController packetController;
        private readonly SequenceBuffer<ReceivedPacketData> receiveBuffer;

        public UnreliableMessageChannel(int channelId) : base(channelId)
        {
            receiveBuffer = new SequenceBuffer<ReceivedPacketData>(256);

            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => { TransmitCallback(buffer, size); };
            config.ProcessPacketCallback = (seq, buffer, size) =>
            {
                if (!receiveBuffer.Exists(seq))
                {
                    receiveBuffer.Insert(seq);
                    ReceiveCallback(buffer, size);
                }
            };

            packetController = new ReliablePacketController(config, DateTime.Now.GetTotalSeconds());
        }

        public override void Reset()
        {
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
    }
}