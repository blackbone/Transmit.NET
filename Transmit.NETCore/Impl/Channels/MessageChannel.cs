using System;

namespace Transmit
{
    /*
     * abstract base for any message channel
     */
    internal abstract class MessageChannel
    {
        private readonly ITransportChannel _transportChannel;

        public Action<byte[], int> ReceiveCallback;
        public Action<byte[], int> TransmitCallback;
        protected int ChannelID { get; }

        internal ITransportChannel Transport { get; set; }

        protected MessageChannel(int channelId)
        {
            ChannelID = channelId;
        }

        public abstract void Reset();
        public abstract void Update(double newTime);
        public abstract void ReceivePacket(ref byte[] buffer, int bufferLength);
        public abstract void SendMessage(ref byte[] buffer, int bufferLength);
    }

    // an unreliable implementation of MessageChannel
    // does not make any guarantees about message reliability except for ignoring duplicate messages

    // an unreliable-ordered implementation of MessageChannel
    // does not make any guarantees that a message will arrive, BUT does guarantee that messages will be received in chronological order

    // a reliable ordered implementation of MessageChannel
}