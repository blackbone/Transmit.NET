namespace ReliableNetcode
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

        public override void ReceivePacket(byte[] buffer, int bufferLength)
        {
        }

        public override void SendMessage(byte[] buffer, int bufferLength)
        {
        }
    }
}