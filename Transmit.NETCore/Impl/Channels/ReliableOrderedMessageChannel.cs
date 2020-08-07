namespace Transmit
{
    internal sealed class ReliableOrderedMessageChannel : MessageChannel
    {
        private readonly ReliablePacketController _packetController;

        public float RTT => _packetController.RTT;

        public float PacketLoss => _packetController.PacketLoss;

        public float SentBandwidthKBPS => _packetController.SentBandwidthKBPS;

        public float ReceivedBandwidthKBPS => _packetController.ReceivedBandwidthKBPS;

        public ReliableOrderedMessageChannel(int channelId) : base(channelId)
        {
        }

        public override void Reset()
        {
        }

        public override void Update(double newTime)
        {
        }

        public override void ReceivePacket(ref byte[] buffer, int bufferLength)
        {
        }

        public override void SendMessage(ref byte[] buffer, int bufferLength)
        {
        }
    }
}